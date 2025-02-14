using System.Linq.Expressions;

namespace Lynx.EfCore.KeyFilter;

internal static class ExpressionCaptureValue
{
    public static Expression CaptureValue<T>(T value)
    {
        //Compiler will generate a field to store the value in the lambda
        Expression<Func<T>> expression = () => value;
        return expression.Body;
    }
}