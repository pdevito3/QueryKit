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
        // Replace all parameters of the same type with the new parameter
        if (node.Type == _newParameter.Type)
        {
            return _newParameter;
        }

        return base.VisitParameter(node);
    }
}