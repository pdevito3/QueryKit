namespace QueryKit;

using System.Linq.Expressions;

internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression newParameter)
    {
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // Replace all parameters with the new parameter
        return _newParameter;
    }
}