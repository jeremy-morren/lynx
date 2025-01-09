using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Npgsql.BackupRestore.Commands;

internal static class CommandHelpers
{
    /// <summary>
    /// Gets arguments from the options object, using the switch names dictionary to map property names to switch names.
    /// </summary>
    /// <param name="options">Options object</param>
    /// <param name="names">Map of property names to option names</param>
    /// <returns></returns>
    public static IEnumerable<string> GetArgs(object options, Dictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(nameof(options));
        ArgumentNullException.ThrowIfNull(nameof(names));

        var properties = options.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in properties)
        {
            if (!names.TryGetValue(p.Name, out var option))
                throw new InvalidOperationException($"Option name for property {p.Name} not specified");
            
            var value = p.GetValue(options);
            switch (value)
            {
                case null:
                    break; //Ignore null values
                case bool b:
                    //If the value is true, add the option name as a switch
                    if (b)
                        yield return option;
                    break;
                case string s:
                    if (string.IsNullOrEmpty(s))
                        break; //Ignore empty strings
                    yield return $"{option}={s}";
                    break;
                case Enum e:
                    yield return $"{option}={e}";
                    break;
                case int i:
                    yield return $"{option}={i}";
                    break;
                default:
                    throw new NotImplementedException($"Unknown property type {value.GetType()}");
            }
        }

    }

    /// <summary>
    /// Gets environment variables for pg tools from a connection string builder.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/libpq-envars.html#LIBPQ-ENVARS
    /// </remarks>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static Dictionary<string, string?> GetEnvVariables(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.Password) 
            && string.IsNullOrEmpty(builder.Passfile)
            && string.IsNullOrEmpty(builder.SslCertificate))
            throw new InvalidOperationException($"Password, passfile, or SSL certificate must be specified in the connection string. Consider setting {nameof(builder.PersistSecurityInfo)} to true");
        var values = new (string Name, object? Value)[]
        {
            ("PGHOST", builder.Host),
            ("PGPORT", builder.Port),
            ("PGDATABASE", builder.Database),
            ("PGUSER", builder.Username),
            ("PGPASSWORD", builder.Password),
            ("PGPASSFILE", builder.Passfile),
            ("PGCHANNELBINDING", builder.ChannelBinding.ToString().ToLowerInvariant()),
            ("PGOPTIONS", builder.Options),
            ("PGAPPNAME", builder.ApplicationName),
            ("PGSSLMODE", builder.SslMode switch
            {
                SslMode.VerifyFull => "verify-full",
                SslMode.VerifyCA => "verify-ca",
                _ => builder.SslMode.ToString().ToLowerInvariant()
            }),
            ("PGSSLCERT", builder.SslCertificate),
            ("PGSSLKEY", builder.SslKey),
            ("PGSSLROOTCERT", builder.RootCertificate),
            ("PGKRBSRVNAME", builder.KerberosServiceName),
            ("PGCONNECT_TIMEOUT", builder.Timeout),
            ("PGCLIENTENCODING", builder.ClientEncoding),
            ("PGLOADBALANCEHOSTS", builder.LoadBalanceHosts ? "random" : "disable"),

            ("PGTZ", builder.Timezone),
        };

        return (
                from x in values
                let value = x.Value?.ToString()
                where !string.IsNullOrEmpty(value)
                select new KeyValuePair<string, string?>(x.Name, value))
            .ToDictionary();
    }
}