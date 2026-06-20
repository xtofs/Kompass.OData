namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL Annotation element.
/// </summary>
public sealed class Annotation
{
    public required string Term { get; set; }
    public string? Qualifier { get; set; }
    public string? Target { get; set; }
    public CsdlAnnotationExpression? Expression { get; set; }
}

/// <summary>
/// A CSDL annotation expression — constant, path, or dynamic.
/// Modeled as a sealed hierarchy mirroring the Rust <c>CsdlAnnotationExpression</c> enum.
/// </summary>
public abstract class CsdlAnnotationExpression
{
    private CsdlAnnotationExpression() { }

    // Constant expressions
    public sealed class Binary(byte[] Value) : CsdlAnnotationExpression { public byte[] Value { get; } = Value; }
    public sealed class Bool(bool Value) : CsdlAnnotationExpression { public bool Value { get; } = Value; }
    public sealed class Date(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class DateTimeOffset(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class Decimal(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class Duration(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class EnumMemberExpr(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class Float(double Value) : CsdlAnnotationExpression { public double Value { get; } = Value; }
    public sealed class Guid(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class Int(long Value) : CsdlAnnotationExpression { public long Value { get; } = Value; }
    public sealed class StringExpr(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class TimeOfDay(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class Null : CsdlAnnotationExpression;

    // Path expressions
    public sealed class Path(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class PropertyPath(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class NavigationPropertyPath(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }
    public sealed class AnnotationPath(string Value) : CsdlAnnotationExpression { public string Value { get; } = Value; }

    // Dynamic expressions
    public sealed class Not(CsdlAnnotationExpression Operand) : CsdlAnnotationExpression { public CsdlAnnotationExpression Operand { get; } = Operand; }

    public sealed class BinaryExpr : CsdlAnnotationExpression
    {
        public required AnnotationBinaryOperator Op { get; init; }
        public required CsdlAnnotationExpression Lhs { get; init; }
        public required CsdlAnnotationExpression Rhs { get; init; }
    }

    public sealed class If : CsdlAnnotationExpression
    {
        public required CsdlAnnotationExpression Test { get; init; }
        public required CsdlAnnotationExpression Then { get; init; }
        public CsdlAnnotationExpression? Else { get; init; }
    }

    public sealed class Apply : CsdlAnnotationExpression
    {
        public required string Function { get; init; }
        public List<CsdlAnnotationExpression> Args { get; init; } = [];
    }

    public sealed class Cast : CsdlAnnotationExpression
    {
        public required string Type { get; init; }
        public required CsdlAnnotationExpression Expr { get; init; }
    }

    public sealed class IsOf : CsdlAnnotationExpression
    {
        public required string Type { get; init; }
        public required CsdlAnnotationExpression Expr { get; init; }
    }

    public sealed class Record : CsdlAnnotationExpression
    {
        public string? Type { get; init; }
        public List<AnnotationPropertyValue> Properties { get; init; } = [];
        public List<Annotation> Annotations { get; init; } = [];
    }

    public sealed class Collection(List<CsdlAnnotationExpression> Items) : CsdlAnnotationExpression
    {
        public List<CsdlAnnotationExpression> Items { get; } = Items;
    }

    public sealed class LabeledElement : CsdlAnnotationExpression
    {
        public required string Name { get; init; }
        public required CsdlAnnotationExpression Expr { get; init; }
    }

    public sealed class LabeledElementReference(string Name) : CsdlAnnotationExpression
    {
        public string Name { get; } = Name;
    }

    public sealed class UrlRef(CsdlAnnotationExpression Expr) : CsdlAnnotationExpression
    {
        public CsdlAnnotationExpression Expr { get; } = Expr;
    }
}

/// <summary>
/// Binary operators for annotation dynamic expressions.
/// </summary>
public enum AnnotationBinaryOperator
{
    And,
    Or,
    Eq,
    Ne,
    Gt,
    Ge,
    Lt,
    Le,
}

/// <summary>
/// A PropertyValue within a Record annotation expression.
/// </summary>
public sealed class AnnotationPropertyValue
{
    public required string Property { get; set; }
    public CsdlAnnotationExpression? Value { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}
