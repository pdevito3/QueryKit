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

    // A flat chain such as a && b && ... is a left-nested tree with one level for each operator.
    // The base visitor recurses once for each level, so a long chain overflowed the stack.
    // This walks down the left side of the chain in a loop, then rebuilds it from the bottom.
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (!IsLogical(node))
        {
            return base.VisitBinary(node);
        }

        var chain = new Stack<BinaryExpression>();
        Expression current = node;
        while (current is BinaryExpression binary && IsLogical(binary))
        {
            chain.Push(binary);
            current = binary.Left;
        }

        var left = Visit(current);
        while (chain.Count > 0)
        {
            var binary = chain.Pop();
            left = binary.Update(left, VisitAndConvert(binary.Conversion, nameof(VisitBinary)), Visit(binary.Right));
        }

        return left;
    }

    private static bool IsLogical(BinaryExpression node)
        => node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse;
}
