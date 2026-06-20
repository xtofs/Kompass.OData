namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// OData EDM primitive type kind.
/// </summary>
public enum PrimitiveTypeKind
{
    Binary,
    Boolean,
    Byte,
    Date,
    DateTimeOffset,
    Decimal,
    Double,
    Duration,
    Guid,
    Int16,
    Int32,
    Int64,
    SByte,
    Single,
    String,
    TimeOfDay,
}

/// <summary>
/// A resolved OData EDM primitive type. Each instance is a singleton keyed by <see cref="PrimitiveTypeKind"/>.
/// </summary>
public sealed class PrimitiveType : SchemaElement, IPropertyType, ITermType
{
    private PrimitiveType(PrimitiveTypeKind kind) { Kind = kind; }

    public PrimitiveTypeKind Kind { get; }
    public override string Name => $"Edm.{Kind}";

    public override string ToString() => Name;

    public static readonly PrimitiveType Binary = new PrimitiveType(PrimitiveTypeKind.Binary);
    public static readonly PrimitiveType Boolean = new PrimitiveType(PrimitiveTypeKind.Boolean);
    public static readonly PrimitiveType Byte = new PrimitiveType(PrimitiveTypeKind.Byte);
    public static readonly PrimitiveType Date = new PrimitiveType(PrimitiveTypeKind.Date);
    public static readonly PrimitiveType DateTimeOffset = new PrimitiveType(PrimitiveTypeKind.DateTimeOffset);
    public static readonly PrimitiveType Decimal = new PrimitiveType(PrimitiveTypeKind.Decimal);
    public static readonly PrimitiveType Double = new PrimitiveType(PrimitiveTypeKind.Double);
    public static readonly PrimitiveType Duration = new PrimitiveType(PrimitiveTypeKind.Duration);
    public static readonly PrimitiveType Guid = new PrimitiveType(PrimitiveTypeKind.Guid);
    public static readonly PrimitiveType Int16 = new PrimitiveType(PrimitiveTypeKind.Int16);
    public static readonly PrimitiveType Int32 = new PrimitiveType(PrimitiveTypeKind.Int32);
    public static readonly PrimitiveType Int64 = new PrimitiveType(PrimitiveTypeKind.Int64);
    public static readonly PrimitiveType SByte = new PrimitiveType(PrimitiveTypeKind.SByte);
    public static readonly PrimitiveType Single = new PrimitiveType(PrimitiveTypeKind.Single);
    public static readonly PrimitiveType String = new PrimitiveType(PrimitiveTypeKind.String);
    public static readonly PrimitiveType TimeOfDay = new PrimitiveType(PrimitiveTypeKind.TimeOfDay);
}
