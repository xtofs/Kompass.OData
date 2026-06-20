namespace Kompass.CsdlEdm;

using System.Xml;
using Kompass.CsdlEdm.Csdl;

/// <summary>
/// Parses CSDL XML documents into the syntactic <see cref="CsdlDocument"/> model.
/// Uses streaming <see cref="XmlReader"/> for efficient parsing.
/// </summary>
public static class CsdlXmlReader
{
    private const string EdmxNamespace = "http://docs.oasis-open.org/odata/ns/edmx";
    private const string EdmNamespace = "http://docs.oasis-open.org/odata/ns/edm";

    public static CsdlDocument Read(string xml)
    {
        using var reader = new StringReader(xml);
        return Read(reader);
    }

    public static CsdlDocument Read(TextReader textReader)
    {
        using var reader = XmlReader.Create(textReader, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
        });

        var doc = new CsdlDocument();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Edmx")
            {
                doc.Edmx = ReadEdmx(reader);
            }
        }

        return doc;
    }

    public static CsdlDocument Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return Read(reader);
    }

    private static Edmx ReadEdmx(XmlReader reader)
    {
        var edmx = new Edmx
        {
            Version = reader.GetAttribute("Version"),
        };

        if (reader.IsEmptyElement)
        {
            return edmx;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Reference":
                    edmx.References.Add(ReadReference(reader));
                    break;
                case "DataServices":
                    ReadDataServices(reader, edmx);
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return edmx;
    }

    private static Reference ReadReference(XmlReader reader)
    {
        var reference = new Reference
        {
            Uri = reader.GetAttribute("Uri") ?? "",
        };

        if (reader.IsEmptyElement)
        {
            return reference;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Include":
                    reference.Includes.Add(ReadInclude(reader));
                    break;
                case "IncludeAnnotations":
                    reference.IncludeAnnotations.Add(ReadIncludeAnnotations(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return reference;
    }

    private static Include ReadInclude(XmlReader reader)
    {
        var include = new Include
        {
            Namespace = reader.GetAttribute("Namespace") ?? "",
            Alias = reader.GetAttribute("Alias"),
        };

        if (reader.IsEmptyElement)
        {
            return include;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                include.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return include;
    }

    private static IncludeAnnotations ReadIncludeAnnotations(XmlReader reader)
    {
        var ia = new IncludeAnnotations
        {
            TermNamespace = reader.GetAttribute("TermNamespace") ?? "",
            Qualifier = reader.GetAttribute("Qualifier"),
            TargetNamespace = reader.GetAttribute("TargetNamespace"),
        };

        if (!reader.IsEmptyElement)
        {
            reader.Skip();
        }

        return ia;
    }

    private static void ReadDataServices(XmlReader reader, Edmx edmx)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Schema")
            {
                edmx.Schemas.Add(ReadSchema(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }
    }

    private static Schema ReadSchema(XmlReader reader)
    {
        var schema = new Schema
        {
            Namespace = reader.GetAttribute("Namespace") ?? "",
            Alias = reader.GetAttribute("Alias"),
        };

        if (reader.IsEmptyElement)
        {
            return schema;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "EntityType":
                    var et = ReadEntityType(reader);
                    schema.Elements.Add(et);
                    break;
                case "ComplexType":
                    var ct = ReadComplexType(reader);
                    schema.Elements.Add(ct);
                    break;
                case "EnumType":
                    var enumT = ReadEnumType(reader);
                    schema.Elements.Add(enumT);
                    break;
                case "TypeDefinition":
                    var td = ReadTypeDefinition(reader);
                    schema.Elements.Add(td);
                    break;
                case "Term":
                    var term = ReadTerm(reader);
                    schema.Elements.Add(term);
                    break;
                case "Function":
                    var func = ReadFunction(reader);
                    schema.Elements.Add(func);
                    break;
                case "Action":
                    var action = ReadAction(reader);
                    schema.Elements.Add(action);
                    break;
                case "EntityContainer":
                    var container = ReadEntityContainer(reader);
                    schema.Elements.Add(container);
                    break;
                case "Annotations":
                    ReadExternalAnnotations(reader, schema);
                    break;
                case "Annotation":
                    schema.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return schema;
    }

    private static Csdl.EntityType ReadEntityType(XmlReader reader)
    {
        var et = new Csdl.EntityType
        {
            Name = reader.GetAttribute("Name") ?? "",
            BaseType = reader.GetAttribute("BaseType"),
            Abstract = ParseOptionalBool(reader.GetAttribute("Abstract")),
            OpenType = ParseOptionalBool(reader.GetAttribute("OpenType")),
            HasStream = ParseOptionalBool(reader.GetAttribute("HasStream")),
        };

        if (reader.IsEmptyElement)
        {
            return et;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Key":
                    et.Key = ReadKey(reader);
                    break;
                case "Property":
                    et.Properties.Add(ReadProperty(reader));
                    break;
                case "NavigationProperty":
                    et.NavigationProperties.Add(ReadNavigationProperty(reader));
                    break;
                case "Annotation":
                    et.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return et;
    }

    private static Csdl.ComplexType ReadComplexType(XmlReader reader)
    {
        var ct = new Csdl.ComplexType
        {
            Name = reader.GetAttribute("Name") ?? "",
            BaseType = reader.GetAttribute("BaseType"),
            Abstract = ParseOptionalBool(reader.GetAttribute("Abstract")),
            OpenType = ParseOptionalBool(reader.GetAttribute("OpenType")),
        };

        if (reader.IsEmptyElement)
        {
            return ct;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Property":
                    ct.Properties.Add(ReadProperty(reader));
                    break;
                case "NavigationProperty":
                    ct.NavigationProperties.Add(ReadNavigationProperty(reader));
                    break;
                case "Annotation":
                    ct.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return ct;
    }

    private static Key ReadKey(XmlReader reader)
    {
        var key = new Key();

        if (reader.IsEmptyElement)
        {
            return key;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "PropertyRef")
            {
                key.PropertyRefs.Add(new PropertyRef { Name = reader.GetAttribute("Name") ?? "" });
                if (!reader.IsEmptyElement)
                {
                    reader.Skip();
                }
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return key;
    }

    private static Csdl.Property ReadProperty(XmlReader reader)
    {
        var (typeName, isCollection) = ParseTypeAttribute(reader.GetAttribute("Type"));

        var prop = new Csdl.Property
        {
            Name = reader.GetAttribute("Name") ?? "",
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = ParseOptionalBool(reader.GetAttribute("Nullable")),
            MaxLength = MaxLengthFacet.Parse(reader.GetAttribute("MaxLength")),
            Precision = reader.GetAttribute("Precision"),
            Scale = ScaleFacet.Parse(reader.GetAttribute("Scale")),
            Srid = SridFacet.Parse(reader.GetAttribute("SRID")),
            Unicode = ParseOptionalBool(reader.GetAttribute("Unicode")),
            DefaultValue = reader.GetAttribute("DefaultValue"),
        };

        if (reader.IsEmptyElement)
        {
            return prop;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                prop.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return prop;
    }

    private static Csdl.NavigationProperty ReadNavigationProperty(XmlReader reader)
    {
        var (typeName, isCollection) = ParseTypeAttribute(reader.GetAttribute("Type"));

        var nav = new Csdl.NavigationProperty
        {
            Name = reader.GetAttribute("Name") ?? "",
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = ParseOptionalBool(reader.GetAttribute("Nullable")),
            Partner = reader.GetAttribute("Partner"),
            ContainsTarget = ParseOptionalBool(reader.GetAttribute("ContainsTarget")),
        };

        if (reader.IsEmptyElement)
        {
            return nav;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "OnDelete":
                    nav.OnDelete = OnDeleteActionExtensions.Parse(reader.GetAttribute("Action"));
                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                    }
                    break;
                case "ReferentialConstraint":
                    nav.ReferentialConstraints.Add(ReadReferentialConstraint(reader));
                    break;
                case "Annotation":
                    nav.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return nav;
    }

    private static Csdl.ReferentialConstraint ReadReferentialConstraint(XmlReader reader)
    {
        var rc = new Csdl.ReferentialConstraint
        {
            Property = reader.GetAttribute("Property") ?? "",
            ReferencedProperty = reader.GetAttribute("ReferencedProperty") ?? "",
        };

        if (reader.IsEmptyElement)
        {
            return rc;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                rc.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return rc;
    }

    private static Csdl.EnumType ReadEnumType(XmlReader reader)
    {
        var enumType = new Csdl.EnumType
        {
            Name = reader.GetAttribute("Name") ?? "",
            UnderlyingType = reader.GetAttribute("UnderlyingType"),
            IsFlags = ParseOptionalBool(reader.GetAttribute("IsFlags")),
        };

        if (reader.IsEmptyElement)
        {
            return enumType;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Member":
                    var member = new Csdl.EnumMember
                    {
                        Name = reader.GetAttribute("Name") ?? "",
                    };
                    var valueStr = reader.GetAttribute("Value");
                    if (valueStr is not null && long.TryParse(valueStr, out var value))
                    {
                        member.Value = value;
                    }
                    if (!reader.IsEmptyElement)
                    {
                        // Read potential annotations on the member
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                break;
                            }

                            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
                            {
                                member.Annotations.Add(ReadAnnotation(reader));
                            }
                            else if (reader.NodeType == XmlNodeType.Element)
                            {
                                SkipElement(reader);
                            }
                        }
                    }
                    enumType.Members.Add(member);
                    break;
                case "Annotation":
                    enumType.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return enumType;
    }

    private static TypeDefinition ReadTypeDefinition(XmlReader reader)
    {
        var td = new TypeDefinition
        {
            Name = reader.GetAttribute("Name") ?? "",
            UnderlyingType = reader.GetAttribute("UnderlyingType") ?? "",
            MaxLength = MaxLengthFacet.Parse(reader.GetAttribute("MaxLength")),
            Precision = reader.GetAttribute("Precision"),
            Scale = ScaleFacet.Parse(reader.GetAttribute("Scale")),
            Srid = SridFacet.Parse(reader.GetAttribute("SRID")),
            Unicode = ParseOptionalBool(reader.GetAttribute("Unicode")),
        };

        if (reader.IsEmptyElement)
        {
            return td;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                td.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return td;
    }

    private static Csdl.Term ReadTerm(XmlReader reader)
    {
        var (typeName, isCollection) = ParseTypeAttribute(reader.GetAttribute("Type"));
        var appliesTo = reader.GetAttribute("AppliesTo");

        var term = new Csdl.Term
        {
            Name = reader.GetAttribute("Name") ?? "",
            TypeName = typeName,
            IsCollection = isCollection,
            BaseTerm = reader.GetAttribute("BaseTerm"),
            DefaultValue = reader.GetAttribute("DefaultValue"),
            Nullable = ParseOptionalBool(reader.GetAttribute("Nullable")),
            MaxLength = MaxLengthFacet.Parse(reader.GetAttribute("MaxLength")),
            Precision = reader.GetAttribute("Precision"),
            Scale = ScaleFacet.Parse(reader.GetAttribute("Scale")),
            Srid = SridFacet.Parse(reader.GetAttribute("SRID")),
            Unicode = ParseOptionalBool(reader.GetAttribute("Unicode")),
        };

        if (appliesTo is not null)
        {
            term.AppliesTo.AddRange(appliesTo.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        if (reader.IsEmptyElement)
        {
            return term;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                term.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return term;
    }

    private static Csdl.Function ReadFunction(XmlReader reader)
    {
        var func = new Csdl.Function
        {
            Name = reader.GetAttribute("Name") ?? "",
            IsBound = ParseOptionalBool(reader.GetAttribute("IsBound")),
            IsComposable = ParseOptionalBool(reader.GetAttribute("IsComposable")),
            EntitySetPath = reader.GetAttribute("EntitySetPath"),
        };

        if (reader.IsEmptyElement)
        {
            return func;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Parameter":
                    func.Parameters.Add(ReadParameter(reader));
                    break;
                case "ReturnType":
                    func.ReturnType = ReadReturnType(reader);
                    break;
                case "Annotation":
                    func.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return func;
    }

    private static Csdl.Action ReadAction(XmlReader reader)
    {
        var action = new Csdl.Action
        {
            Name = reader.GetAttribute("Name") ?? "",
            IsBound = ParseOptionalBool(reader.GetAttribute("IsBound")),
            EntitySetPath = reader.GetAttribute("EntitySetPath"),
        };

        if (reader.IsEmptyElement)
        {
            return action;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "Parameter":
                    action.Parameters.Add(ReadParameter(reader));
                    break;
                case "ReturnType":
                    action.ReturnType = ReadReturnType(reader);
                    break;
                case "Annotation":
                    action.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return action;
    }

    private static Csdl.Parameter ReadParameter(XmlReader reader)
    {
        var (typeName, isCollection) = ParseTypeAttribute(reader.GetAttribute("Type"));

        var param = new Csdl.Parameter
        {
            Name = reader.GetAttribute("Name") ?? "",
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = ParseOptionalBool(reader.GetAttribute("Nullable")),
            MaxLength = MaxLengthFacet.Parse(reader.GetAttribute("MaxLength")),
            Precision = reader.GetAttribute("Precision"),
            Scale = ScaleFacet.Parse(reader.GetAttribute("Scale")),
            Srid = SridFacet.Parse(reader.GetAttribute("SRID")),
            Unicode = ParseOptionalBool(reader.GetAttribute("Unicode")),
            DefaultValue = reader.GetAttribute("DefaultValue"),
        };

        if (reader.IsEmptyElement)
        {
            return param;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                param.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return param;
    }

    private static Csdl.ReturnType ReadReturnType(XmlReader reader)
    {
        var (typeName, isCollection) = ParseTypeAttribute(reader.GetAttribute("Type"));

        var rt = new Csdl.ReturnType
        {
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = ParseOptionalBool(reader.GetAttribute("Nullable")),
            MaxLength = MaxLengthFacet.Parse(reader.GetAttribute("MaxLength")),
            Precision = reader.GetAttribute("Precision"),
            Scale = ScaleFacet.Parse(reader.GetAttribute("Scale")),
            Srid = SridFacet.Parse(reader.GetAttribute("SRID")),
            Unicode = ParseOptionalBool(reader.GetAttribute("Unicode")),
        };

        if (reader.IsEmptyElement)
        {
            return rt;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                rt.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return rt;
    }

    private static Csdl.EntityContainer ReadEntityContainer(XmlReader reader)
    {
        var container = new Csdl.EntityContainer
        {
            Name = reader.GetAttribute("Name") ?? "",
            Extends = reader.GetAttribute("Extends"),
        };

        if (reader.IsEmptyElement)
        {
            return container;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "EntitySet":
                    container.EntitySets.Add(ReadEntitySet(reader));
                    break;
                case "Singleton":
                    container.Singletons.Add(ReadSingleton(reader));
                    break;
                case "FunctionImport":
                    container.FunctionImports.Add(ReadFunctionImport(reader));
                    break;
                case "ActionImport":
                    container.ActionImports.Add(ReadActionImport(reader));
                    break;
                case "Annotation":
                    container.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return container;
    }

    private static Csdl.EntitySet ReadEntitySet(XmlReader reader)
    {
        var es = new Csdl.EntitySet
        {
            Name = reader.GetAttribute("Name") ?? "",
            EntityType = reader.GetAttribute("EntityType"),
            IncludeInServiceDocument = ParseOptionalBool(reader.GetAttribute("IncludeInServiceDocument")),
        };

        if (reader.IsEmptyElement)
        {
            return es;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "NavigationPropertyBinding":
                    es.NavigationPropertyBindings.Add(new Csdl.NavigationPropertyBinding
                    {
                        Path = reader.GetAttribute("Path") ?? "",
                        Target = reader.GetAttribute("Target") ?? "",
                    });
                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                    }
                    break;
                case "Annotation":
                    es.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return es;
    }

    private static Csdl.Singleton ReadSingleton(XmlReader reader)
    {
        var singleton = new Csdl.Singleton
        {
            Name = reader.GetAttribute("Name") ?? "",
            TypeName = reader.GetAttribute("Type"),
            IncludeInServiceDocument = ParseOptionalBool(reader.GetAttribute("IncludeInServiceDocument")),
        };

        if (reader.IsEmptyElement)
        {
            return singleton;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "NavigationPropertyBinding":
                    singleton.NavigationPropertyBindings.Add(new Csdl.NavigationPropertyBinding
                    {
                        Path = reader.GetAttribute("Path") ?? "",
                        Target = reader.GetAttribute("Target") ?? "",
                    });
                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                    }
                    break;
                case "Annotation":
                    singleton.Annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return singleton;
    }

    private static Csdl.FunctionImport ReadFunctionImport(XmlReader reader)
    {
        var fi = new Csdl.FunctionImport
        {
            Name = reader.GetAttribute("Name") ?? "",
            Function = reader.GetAttribute("Function"),
            EntitySet = reader.GetAttribute("EntitySet"),
            IncludeInServiceDocument = ParseOptionalBool(reader.GetAttribute("IncludeInServiceDocument")),
        };

        if (reader.IsEmptyElement)
        {
            return fi;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                fi.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return fi;
    }

    private static Csdl.ActionImport ReadActionImport(XmlReader reader)
    {
        var ai = new Csdl.ActionImport
        {
            Name = reader.GetAttribute("Name") ?? "",
            Action = reader.GetAttribute("Action"),
            EntitySet = reader.GetAttribute("EntitySet"),
            IncludeInServiceDocument = ParseOptionalBool(reader.GetAttribute("IncludeInServiceDocument")),
        };

        if (reader.IsEmptyElement)
        {
            return ai;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                ai.Annotations.Add(ReadAnnotation(reader));
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }

        return ai;
    }

    private static void ReadExternalAnnotations(XmlReader reader, Schema schema)
    {
        // <Annotations Target="..."> contains Annotation children
        if (reader.IsEmptyElement)
        {
            return;
        }

        var target = reader.GetAttribute("Target");

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Annotation")
            {
                var annotation = ReadAnnotation(reader);
                annotation.Target ??= target;
                schema.Annotations.Add(annotation);
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                SkipElement(reader);
            }
        }
    }

    private static Annotation ReadAnnotation(XmlReader reader)
    {
        var annotation = new Annotation
        {
            Term = reader.GetAttribute("Term") ?? "",
            Qualifier = reader.GetAttribute("Qualifier"),
        };

        // Check for inline constant expression attributes
        annotation.Expression = TryReadInlineAnnotationExpression(reader);

        if (reader.IsEmptyElement)
        {
            // If no inline expression, default to Bool(true)
            annotation.Expression ??= new CsdlAnnotationExpression.Bool(true);
            return annotation;
        }

        // If we already have an inline expression, skip children but still consume the element
        if (annotation.Expression is not null)
        {
            reader.Skip();
            return annotation;
        }

        // Read nested expression
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                annotation.Expression = ReadAnnotationExpression(reader);
                break;
            }
        }

        // Default to Bool(true) if no expression was found
        annotation.Expression ??= new CsdlAnnotationExpression.Bool(true);

        // Consume remaining children
        if (reader.NodeType != XmlNodeType.EndElement)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    SkipElement(reader);
                }
            }
        }

        return annotation;
    }

    private static CsdlAnnotationExpression? TryReadInlineAnnotationExpression(XmlReader reader)
    {
        string?[] inlineAttrs =
        [
            reader.GetAttribute("String"),
            reader.GetAttribute("Bool"),
            reader.GetAttribute("Int"),
            reader.GetAttribute("Float"),
            reader.GetAttribute("Decimal"),
            reader.GetAttribute("Path"),
            reader.GetAttribute("PropertyPath"),
            reader.GetAttribute("NavigationPropertyPath"),
            reader.GetAttribute("AnnotationPath"),
            reader.GetAttribute("EnumMember"),
            reader.GetAttribute("Date"),
            reader.GetAttribute("DateTimeOffset"),
            reader.GetAttribute("Duration"),
            reader.GetAttribute("Guid"),
            reader.GetAttribute("TimeOfDay"),
            reader.GetAttribute("Binary"),
        ];

        string?[] attrNames =
        [
            "String", "Bool", "Int", "Float", "Decimal",
            "Path", "PropertyPath", "NavigationPropertyPath", "AnnotationPath",
            "EnumMember", "Date", "DateTimeOffset", "Duration", "Guid", "TimeOfDay", "Binary",
        ];

        for (var i = 0; i < inlineAttrs.Length; i++)
        {
            if (inlineAttrs[i] is not null)
            {
                return ConstantToExpression(attrNames[i]!, inlineAttrs[i]!);
            }
        }

        return null;
    }

    private static CsdlAnnotationExpression ConstantToExpression(string kind, string value)
    {
        return kind switch
        {
            "String" => new CsdlAnnotationExpression.StringExpr(value),
            "Bool" => new CsdlAnnotationExpression.Bool(bool.Parse(value)),
            "Int" => new CsdlAnnotationExpression.Int(long.Parse(value)),
            "Float" => new CsdlAnnotationExpression.Float(double.Parse(value)),
            "Decimal" => new CsdlAnnotationExpression.Decimal(value),
            "Path" => new CsdlAnnotationExpression.Path(value),
            "PropertyPath" => new CsdlAnnotationExpression.PropertyPath(value),
            "NavigationPropertyPath" => new CsdlAnnotationExpression.NavigationPropertyPath(value),
            "AnnotationPath" => new CsdlAnnotationExpression.AnnotationPath(value),
            "EnumMember" => new CsdlAnnotationExpression.EnumMemberExpr(value),
            "Date" => new CsdlAnnotationExpression.Date(value),
            "DateTimeOffset" => new CsdlAnnotationExpression.DateTimeOffset(value),
            "Duration" => new CsdlAnnotationExpression.Duration(value),
            "Guid" => new CsdlAnnotationExpression.Guid(value),
            "TimeOfDay" => new CsdlAnnotationExpression.TimeOfDay(value),
            "Binary" => new CsdlAnnotationExpression.Binary(Convert.FromBase64String(value)),
            _ => new CsdlAnnotationExpression.StringExpr(value),
        };
    }

    private static CsdlAnnotationExpression ReadAnnotationExpression(XmlReader reader)
    {
        var name = reader.LocalName;

        return name switch
        {
            "String" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.StringExpr(v)),
            "Bool" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Bool(bool.Parse(v))),
            "Int" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Int(long.Parse(v))),
            "Float" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Float(double.Parse(v))),
            "Decimal" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Decimal(v)),
            "Date" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Date(v)),
            "DateTimeOffset" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.DateTimeOffset(v)),
            "Duration" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Duration(v)),
            "Guid" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Guid(v)),
            "TimeOfDay" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.TimeOfDay(v)),
            "Binary" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Binary(Convert.FromBase64String(v))),
            "EnumMember" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.EnumMemberExpr(v)),
            "Path" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.Path(v)),
            "PropertyPath" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.PropertyPath(v)),
            "NavigationPropertyPath" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.NavigationPropertyPath(v)),
            "AnnotationPath" => ReadTextExpression(reader, v => new CsdlAnnotationExpression.AnnotationPath(v)),
            "Null" => ReadNullExpression(reader),
            "Record" => ReadRecordExpression(reader),
            "Collection" => ReadCollectionExpression(reader),
            "If" => ReadIfExpression(reader),
            "Not" => ReadNotExpression(reader),
            "And" or "Or" or "Eq" or "Ne" or "Gt" or "Ge" or "Lt" or "Le"
                => ReadBinaryExpression(reader, name),
            "Apply" => ReadApplyExpression(reader),
            "Cast" => ReadCastExpression(reader),
            "IsOf" => ReadIsOfExpression(reader),
            "LabeledElement" => ReadLabeledElementExpression(reader),
            "LabeledElementReference" => ReadLabeledElementReferenceExpression(reader),
            "UrlRef" => ReadUrlRefExpression(reader),
            _ => ReadUnknownExpression(reader),
        };
    }

    private static CsdlAnnotationExpression ReadTextExpression(
        XmlReader reader,
        Func<string, CsdlAnnotationExpression> factory)
    {
        if (reader.IsEmptyElement)
        {
            return factory("");
        }

        var text = reader.ReadElementContentAsString();
        return factory(text);
    }

    private static CsdlAnnotationExpression ReadNullExpression(XmlReader reader)
    {
        if (!reader.IsEmptyElement)
        {
            reader.Skip();
        }

        return new CsdlAnnotationExpression.Null();
    }

    private static CsdlAnnotationExpression ReadRecordExpression(XmlReader reader)
    {
        var type = reader.GetAttribute("Type");
        var properties = new List<AnnotationPropertyValue>();
        var annotations = new List<Annotation>();

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Record { Type = type, Properties = properties, Annotations = annotations };
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "PropertyValue":
                    properties.Add(ReadPropertyValue(reader));
                    break;
                case "Annotation":
                    annotations.Add(ReadAnnotation(reader));
                    break;
                default:
                    SkipElement(reader);
                    break;
            }
        }

        return new CsdlAnnotationExpression.Record { Type = type, Properties = properties, Annotations = annotations };
    }

    private static AnnotationPropertyValue ReadPropertyValue(XmlReader reader)
    {
        var pv = new AnnotationPropertyValue
        {
            Property = reader.GetAttribute("Property") ?? "",
        };

        // Check for inline expression attribute
        pv.Value = TryReadInlineAnnotationExpression(reader);

        if (reader.IsEmptyElement)
        {
            return pv;
        }

        if (pv.Value is not null)
        {
            // Skip children but consume the element
            reader.Skip();
            return pv;
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.LocalName == "Annotation")
            {
                pv.Annotations.Add(ReadAnnotation(reader));
            }
            else if (pv.Value is null)
            {
                pv.Value = ReadAnnotationExpression(reader);
            }
            else
            {
                SkipElement(reader);
            }
        }

        return pv;
    }

    private static CsdlAnnotationExpression ReadCollectionExpression(XmlReader reader)
    {
        var items = new List<CsdlAnnotationExpression>();

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Collection(items);
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                items.Add(ReadAnnotationExpression(reader));
            }
        }

        return new CsdlAnnotationExpression.Collection(items);
    }

    private static CsdlAnnotationExpression ReadIfExpression(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Null();
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.If
        {
            Test = children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null(),
            Then = children.Count > 1 ? children[1] : new CsdlAnnotationExpression.Null(),
            Else = children.Count > 2 ? children[2] : null,
        };
    }

    private static CsdlAnnotationExpression ReadNotExpression(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Null();
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.Not(
            children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null());
    }

    private static CsdlAnnotationExpression ReadBinaryExpression(XmlReader reader, string opName)
    {
        var op = opName switch
        {
            "And" => AnnotationBinaryOperator.And,
            "Or" => AnnotationBinaryOperator.Or,
            "Eq" => AnnotationBinaryOperator.Eq,
            "Ne" => AnnotationBinaryOperator.Ne,
            "Gt" => AnnotationBinaryOperator.Gt,
            "Ge" => AnnotationBinaryOperator.Ge,
            "Lt" => AnnotationBinaryOperator.Lt,
            "Le" => AnnotationBinaryOperator.Le,
            _ => AnnotationBinaryOperator.Eq,
        };

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Null();
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.BinaryExpr
        {
            Op = op,
            Lhs = children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null(),
            Rhs = children.Count > 1 ? children[1] : new CsdlAnnotationExpression.Null(),
        };
    }

    private static CsdlAnnotationExpression ReadApplyExpression(XmlReader reader)
    {
        var function = reader.GetAttribute("Function") ?? "";

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Apply { Function = function };
        }

        var args = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.Apply { Function = function, Args = args };
    }

    private static CsdlAnnotationExpression ReadCastExpression(XmlReader reader)
    {
        var type = reader.GetAttribute("Type") ?? "";

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.Cast
            {
                Type = type,
                Expr = new CsdlAnnotationExpression.Null(),
            };
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.Cast
        {
            Type = type,
            Expr = children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null(),
        };
    }

    private static CsdlAnnotationExpression ReadIsOfExpression(XmlReader reader)
    {
        var type = reader.GetAttribute("Type") ?? "";

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.IsOf
            {
                Type = type,
                Expr = new CsdlAnnotationExpression.Null(),
            };
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.IsOf
        {
            Type = type,
            Expr = children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null(),
        };
    }

    private static CsdlAnnotationExpression ReadLabeledElementExpression(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";

        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.LabeledElement
            {
                Name = name,
                Expr = new CsdlAnnotationExpression.Null(),
            };
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.LabeledElement
        {
            Name = name,
            Expr = children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null(),
        };
    }

    private static CsdlAnnotationExpression ReadLabeledElementReferenceExpression(XmlReader reader)
    {
        var name = reader.GetAttribute("Name") ?? "";
        if (!reader.IsEmptyElement)
        {
            reader.Skip();
        }

        return new CsdlAnnotationExpression.LabeledElementReference(name);
    }

    private static CsdlAnnotationExpression ReadUrlRefExpression(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            return new CsdlAnnotationExpression.UrlRef(new CsdlAnnotationExpression.Null());
        }

        var children = ReadChildExpressions(reader);
        return new CsdlAnnotationExpression.UrlRef(
            children.Count > 0 ? children[0] : new CsdlAnnotationExpression.Null());
    }

    private static CsdlAnnotationExpression ReadUnknownExpression(XmlReader reader)
    {
        if (!reader.IsEmptyElement)
        {
            reader.Skip();
        }

        return new CsdlAnnotationExpression.Null();
    }

    private static List<CsdlAnnotationExpression> ReadChildExpressions(XmlReader reader)
    {
        var exprs = new List<CsdlAnnotationExpression>();

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                exprs.Add(ReadAnnotationExpression(reader));
            }
        }

        return exprs;
    }

    /// <summary>
    /// Parses a type attribute value, handling the Collection(Foo) wrapper.
    /// Returns (typeName, isCollection).
    /// </summary>
    internal static (string? TypeName, bool IsCollection) ParseTypeAttribute(string? raw)
    {
        if (raw is null)
        {
            return (null, false);
        }

        if (raw.StartsWith("Collection(", StringComparison.Ordinal) && raw.EndsWith(')'))
        {
            var inner = raw.Substring(11, raw.Length - 12);
            return (inner, true);
        }

        return (raw, false);
    }

    private static bool? ParseOptionalBool(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return bool.TryParse(value, out var result) ? result : null;
    }

    private static void SkipElement(XmlReader reader)
    {
        if (!reader.IsEmptyElement)
        {
            reader.Skip();
        }
    }
}
