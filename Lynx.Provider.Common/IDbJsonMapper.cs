using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Lynx.Provider.Common.Models;

namespace Lynx.Provider.Common;

internal interface IDbJsonMapper
{
    /// <summary>
    /// Setup a parameter for a JSON value
    /// </summary>
    static abstract Expression SetupJsonParameter(Expression parameter);

    /// <summary>
    /// Convert a value to a JSON parameter value, if a conversion is necessary
    /// </summary>
    static abstract Expression? CreateJsonValue(Expression value);
}