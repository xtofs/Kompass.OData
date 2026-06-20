namespace Kompass.OData.Url;

/// <summary>
/// Byte span in the decoded $filter expression string.
/// </summary>
public readonly record struct FilterSpan(int Start, int End)
{
    public int Length => End - Start;

    public override string ToString()
    {
        return $"{Start}:{End}";
    }
}

/// <summary>
/// Represents the $filter query option.
/// </summary>
public sealed class FilterClause
{
    public FilterExpression Expression { get; }

    public FilterClause(FilterExpression expression)
    {
        Expression = expression;
    }

    public override string ToString()
    {
        return Expression.ToString();
    }
}

/// <summary>
/// A node in the $filter expression AST.
/// </summary>
public sealed class FilterExpression
{
    public FilterExpressionKind Kind { get; }
    public FilterSpan Span { get; }

    public FilterExpression(FilterExpressionKind kind, FilterSpan span)
    {
        Kind = kind;
        Span = span;
    }

    public override string ToString()
    {
        return FilterFormatter.Format(Kind, 0);
    }
}

/// <summary>
/// The kind of a filter expression node.
/// </summary>
public abstract record FilterExpressionKind
{
    public sealed record Literal(FilterLiteral Value) : FilterExpressionKind;

    public sealed record Member(FilterMemberPath Path) : FilterExpressionKind;

    public sealed record FunctionCall(FilterFunctionCallData Data) : FilterExpressionKind;

    public sealed record Unary(FilterUnaryOperator Operator, FilterExpression Operand) : FilterExpressionKind;

    public sealed record Binary(
        FilterExpression Left,
        FilterBinaryOperator Operator,
        FilterExpression Right) : FilterExpressionKind;
}

/// <summary>
/// A literal value in a filter expression.
/// </summary>
public abstract record FilterLiteral
{
    public sealed record Null : FilterLiteral
    {
        public static Null Instance { get; } = new Null();
        public override string ToString() => "null";
    }

    public sealed record Boolean(bool Value) : FilterLiteral
    {
        public override string ToString() => Value ? "true" : "false";
    }

    public sealed record Number(string Value) : FilterLiteral
    {
        public override string ToString() => Value;
    }

    public sealed record String(string Value) : FilterLiteral
    {
        public override string ToString() => $"'{Value.Replace("'", "''")}'";
    }
}

/// <summary>
/// A member access path in a filter expression (e.g. "Orders/anyCount").
/// </summary>
public sealed class FilterMemberPath
{
    public IReadOnlyList<string> Segments { get; }

    public FilterMemberPath(IReadOnlyList<string> segments)
    {
        Segments = segments;
    }

    public override string ToString()
    {
        return string.Join("/", Segments);
    }
}

/// <summary>
/// A function call in a filter expression.
/// </summary>
public sealed class FilterFunctionCallData
{
    public string Name { get; }
    public IReadOnlyList<FilterExpression> Arguments { get; }

    public FilterFunctionCallData(string name, IReadOnlyList<FilterExpression> arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public override string ToString()
    {
        var args = string.Join(", ", Arguments.Select(a => a.ToString()));
        return $"{Name}({args})";
    }
}

/// <summary>
/// Unary operators for filter expressions.
/// </summary>
public enum FilterUnaryOperator
{
    Not,
    Negate,
}

/// <summary>
/// Binary operators for filter expressions, ordered by precedence group.
/// </summary>
public enum FilterBinaryOperator
{
    Or,
    And,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
}

/// <summary>
/// Internal formatter that handles operator precedence and parenthesization.
/// </summary>
internal static class FilterFormatter
{
    internal static int Precedence(FilterBinaryOperator op)
    {
        return op switch
        {
            FilterBinaryOperator.Or => 0,
            FilterBinaryOperator.And => 1,
            FilterBinaryOperator.Equal or FilterBinaryOperator.NotEqual or
            FilterBinaryOperator.GreaterThan or FilterBinaryOperator.GreaterThanOrEqual or
            FilterBinaryOperator.LessThan or FilterBinaryOperator.LessThanOrEqual => 2,
            FilterBinaryOperator.Add or FilterBinaryOperator.Subtract => 3,
            FilterBinaryOperator.Multiply or FilterBinaryOperator.Divide or
            FilterBinaryOperator.Modulo => 4,
            _ => 0,
        };
    }

    internal static string OperatorKeyword(FilterBinaryOperator op)
    {
        return op switch
        {
            FilterBinaryOperator.Or => "or",
            FilterBinaryOperator.And => "and",
            FilterBinaryOperator.Equal => "eq",
            FilterBinaryOperator.NotEqual => "ne",
            FilterBinaryOperator.GreaterThan => "gt",
            FilterBinaryOperator.GreaterThanOrEqual => "ge",
            FilterBinaryOperator.LessThan => "lt",
            FilterBinaryOperator.LessThanOrEqual => "le",
            FilterBinaryOperator.Add => "add",
            FilterBinaryOperator.Subtract => "sub",
            FilterBinaryOperator.Multiply => "mul",
            FilterBinaryOperator.Divide => "div",
            FilterBinaryOperator.Modulo => "mod",
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    internal static string Format(FilterExpressionKind kind, int parentPrecedence)
    {
        switch (kind)
        {
            case FilterExpressionKind.Literal lit:
                return lit.Value.ToString()!;

            case FilterExpressionKind.Member mem:
                return mem.Path.ToString();

            case FilterExpressionKind.FunctionCall fc:
            {
                var args = string.Join(", ", fc.Data.Arguments.Select(a => Format(a.Kind, 0)));
                return $"{fc.Data.Name}({args})";
            }

            case FilterExpressionKind.Unary un:
            {
                var operandStr = Format(un.Operand.Kind, 5);
                return un.Operator switch
                {
                    FilterUnaryOperator.Not => $"not {operandStr}",
                    FilterUnaryOperator.Negate => $"-{operandStr}",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            case FilterExpressionKind.Binary bin:
            {
                var myPrec = Precedence(bin.Operator);
                var leftStr = Format(bin.Left.Kind, myPrec);
                var rightStr = Format(bin.Right.Kind, myPrec);
                var keyword = OperatorKeyword(bin.Operator);
                var result = $"{leftStr} {keyword} {rightStr}";

                if (myPrec < parentPrecedence)
                {
                    result = $"({result})";
                }

                return result;
            }

            default:
                throw new InvalidOperationException($"Unknown filter expression kind: {kind.GetType().Name}");
        }
    }
}
