using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.EfCore;

/// <summary>
/// Indexes over any members that EF Core can translate: properties of the entity, members of complex types
/// (mapped to columns or to JSON) and of owned types, in any combination.
/// </summary>
/// <remarks>
/// <para>
/// EF Core can't express some of these as model indexes: before EF Core 11, indexes can't use members of complex types;
/// from EF Core 11, an index can't mix members inside a JSON column with regular columns.
/// </para>
/// <para>
/// The SQL for each column is EF Core's own translation of the member
/// (i.e. the projection of <c>Set&lt;T&gt;().Select(x =&gt; x.Member)</c>), so the index matches the queries that use it.
/// Supported on PostgreSQL and SQLite (the script uses CREATE INDEX IF NOT EXISTS).
/// The indexes are not part of the EF Core model (so migrations don't know about them):
/// create them after creating the database, with <see cref="ExecuteCreateFullIndexes"/>,
/// as with <see cref="ForeignKeyHelpers.ExecuteSetConstraintsDeferrable"/>.
/// </para>
/// </remarks>
[PublicAPI]
public static partial class FullIndexHelpers
{
    private const string AnnotationName = "Lynx:FullIndexes";

    /// <summary>
    /// Defines an index over the given members (e.g. <c>x =&gt; new { x.FactoryId, x.StartedAt.Instant, x.Details.Status }</c>)
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    /// <param name="members">A member (<c>x =&gt; x.A.B</c>) or an anonymous type of members</param>
    /// <param name="name">Index name. Defaults to IX_{table}_{members}</param>
    /// <param name="unique">Whether the index is unique</param>
    public static EntityTypeBuilder<T> HasFullIndex<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, object?>> members,
        string? name = null,
        bool unique = false)
        where T : class
    {
        var body = members.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : members.Body;

        var columns = body is NewExpression newExpression
            ? newExpression.Arguments.ToList()
            : [body];

        var lambdas = columns
            .Select(c => Expression.Lambda(c, members.Parameters))
            .ToList();

        var definitions = builder.Metadata.FindAnnotation(AnnotationName)?.Value as List<FullIndex> ?? [];
        definitions.Add(new FullIndex(typeof(T), lambdas, name, unique));
        builder.Metadata.SetAnnotation(AnnotationName, definitions);
        return builder;
    }

    /// <summary>
    /// Gets the full indexes defined on the model
    /// </summary>
    public static IEnumerable<FullIndex> GetFullIndexes(this IModel model) =>
        model.GetEntityTypes()
            .SelectMany(e => e.FindAnnotation(AnnotationName)?.Value as List<FullIndex> ?? []);

    /// <summary>
    /// Generates the script to create all full indexes (if they don't exist).
    /// </summary>
    /// <remarks>
    /// Requires a context (rather than the model), as the members are translated by EF Core's query pipeline
    /// </remarks>
    public static string GenerateCreateFullIndexesScript(this DbContext context)
    {
        var sql = new StringBuilder();
        foreach (var index in context.Model.GetFullIndexes())
            sql.AppendLine(GenerateCreateIndex(context, index));
        return sql.ToString();
    }

    /// <summary>
    /// Creates all full indexes in the database (if they don't exist)
    /// </summary>
    public static void ExecuteCreateFullIndexes(DbContext context)
    {
        var sql = context.GenerateCreateFullIndexesScript();
        if (sql.Length > 0)
            context.Database.ExecuteSqlRaw(sql);
    }

    /// <summary>
    /// Creates all full indexes in the database (if they don't exist)
    /// </summary>
    public static async Task ExecuteCreateFullIndexesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var sql = context.GenerateCreateFullIndexesScript();
        if (sql.Length > 0)
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static string GenerateCreateIndex(DbContext context, FullIndex index)
    {
        var entityType = context.Model.FindEntityType(index.EntityType)
                         ?? throw new InvalidOperationException($"Entity type {index.EntityType} not found");
        var table = entityType.GetTableName()
                    ?? throw new InvalidOperationException($"Entity type {entityType.DisplayName()} is not mapped to a table");
        var schema = entityType.GetSchema();

        var helper = context.GetService<ISqlGenerationHelper>();
        var columns = index.Members.Select(m => TranslateMember(context, index.EntityType, m)).ToList();

        var name = index.Name ?? $"IX_{table}_{string.Join("_", index.Members.Select(GetMemberName))}";

        return $"CREATE {(index.Unique ? "UNIQUE " : "")}INDEX IF NOT EXISTS {helper.DelimitIdentifier(name)} " +
               $"ON {helper.DelimitIdentifier(table, schema)} ({string.Join(", ", columns)}){helper.StatementTerminator}";
    }

    /// <summary>
    /// Gets the SQL for a member, by translating <c>Set&lt;T&gt;().Select(x =&gt; x.Member)</c> and removing the table alias
    /// </summary>
    private static string TranslateMember(DbContext context, Type entityType, LambdaExpression member)
    {
        // Translate as an ordering: unlike a projection, it is always translated to SQL
        // (EF Core 8/9 evaluate a projected complex type member on the client)
        var set = (IQueryable)SetMethod.MakeGenericMethod(entityType).Invoke(context, [])!;
        var orderBy = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.OrderBy),
            [entityType, member.ReturnType],
            set.Expression,
            Expression.Quote(member));
        var parameter = Expression.Parameter(entityType, "x");
        var select = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [entityType, typeof(int)],
            orderBy,
            Expression.Quote(Expression.Lambda(Expression.Constant(1), parameter)));
        var sql = set.Provider.CreateQuery(select).ToQueryString();

        var match = OrderByRegex().Match(sql);
        if (!match.Success)
            throw new InvalidOperationException($"Unable to parse the translation of {member}:{Environment.NewLine}{sql}");

        var alias = match.Groups["alias"].Value;
        var expression = match.Groups["expression"].Value.Trim();
        expression = Regex.Replace(expression, $@"(?<![\w""\]]){Regex.Escape(alias)}\.(?=[""\[`])", "");

        // A column is used as is, anything else is an expression index
        return PlainIdentifierRegex().IsMatch(expression) ? expression : $"({expression})";
    }

    private static string GetMemberName(LambdaExpression member)
    {
        var names = new List<string>();
        for (var e = member.Body; e != null;)
        {
            switch (e)
            {
                case MemberExpression m:
                    names.Add(m.Member.Name);
                    e = m.Expression;
                    break;
                case UnaryExpression u:
                    e = u.Operand;
                    break;
                default:
                    e = null;
                    break;
            }
        }
        names.Reverse();
        return string.Join("_", names);
    }

    private static readonly System.Reflection.MethodInfo SetMethod =
        typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!;

    [GeneratedRegex("""^FROM [^\n]+? AS (?<alias>\w+|"\w+"|\[\w+\]|`\w+`)\s*\nORDER BY (?<expression>.+?)\s*\z""", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex OrderByRegex();

    [GeneratedRegex("""^("[^"]+"|\[[^\]]+\]|`[^`]+`|\w+)$""")]
    private static partial Regex PlainIdentifierRegex();

    /// <summary>
    /// A full index definition
    /// </summary>
    /// <param name="EntityType">Entity CLR type</param>
    /// <param name="Members">Members (lambdas over the entity)</param>
    /// <param name="Name">Index name, or null for the default name</param>
    /// <param name="Unique">Whether the index is unique</param>
    public record FullIndex(Type EntityType, IReadOnlyList<LambdaExpression> Members, string? Name, bool Unique);
}
