namespace Kompass.CsdlEdm;

using System.Text.Json;
using Kompass.CsdlEdm.Csdl;

/// <summary>
/// Parses CSDL JSON documents (OASIS JSON format) into the syntactic <see cref="CsdlDocument"/> model.
/// Uses <see cref="System.Text.Json.JsonDocument"/> for DOM-based parsing.
/// </summary>
public static class CsdlJsonReader
{
    public static CsdlDocument Read(string json)
    {
        using var jsonDoc = JsonDocument.Parse(json);
        return ReadDocument(jsonDoc.RootElement);
    }

    public static CsdlDocument Read(Stream stream)
    {
        using var jsonDoc = JsonDocument.Parse(stream);
        return ReadDocument(jsonDoc.RootElement);
    }

    private static CsdlDocument ReadDocument(JsonElement root)
    {
        var doc = new CsdlDocument();

        if (root.ValueKind != JsonValueKind.Object)
        {
            return doc;
        }

        var edmx = new Edmx();

        if (root.TryGetProperty("$Version", out var versionElem))
        {
            edmx.Version = versionElem.GetString();
        }

        // References
        if (root.TryGetProperty("$Reference", out var refsElem) && refsElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var refProp in refsElem.EnumerateObject())
            {
                edmx.References.Add(ReadReference(refProp.Name, refProp.Value));
            }
        }

