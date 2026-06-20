namespace Kompass.OData.Url;

/// <summary>
/// Internal recursive-descent parser for OData $filter expressions.
/// Implements operator precedence via a Pratt-style layered approach.
/// </summary>
internal sealed class FilterParser
{
    private readonly string _input;
    private int _index;

    internal FilterParser(string input)
    {
        _input = input;
        _index = 0;
    }

    internal int Position => _index;
    private bool IsEof => _index >= _input.Length;
    private ReadOnlySpan<char> Current => _input.AsSpan(_index);

    internal FilterExpression ParseExpression()
    {
        return ParseOrExpression();
    }

    private FilterExpression ParseOrExpression()
    {
        var left = ParseAndExpression();
        while (ConsumeKeyword("or"))
        {
            var right = ParseAndExpression();
            var span = new FilterSpan(left.Span.Start, right.Span.End);
            left = new FilterExpression(
                new FilterExpressionKind.Binary(left, FilterBinaryOperator.Or, right),
                span);
        }
        return left;
    }

    private FilterExpression ParseAndExpression()
    {
        var left = ParseComparisonExpression();
        while (ConsumeKeyword("and"))
        {
            var right = ParseComparisonExpression();
            var span = new FilterSpan(left.Span.Start, right.Span.End);
            left = new FilterExpression(
                new FilterExpressionKind.Binary(left, FilterBinaryOperator.And, right),
                span);
        }
        return left;
    }

    private FilterExpression ParseComparisonExpression()
    {
        var left = ParseAdditiveExpression();
        var op = TryConsumeComparisonOperator();
        if (op is not null)
        {
            var right = ParseAdditiveExpression();
            var span = new FilterSpan(left.Span.Start, right.Span.End);
            return new FilterExpression(
                new FilterExpressionKind.Binary(left, op.Value, right),
                span);
        }
        return left;
    }

    private FilterBinaryOperator? TryConsumeComparisonOperator()
    {
        if (ConsumeKeyword("eq")) { return FilterBinaryOperator.Equal; }
        if (ConsumeKeyword("ne")) { return FilterBinaryOperator.NotEqual; }
        if (ConsumeKeyword("ge")) { return FilterBinaryOperator.GreaterThanOrEqual; }
        if (ConsumeKeyword("gt")) { return FilterBinaryOperator.GreaterThan; }
        if (ConsumeKeyword("le")) { return FilterBinaryOperator.LessThanOrEqual; }
        if (ConsumeKeyword("lt")) { return FilterBinaryOperator.LessThan; }
        return null;
    }

