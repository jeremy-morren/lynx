using System.Linq.Expressions;

namespace Lynx.Provider.Common;

internal interface IDbJsonMapper
{
    /// <summary>
    /// Setup a parameter for a JSON value
    /// </summary>
    static abstract Expression SetupJsonParameter(Expression parameter);

    /// <summary>
    /// Convert a value to a JSON parameter value
    /// </summary>
    static abstract Expression SerializeJson(Expression value);
}