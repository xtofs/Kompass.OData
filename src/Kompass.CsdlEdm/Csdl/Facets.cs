namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// MaxLength facet: either a numeric value or "max".
/// </summary>
public abstract record MaxLengthFacet
{
    private MaxLengthFacet() { }

    public sealed record Number(ulong Value) : MaxLengthFacet
    {
        public override string ToString() => Value.ToString();
    }

    public sealed record Max() : MaxLengthFacet
    {
        public override string ToString() => "max";
    }

    public static MaxLengthFacet? Parse(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        if (string.Equals(raw, "max", StringComparison.OrdinalIgnoreCase))
        {
            return new Max();
        }

        if (ulong.TryParse(raw, out var value))
        {
            return new Number(value);
        }

        return null;
    }
}

/// <summary>
/// Scale facet: either a numeric value or "variable".
/// </summary>
public abstract record ScaleFacet
{
    private ScaleFacet() { }

    public sealed record Number(ulong Value) : ScaleFacet
    {
        public override string ToString() => Value.ToString();
    }

    public sealed record Variable() : ScaleFacet
    {
        public override string ToString() => "variable";
    }

    public static ScaleFacet? Parse(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        if (string.Equals(raw, "variable", StringComparison.OrdinalIgnoreCase))
        {
            return new Variable();
        }

        if (ulong.TryParse(raw, out var value))
        {
            return new Number(value);
        }

        return null;
    }
}

/// <summary>
/// SRID facet: either a numeric value or "variable".
/// </summary>
public abstract record SridFacet
{
    private SridFacet() { }

    public sealed record Number(ulong Value) : SridFacet
    {
        public override string ToString() => Value.ToString();
    }

    public sealed record Variable() : SridFacet
    {
        public override string ToString() => "variable";
    }

    public static SridFacet? Parse(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        if (string.Equals(raw, "variable", StringComparison.OrdinalIgnoreCase))
        {
            return new Variable();
        }

        if (ulong.TryParse(raw, out var value))
        {
            return new Number(value);
        }

        return null;
    }
}