        // Schemas are all other top-level properties that don't start with $
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.StartsWith('$'))
            {
                continue;
            }

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                edmx.Schemas.Add(ReadSchema(prop.Name, prop.Value));
            }
        }

        doc.Edmx = edmx;
        return doc;
    }

    private static Reference ReadReference(string uri, JsonElement elem)
    {
        var reference = new Reference { Uri = uri };

        if (elem.ValueKind != JsonValueKind.Object)
        {
            return reference;
        }

        if (elem.TryGetProperty("$Include", out var includesElem) && includesElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var inc in includesElem.EnumerateArray())
            {
                reference.Includes.Add(ReadInclude(inc));
            }
        }

        if (elem.TryGetProperty("$IncludeAnnotations", out var iaElem) && iaElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var ia in iaElem.EnumerateArray())
            {
                reference.IncludeAnnotations.Add(ReadIncludeAnnotations(ia));
            }
        }

        return reference;
    }

    private static Include ReadInclude(JsonElement elem)
    {
        var include = new Include
        {
            Namespace = GetStringProp(elem, "$Namespace") ?? "",
            Alias = GetStringProp(elem, "$Alias"),
        };

        ReadAnnotationsFromObject(elem, include.Annotations);
        return include;
    }

    private static IncludeAnnotations ReadIncludeAnnotations(JsonElement elem)
    {
        return new IncludeAnnotations
        {
            TermNamespace = GetStringProp(elem, "$TermNamespace") ?? "",
            Qualifier = GetStringProp(elem, "$Qualifier"),
            TargetNamespace = GetStringProp(elem, "$TargetNamespace"),
        };
    }

    private static Schema ReadSchema(string ns, JsonElement elem)
    {
        var schema = new Schema
        {
            Namespace = ns,
            Alias = GetStringProp(elem, "$Alias"),
        };

        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('$'))
            {
                continue;
            }

            if (prop.Name.StartsWith('@'))
            {
                schema.Annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = GetStringProp(prop.Value, "$Kind");

            switch (kind)
            {
                case "EntityType":
                    var et = ReadEntityType(prop.Name, prop.Value);
                    schema.Elements.Add(et);
                    break;
                case "ComplexType":
                    var ct = ReadComplexType(prop.Name, prop.Value);
                    schema.Elements.Add(ct);
                    break;
                case "EnumType":
                    var enumT = ReadEnumType(prop.Name, prop.Value);
                    schema.Elements.Add(enumT);
                    break;
                case "TypeDefinition":
                    var td = ReadTypeDefinition(prop.Name, prop.Value);
                    schema.Elements.Add(td);
                    break;
                case "Term":
                    var term = ReadTerm(prop.Name, prop.Value);
                    schema.Elements.Add(term);
                    break;
                case "Action":
                    var action = ReadAction(prop.Name, prop.Value);
                    schema.Elements.Add(action);
                    break;
                case "EntityContainer":
                    var container = ReadEntityContainer(prop.Name, prop.Value);
                    schema.Elements.Add(container);
                    break;
                default:
                    // Function is the default $Kind, or explicitly "Function"
                    if (kind == "Function" || kind is null)
                    {
                        // Check if it looks like a function (has $Parameter or $ReturnType)
                        if (prop.Value.TryGetProperty("$Parameter", out _) ||
                            prop.Value.TryGetProperty("$ReturnType", out _) ||
                            prop.Value.TryGetProperty("$IsBound", out _) ||
                            kind == "Function")
                        {
                            var func = ReadFunction(prop.Name, prop.Value);
                            schema.Elements.Add(func);
                        }
                    }
                    break;
            }
        }

        return schema;
    }

    private static Csdl.EntityType ReadEntityType(string name, JsonElement elem)
    {
        var et = new Csdl.EntityType
        {
            Name = name,
            BaseType = GetStringProp(elem, "$BaseType"),
            Abstract = GetBoolProp(elem, "$Abstract"),
            OpenType = GetBoolProp(elem, "$OpenType"),
            HasStream = GetBoolProp(elem, "$HasStream"),
        };

        // Key
        if (elem.TryGetProperty("$Key", out var keyElem) && keyElem.ValueKind == JsonValueKind.Array)
        {
            et.Key = new Key();
            foreach (var k in keyElem.EnumerateArray())
            {
                if (k.ValueKind == JsonValueKind.String)
                {
                    et.Key.PropertyRefs.Add(new PropertyRef { Name = k.GetString()! });
                }
                else if (k.ValueKind == JsonValueKind.Object)
                {
                    // Composite key alias form: { "alias": "propertyPath" }
                    foreach (var kp in k.EnumerateObject())
                    {
                        et.Key.PropertyRefs.Add(new PropertyRef { Name = kp.Value.GetString() ?? kp.Name });
                    }
                }
            }
        }

        // Properties and nav properties
        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('$') || prop.Name.StartsWith('@'))
            {
                if (prop.Name.StartsWith('@'))
                {
                    et.Annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
                }
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = GetStringProp(prop.Value, "$Kind");
            if (kind == "NavigationProperty")
            {
                et.NavigationProperties.Add(ReadNavigationProperty(prop.Name, prop.Value));
            }
            else
            {
                // Default kind is "Property"
                et.Properties.Add(ReadProperty(prop.Name, prop.Value));
            }
        }

        return et;
    }

    private static Csdl.ComplexType ReadComplexType(string name, JsonElement elem)
    {
        var ct = new Csdl.ComplexType
        {
            Name = name,
            BaseType = GetStringProp(elem, "$BaseType"),
            Abstract = GetBoolProp(elem, "$Abstract"),
            OpenType = GetBoolProp(elem, "$OpenType"),
        };

        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('$') || prop.Name.StartsWith('@'))
            {
                if (prop.Name.StartsWith('@'))
                {
                    ct.Annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
                }
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = GetStringProp(prop.Value, "$Kind");
            if (kind == "NavigationProperty")
            {
                ct.NavigationProperties.Add(ReadNavigationProperty(prop.Name, prop.Value));
            }
            else
            {
                ct.Properties.Add(ReadProperty(prop.Name, prop.Value));
            }
        }

        return ct;
    }

    private static Csdl.Property ReadProperty(string name, JsonElement elem)
    {
        var (typeName, isCollection) = ParseJsonType(elem);

        var prop = new Csdl.Property
        {
            Name = name,
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = GetBoolProp(elem, "$Nullable"),
            MaxLength = MaxLengthFacet.Parse(GetStringOrNumberProp(elem, "$MaxLength")),
            Precision = GetStringOrNumberProp(elem, "$Precision"),
            Scale = ScaleFacet.Parse(GetStringOrNumberProp(elem, "$Scale")),
            Srid = SridFacet.Parse(GetStringOrNumberProp(elem, "$SRID")),
            Unicode = GetBoolProp(elem, "$Unicode"),
            DefaultValue = GetStringProp(elem, "$DefaultValue"),
        };

        ReadAnnotationsFromObject(elem, prop.Annotations);
        return prop;
    }

    private static Csdl.NavigationProperty ReadNavigationProperty(string name, JsonElement elem)
    {
        var (typeName, isCollection) = ParseJsonType(elem);

        var nav = new Csdl.NavigationProperty
        {
            Name = name,
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = GetBoolProp(elem, "$Nullable"),
            Partner = GetStringProp(elem, "$Partner"),
            ContainsTarget = GetBoolProp(elem, "$ContainsTarget"),
        };

        if (elem.TryGetProperty("$OnDelete", out var onDeleteElem))
        {
            nav.OnDelete = OnDeleteActionExtensions.Parse(onDeleteElem.GetString());
        }

        if (elem.TryGetProperty("$ReferentialConstraint", out var rcElem) &&
            rcElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var rc in rcElem.EnumerateObject())
            {
                if (!rc.Name.StartsWith('@'))
                {
                    nav.ReferentialConstraints.Add(new Csdl.ReferentialConstraint
                    {
                        Property = rc.Name,
                        ReferencedProperty = rc.Value.GetString() ?? "",
                    });
                }
            }
        }

        ReadAnnotationsFromObject(elem, nav.Annotations);
        return nav;
    }

    private static Csdl.EnumType ReadEnumType(string name, JsonElement elem)
    {
        var enumType = new Csdl.EnumType
        {
            Name = name,
            UnderlyingType = GetStringProp(elem, "$UnderlyingType"),
            IsFlags = GetBoolProp(elem, "$IsFlags"),
        };

        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('$') || prop.Name.StartsWith('@'))
            {
                if (prop.Name.StartsWith('@'))
                {
                    enumType.Annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
                }
                continue;
            }

            var member = new Csdl.EnumMember { Name = prop.Name };
            if (prop.Value.ValueKind == JsonValueKind.Number)
            {
                member.Value = prop.Value.GetInt64();
            }

            enumType.Members.Add(member);
        }

        return enumType;
    }

    private static TypeDefinition ReadTypeDefinition(string name, JsonElement elem)
    {
        return new TypeDefinition
        {
            Name = name,
            UnderlyingType = GetStringProp(elem, "$UnderlyingType") ?? "",
            MaxLength = MaxLengthFacet.Parse(GetStringOrNumberProp(elem, "$MaxLength")),
            Precision = GetStringOrNumberProp(elem, "$Precision"),
            Scale = ScaleFacet.Parse(GetStringOrNumberProp(elem, "$Scale")),
            Srid = SridFacet.Parse(GetStringOrNumberProp(elem, "$SRID")),
            Unicode = GetBoolProp(elem, "$Unicode"),
        };
    }

    private static Csdl.Term ReadTerm(string name, JsonElement elem)
    {
        var (typeName, isCollection) = ParseJsonType(elem);
        var appliesTo = GetStringProp(elem, "$AppliesTo");

        var term = new Csdl.Term
        {
            Name = name,
            TypeName = typeName,
            IsCollection = isCollection,
            BaseTerm = GetStringProp(elem, "$BaseTerm"),
            DefaultValue = GetStringProp(elem, "$DefaultValue"),
            Nullable = GetBoolProp(elem, "$Nullable"),
            MaxLength = MaxLengthFacet.Parse(GetStringOrNumberProp(elem, "$MaxLength")),
            Precision = GetStringOrNumberProp(elem, "$Precision"),
            Scale = ScaleFacet.Parse(GetStringOrNumberProp(elem, "$Scale")),
            Srid = SridFacet.Parse(GetStringOrNumberProp(elem, "$SRID")),
            Unicode = GetBoolProp(elem, "$Unicode"),
        };

        if (appliesTo is not null)
        {
            term.AppliesTo.AddRange(appliesTo.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        ReadAnnotationsFromObject(elem, term.Annotations);
        return term;
    }

    private static Csdl.Function ReadFunction(string name, JsonElement elem)
    {
        return new Csdl.Function
        {
            Name = name,
            IsBound = GetBoolProp(elem, "$IsBound"),
            IsComposable = GetBoolProp(elem, "$IsComposable"),
            EntitySetPath = GetStringProp(elem, "$EntitySetPath"),
            Parameters = ReadParameters(elem),
            ReturnType = ReadReturnTypeFromJson(elem),
        };
    }

    private static Csdl.Action ReadAction(string name, JsonElement elem)
    {
        return new Csdl.Action
        {
            Name = name,
            IsBound = GetBoolProp(elem, "$IsBound"),
            EntitySetPath = GetStringProp(elem, "$EntitySetPath"),
            Parameters = ReadParameters(elem),
            ReturnType = ReadReturnTypeFromJson(elem),
        };
    }

    private static List<Csdl.Parameter> ReadParameters(JsonElement elem)
    {
        var parameters = new List<Csdl.Parameter>();

        if (elem.TryGetProperty("$Parameter", out var paramsElem) && paramsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in paramsElem.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var (typeName, isCollection) = ParseJsonType(p);

                parameters.Add(new Csdl.Parameter
                {
                    Name = GetStringProp(p, "$Name") ?? "",
                    TypeName = typeName,
                    IsCollection = isCollection,
                    Nullable = GetBoolProp(p, "$Nullable"),
                    MaxLength = MaxLengthFacet.Parse(GetStringOrNumberProp(p, "$MaxLength")),
                    Precision = GetStringOrNumberProp(p, "$Precision"),
                    Scale = ScaleFacet.Parse(GetStringOrNumberProp(p, "$Scale")),
                    Srid = SridFacet.Parse(GetStringOrNumberProp(p, "$SRID")),
                    Unicode = GetBoolProp(p, "$Unicode"),
                    DefaultValue = GetStringProp(p, "$DefaultValue"),
                });
            }
        }

        return parameters;
    }

    private static Csdl.ReturnType? ReadReturnTypeFromJson(JsonElement elem)
    {
        if (!elem.TryGetProperty("$ReturnType", out var rtElem) || rtElem.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var (typeName, isCollection) = ParseJsonType(rtElem);

        return new Csdl.ReturnType
        {
            TypeName = typeName,
            IsCollection = isCollection,
            Nullable = GetBoolProp(rtElem, "$Nullable"),
            MaxLength = MaxLengthFacet.Parse(GetStringOrNumberProp(rtElem, "$MaxLength")),
            Precision = GetStringOrNumberProp(rtElem, "$Precision"),
            Scale = ScaleFacet.Parse(GetStringOrNumberProp(rtElem, "$Scale")),
            Srid = SridFacet.Parse(GetStringOrNumberProp(rtElem, "$SRID")),
            Unicode = GetBoolProp(rtElem, "$Unicode"),
        };
    }

    private static Csdl.EntityContainer ReadEntityContainer(string name, JsonElement elem)
    {
        var container = new Csdl.EntityContainer
        {
            Name = name,
            Extends = GetStringProp(elem, "$Extends"),
        };

        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('$') || prop.Name.StartsWith('@'))
            {
                if (prop.Name.StartsWith('@'))
                {
                    container.Annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
                }
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var kind = GetStringProp(prop.Value, "$Kind");

            switch (kind)
            {
                case "Singleton":
                    container.Singletons.Add(ReadSingleton(prop.Name, prop.Value));
                    break;
                case "FunctionImport":
                    container.FunctionImports.Add(ReadFunctionImport(prop.Name, prop.Value));
                    break;
                case "ActionImport":
                    container.ActionImports.Add(ReadActionImport(prop.Name, prop.Value));
                    break;
                default:
                    // Default is EntitySet
                    container.EntitySets.Add(ReadEntitySet(prop.Name, prop.Value));
                    break;
            }
        }

        return container;
    }

    private static Csdl.EntitySet ReadEntitySet(string name, JsonElement elem)
    {
        var es = new Csdl.EntitySet
        {
            Name = name,
            EntityType = GetStringProp(elem, "$Type"),
            IncludeInServiceDocument = GetBoolProp(elem, "$IncludeInServiceDocument"),
        };

        if (elem.TryGetProperty("$NavigationPropertyBinding", out var nbElem) &&
            nbElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var nb in nbElem.EnumerateObject())
            {
                es.NavigationPropertyBindings.Add(new Csdl.NavigationPropertyBinding
                {
                    Path = nb.Name,
                    Target = nb.Value.GetString() ?? "",
                });
            }
        }

        ReadAnnotationsFromObject(elem, es.Annotations);
        return es;
    }

    private static Csdl.Singleton ReadSingleton(string name, JsonElement elem)
    {
        var singleton = new Csdl.Singleton
        {
            Name = name,
            TypeName = GetStringProp(elem, "$Type"),
            IncludeInServiceDocument = GetBoolProp(elem, "$IncludeInServiceDocument"),
        };

        if (elem.TryGetProperty("$NavigationPropertyBinding", out var nbElem) &&
            nbElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var nb in nbElem.EnumerateObject())
            {
                singleton.NavigationPropertyBindings.Add(new Csdl.NavigationPropertyBinding
                {
                    Path = nb.Name,
                    Target = nb.Value.GetString() ?? "",
                });
            }
        }

        ReadAnnotationsFromObject(elem, singleton.Annotations);
        return singleton;
    }

    private static Csdl.FunctionImport ReadFunctionImport(string name, JsonElement elem)
    {
        return new Csdl.FunctionImport
        {
            Name = name,
            Function = GetStringProp(elem, "$Function"),
            EntitySet = GetStringProp(elem, "$EntitySet"),
            IncludeInServiceDocument = GetBoolProp(elem, "$IncludeInServiceDocument"),
        };
    }

    private static Csdl.ActionImport ReadActionImport(string name, JsonElement elem)
    {
        return new Csdl.ActionImport
        {
            Name = name,
            Action = GetStringProp(elem, "$Action"),
            EntitySet = GetStringProp(elem, "$EntitySet"),
            IncludeInServiceDocument = GetBoolProp(elem, "$IncludeInServiceDocument"),
        };
    }

    // --- Annotation helpers ---

    private static void ReadAnnotationsFromObject(JsonElement elem, List<Annotation> annotations)
    {
        foreach (var prop in elem.EnumerateObject())
        {
            if (prop.Name.StartsWith('@'))
            {
                annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
            }
        }
    }

    private static Annotation ReadAnnotationFromKey(string key, JsonElement value)
    {
        // Key format: @Term or @Term#Qualifier
        var termKey = key.TrimStart('@');
        string? qualifier = null;

        var hashIdx = termKey.IndexOf('#');
        if (hashIdx >= 0)
        {
            qualifier = termKey.Substring(hashIdx + 1);
            termKey = termKey.Substring(0, hashIdx);
        }

        return new Annotation
        {
            Term = termKey,
            Qualifier = qualifier,
            Expression = JsonToAnnotationExpression(value),
        };
    }

    private static CsdlAnnotationExpression JsonToAnnotationExpression(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => new CsdlAnnotationExpression.StringExpr(value.GetString()!),
            JsonValueKind.True => new CsdlAnnotationExpression.Bool(true),
            JsonValueKind.False => new CsdlAnnotationExpression.Bool(false),
            JsonValueKind.Number when value.TryGetInt64(out var i) => new CsdlAnnotationExpression.Int(i),
            JsonValueKind.Number => new CsdlAnnotationExpression.Float(value.GetDouble()),
            JsonValueKind.Null => new CsdlAnnotationExpression.Null(),
            JsonValueKind.Array => new CsdlAnnotationExpression.Collection(
                value.EnumerateArray().Select(JsonToAnnotationExpression).ToList()),
            JsonValueKind.Object => JsonObjectToAnnotationExpression(value),
            _ => new CsdlAnnotationExpression.Null(),
        };
    }

    private static CsdlAnnotationExpression JsonObjectToAnnotationExpression(JsonElement obj)
    {
        // Check for specific expression types by $-prefixed keys
        if (obj.TryGetProperty("$Path", out var pathElem))
        {
            return new CsdlAnnotationExpression.Path(pathElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$PropertyPath", out var ppElem))
        {
            return new CsdlAnnotationExpression.PropertyPath(ppElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$NavigationPropertyPath", out var nppElem))
        {
            return new CsdlAnnotationExpression.NavigationPropertyPath(nppElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$AnnotationPath", out var apElem))
        {
            return new CsdlAnnotationExpression.AnnotationPath(apElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$EnumMember", out var emElem))
        {
            return new CsdlAnnotationExpression.EnumMemberExpr(emElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$If", out var ifElem) && ifElem.ValueKind == JsonValueKind.Array)
        {
            var items = ifElem.EnumerateArray().Select(JsonToAnnotationExpression).ToList();
            return new CsdlAnnotationExpression.If
            {
                Test = items.Count > 0 ? items[0] : new CsdlAnnotationExpression.Null(),
                Then = items.Count > 1 ? items[1] : new CsdlAnnotationExpression.Null(),
                Else = items.Count > 2 ? items[2] : null,
            };
        }

        if (obj.TryGetProperty("$Apply", out var applyElem) && applyElem.ValueKind == JsonValueKind.Array)
        {
            return new CsdlAnnotationExpression.Apply
            {
                Function = GetStringProp(obj, "$Function") ?? "",
                Args = applyElem.EnumerateArray().Select(JsonToAnnotationExpression).ToList(),
            };
        }

        if (obj.TryGetProperty("$Cast", out var castElem))
        {
            return new CsdlAnnotationExpression.Cast
            {
                Type = GetStringProp(obj, "$Type") ?? "",
                Expr = JsonToAnnotationExpression(castElem),
            };
        }

        if (obj.TryGetProperty("$IsOf", out var isOfElem))
        {
            return new CsdlAnnotationExpression.IsOf
            {
                Type = GetStringProp(obj, "$Type") ?? "",
                Expr = JsonToAnnotationExpression(isOfElem),
            };
        }

        if (obj.TryGetProperty("$LabeledElement", out var leElem))
        {
            return new CsdlAnnotationExpression.LabeledElement
            {
                Name = GetStringProp(obj, "$Name") ?? "",
                Expr = JsonToAnnotationExpression(leElem),
            };
        }

        if (obj.TryGetProperty("$LabeledElementReference", out var lerElem))
        {
            return new CsdlAnnotationExpression.LabeledElementReference(lerElem.GetString() ?? "");
        }

        if (obj.TryGetProperty("$UrlRef", out var urlRefElem))
        {
            return new CsdlAnnotationExpression.UrlRef(JsonToAnnotationExpression(urlRefElem));
        }

        if (obj.TryGetProperty("$Null", out _))
        {
            return new CsdlAnnotationExpression.Null();
        }

        // Default: treat as Record expression
        var type = GetStringProp(obj, "$Type");
        var properties = new List<AnnotationPropertyValue>();
        var annotations = new List<Annotation>();

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name.StartsWith('@'))
            {
                annotations.Add(ReadAnnotationFromKey(prop.Name, prop.Value));
            }
            else if (!prop.Name.StartsWith('$'))
            {
                properties.Add(new AnnotationPropertyValue
                {
                    Property = prop.Name,
                    Value = JsonToAnnotationExpression(prop.Value),
                });
            }
        }

        return new CsdlAnnotationExpression.Record
        {
            Type = type,
            Properties = properties,
            Annotations = annotations,
        };
    }

    // --- Type parsing helpers ---

    /// <summary>
    /// Parses the $Type and $Collection properties from a JSON element.
    /// JSON uses separate $Type and $Collection fields rather than Collection(Foo).
    /// </summary>
    private static (string? TypeName, bool IsCollection) ParseJsonType(JsonElement elem)
    {
        var typeName = GetStringProp(elem, "$Type");
        var isCollection = GetBoolProp(elem, "$Collection") ?? false;

        // Also handle Collection(Foo) form for compatibility
        if (typeName is not null && typeName.StartsWith("Collection(", StringComparison.Ordinal) && typeName.EndsWith(')'))
        {
            typeName = typeName.Substring(11, typeName.Length - 12);
            isCollection = true;
        }

        return (typeName, isCollection);
    }

    // --- Generic property accessors ---

    private static string? GetStringProp(JsonElement elem, string name)
    {
        if (elem.TryGetProperty(name, out var val) && val.ValueKind == JsonValueKind.String)
        {
            return val.GetString();
        }

        return null;
    }

    private static bool? GetBoolProp(JsonElement elem, string name)
    {
        if (elem.TryGetProperty(name, out var val))
        {
            return val.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(val.GetString(), out var b) ? b : null,
                _ => null,
            };
        }

        return null;
    }

    private static string? GetStringOrNumberProp(JsonElement elem, string name)
    {
        if (elem.TryGetProperty(name, out var val))
        {
            return val.ValueKind switch
            {
                JsonValueKind.String => val.GetString(),
                JsonValueKind.Number => val.GetRawText(),
                _ => null,
            };
        }

        return null;
    }
}
