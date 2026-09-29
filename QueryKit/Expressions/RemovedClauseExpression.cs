namespace QueryKit.Expressions;

using System.Linq.Expressions;

/// <summary>
/// Marks a filter clause that the parser removed, for example a clause on an unknown property.
/// A logical operator with a removed side becomes its other side, so the clause has no effect on the result.
/// </summary>
internal sealed class RemovedClauseExpression : Expression
{
    public static readonly RemovedClauseExpression Instance = new();

    private RemovedClauseExpression()
    {
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(bool);
}
