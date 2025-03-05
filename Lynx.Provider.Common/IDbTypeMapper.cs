using System.Data.Common;
using System.Linq.Expressions;
using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Common;

internal interface IDbTypeMapper<TCommand> where TCommand : DbCommand
{
    /// <summary>
    /// Creates an expression to set the DbType of a parameter.
    /// </summary>
    /// <param name="command">Command parameter</param>
    /// <param name="property">Entity property</param>
    static abstract Expression SetDbType(ParameterExpression command, IEntityPropertyInfo property);
}