    private FilterExpression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();
        while (true)
        {
            FilterBinaryOperator op;
            if (ConsumeKeyword("add")) { op = FilterBinaryOperator.Add; }
            else if (ConsumeKeyword("sub")) { op = FilterBinaryOperator.Subtract; }
            else { break; }

            var right = ParseMultiplicativeExpression();
            var span = new FilterSpan(left.Span.Start, right.Span.End);
            left = new FilterExpression(
                new FilterExpressionKind.Binary(left, op, right),
                span);
        }
        return left;
    }

    private FilterExpression ParseMultiplicativeExpression()
    {
        var left = ParseUnaryExpression();
        while (true)
        {
            FilterBinaryOperator op;
            if (ConsumeKeyword("mul")) { op = FilterBinaryOperator.Multiply; }
            else if (ConsumeKeyword("div")) { op = FilterBinaryOperator.Divide; }
            else if (ConsumeKeyword("mod")) { op = FilterBinaryOperator.Modulo; }
            else { break; }

            var right = ParseUnaryExpression();
            var span = new FilterSpan(left.Span.Start, right.Span.End);
            left = new FilterExpression(
                new FilterExpressionKind.Binary(left, op, right),
                span);
        }
        return left;
    }

    private FilterExpression ParseUnaryExpression()
    {
        var start = _index;
        if (ConsumeKeyword("not"))
        {
            var operand = ParseUnaryExpression();
            var span = new FilterSpan(start, operand.Span.End);
            return new FilterExpression(
                new FilterExpressionKind.Unary(FilterUnaryOperator.Not, operand),
                span);
        }

        if (!IsEof && _input[_index] == '-')
        {
            _index++;
            ConsumeWhitespace();
            var operand = ParseUnaryExpression();
            var span = new FilterSpan(start, operand.Span.End);
            return new FilterExpression(
                new FilterExpressionKind.Unary(FilterUnaryOperator.Negate, operand),
                span);
        }

        return ParsePrimaryExpression();
    }

    private FilterExpression ParsePrimaryExpression()
    {
        ConsumeWhitespace();

        if (IsEof)
        {
            throw CreateError("unexpected end of expression");
        }

        // Parenthesized group
        if (_input[_index] == '(')
        {
            var start = _index;
            _index++;
            ConsumeWhitespace();
            var inner = ParseExpression();
            ConsumeWhitespace();
            if (IsEof || _input[_index] != ')')
            {
                throw CreateError("expected closing ')'");
            }
            _index++;
            ConsumeWhitespace();
            return new FilterExpression(inner.Kind, new FilterSpan(start, _index));
        }

        // String literal
        if (_input[_index] == '\'')
        {
            var start = _index;
            var value = ConsumeStringLiteral();
            if (value is null)
            {
                throw CreateError("unterminated string literal");
            }
            ConsumeWhitespace();
            return new FilterExpression(
                new FilterExpressionKind.Literal(new FilterLiteral.String(value)),
                new FilterSpan(start, _index));
        }

        // Number literal
        if (char.IsDigit(_input[_index]) || (_input[_index] == '.' && _index + 1 < _input.Length && char.IsDigit(_input[_index + 1])))
        {
            var start = _index;
            var value = ConsumeNumberLiteral()!;
            ConsumeWhitespace();
            return new FilterExpression(
                new FilterExpressionKind.Literal(new FilterLiteral.Number(value)),
                new FilterSpan(start, _index));
        }

        // null / true / false / identifier / function call
        var idStart = _index;
        var identifier = ConsumeIdentifier();
        if (identifier is null)
        {
            throw CreateError($"unexpected character: '{_input[_index]}'");
        }

        // Check for "null"
        if (identifier == "null")
        {
            ConsumeWhitespace();
            return new FilterExpression(
                new FilterExpressionKind.Literal(FilterLiteral.Null.Instance),
                new FilterSpan(idStart, _index));
        }

        // Check for boolean literals
        if (identifier == "true" || identifier == "false")
        {
            ConsumeWhitespace();
            return new FilterExpression(
                new FilterExpressionKind.Literal(new FilterLiteral.Boolean(identifier == "true")),
                new FilterSpan(idStart, _index));
        }

        // Check for function call: identifier followed by '('
        if (!IsEof && _input[_index] == '(')
        {
            _index++;
            ConsumeWhitespace();
            var args = new List<FilterExpression>();
            if (!IsEof && _input[_index] != ')')
            {
                args.Add(ParseExpression());
                while (!IsEof && _input[_index] == ',')
                {
                    _index++;
                    ConsumeWhitespace();
                    args.Add(ParseExpression());
                }
            }
            if (IsEof || _input[_index] != ')')
            {
                throw CreateError("expected closing ')' for function call");
            }
            _index++;
            ConsumeWhitespace();
            return new FilterExpression(
                new FilterExpressionKind.FunctionCall(
                    new FilterFunctionCallData(identifier, args)),
                new FilterSpan(idStart, _index));
        }

        // Member path: identifier possibly followed by /identifier segments
        var segments = new List<string> { identifier };
        while (!IsEof && _input[_index] == '/')
        {
            _index++;
            var seg = ConsumeIdentifier();
            if (seg is null)
            {
                throw CreateError("expected identifier after '/'");
            }
            segments.Add(seg);
        }
        ConsumeWhitespace();
        return new FilterExpression(
            new FilterExpressionKind.Member(new FilterMemberPath(segments)),
            new FilterSpan(idStart, _index));
    }

    private bool ConsumeKeyword(string keyword)
    {
        ConsumeWhitespace();
        var remaining = _input.AsSpan(_index);
        if (remaining.Length >= keyword.Length &&
            remaining[..keyword.Length].SequenceEqual(keyword.AsSpan()))
        {
            // Must be followed by whitespace, EOF, or '('
            var afterIdx = _index + keyword.Length;
            if (afterIdx >= _input.Length ||
                char.IsWhiteSpace(_input[afterIdx]) ||
                _input[afterIdx] == '(')
            {
                _index = afterIdx;
                ConsumeWhitespace();
                return true;
            }
        }
        return false;
    }

    private void ConsumeWhitespace()
    {
        while (_index < _input.Length && char.IsWhiteSpace(_input[_index]))
        {
            _index++;
        }
    }

    private string? ConsumeIdentifier()
    {
        var start = _index;
        while (_index < _input.Length &&
               (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_'))
        {
            _index++;
        }
        if (_index == start)
        {
            return null;
        }
        return _input[start.._index];
    }

    private string? ConsumeStringLiteral()
    {
        if (IsEof || _input[_index] != '\'')
        {
            return null;
        }
        _index++; // skip opening quote
        var sb = new System.Text.StringBuilder();
        while (!IsEof)
        {
            if (_input[_index] == '\'')
            {
                _index++;
                if (!IsEof && _input[_index] == '\'')
                {
                    // Escaped single quote
                    sb.Append('\'');
                    _index++;
                }
                else
                {
                    return sb.ToString();
                }
            }
            else
            {
                sb.Append(_input[_index]);
                _index++;
            }
        }
        return null; // unterminated
    }

    private string? ConsumeNumberLiteral()
    {
        var start = _index;
        var hasDigit = false;

        while (_index < _input.Length && char.IsDigit(_input[_index]))
        {
            _index++;
            hasDigit = true;
        }

        if (_index < _input.Length && _input[_index] == '.')
        {
            _index++;
            while (_index < _input.Length && char.IsDigit(_input[_index]))
            {
                _index++;
                hasDigit = true;
            }
        }

        // Optional exponent
        if (_index < _input.Length && (_input[_index] == 'e' || _input[_index] == 'E'))
        {
            _index++;
            if (_index < _input.Length && (_input[_index] == '+' || _input[_index] == '-'))
            {
                _index++;
            }
            while (_index < _input.Length && char.IsDigit(_input[_index]))
            {
                _index++;
            }
        }

        return hasDigit ? _input[start.._index] : null;
    }

    private ODataParseException CreateError(string message)
    {
        return new ODataParseException(
            new ParseError.InvalidFilterExpression(_input, _index, message));
    }
}
