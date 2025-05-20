using System.Linq.Expressions;
using Lynx.Providers.Common.Models;

namespace Lynx.Providers.Common;

/// <summary>
/// Delegate builder for provider specific expressions
/// </summary>
internal interface IProviderDelegateBuilder
{
    /// <summary>
    /// Setup the database parameter type for a property
    /// </summary>
    static abstract Expression? SetupParameterDbType(ParameterExpression parameter, ScalarEntityPropertyInfo property);
    
    /// <summary>
    /// Setup a parameter for a JSON value
    /// </summary>
    static abstract Expression SetupJsonParameter(Expression parameter);
}