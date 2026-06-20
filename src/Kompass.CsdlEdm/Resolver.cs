namespace Kompass.CsdlEdm;

using Kompass.CsdlEdm.Csdl;
using Kompass.CsdlEdm.Edm;

/// <summary>
/// Resolves a syntactic <see cref="CsdlDocument"/> into a semantic <see cref="DocumentModel"/>.
/// Two-pass resolution:
/// 1. Register all schemas and named elements into lookup dictionaries
/// 2. Resolve all string references into object references
/// </summary>
public static class Resolver
{
    private static readonly Dictionary<string, Edm.PrimitiveType> PrimitiveTypes = new(StringComparer.Ordinal)
    {
        ["Edm.Binary"] = Edm.PrimitiveType.Binary,
        ["Edm.Boolean"] = Edm.PrimitiveType.Boolean,
        ["Edm.Byte"] = Edm.PrimitiveType.Byte,
        ["Edm.Date"] = Edm.PrimitiveType.Date,
        ["Edm.DateTimeOffset"] = Edm.PrimitiveType.DateTimeOffset,
        ["Edm.Decimal"] = Edm.PrimitiveType.Decimal,
        ["Edm.Double"] = Edm.PrimitiveType.Double,
        ["Edm.Duration"] = Edm.PrimitiveType.Duration,
        ["Edm.Guid"] = Edm.PrimitiveType.Guid,
        ["Edm.Int16"] = Edm.PrimitiveType.Int16,
        ["Edm.Int32"] = Edm.PrimitiveType.Int32,
        ["Edm.Int64"] = Edm.PrimitiveType.Int64,
        ["Edm.SByte"] = Edm.PrimitiveType.SByte,
        ["Edm.Single"] = Edm.PrimitiveType.Single,
        ["Edm.String"] = Edm.PrimitiveType.String,
        ["Edm.TimeOfDay"] = Edm.PrimitiveType.TimeOfDay,
    };

    /// <summary>
    /// Resolves a full CSDL document into a <see cref="DocumentModel"/>.
    /// </summary>
    public static DocumentModel ResolveDocument(CsdlDocument document)
    {
        var edmx = document.Edmx ?? throw new ResolveException("Document has no Edmx element");

        // Collect aliases from references
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var reference in edmx.References)
        {
            foreach (var include in reference.Includes)
            {
                if (include.Alias is not null)
                {
                    aliases[include.Alias] = include.Namespace;
                }
            }
        }

        // Resolve each schema
        var schemas = new List<Edm.Model>();
        foreach (var schema in edmx.Schemas)
        {
            schemas.Add(ResolveSchema(schema, aliases));
        }

        // Build cross-schema references
        var references = edmx.References.Select(r => new Edm.Reference
        {
            Uri = r.Uri,
            Includes = r.Includes.Select(i => new Edm.Include { Namespace = i.Namespace, Alias = i.Alias }).ToList(),
            IncludeAnnotations = r.IncludeAnnotations.Select(ia => new Edm.IncludeAnnotations
            {
                TermNamespace = ia.TermNamespace,
                TargetNamespace = ia.TargetNamespace,
                Qualifier = ia.Qualifier,
            }).ToList(),
        }).ToList();

        return new DocumentModel
        {
            Version = edmx.Version ?? "4.0",
            References = references,
            Schemas = schemas,
        };
    }

    /// <summary>
    /// Resolves a single CSDL schema into a <see cref="Edm.Model"/>.
    /// </summary>
    public static Edm.Model Resolve(Csdl.Schema schema)
    {
        return ResolveSchema(schema, new Dictionary<string, string>());
    }

    private static Edm.Model ResolveSchema(Csdl.Schema schema, Dictionary<string, string> externalAliases)
    {
        var ctx = new ResolverContext(schema.Namespace, schema.Alias, externalAliases);

        // Pass 1: Register all named elements (create empty shells)
        foreach (var element in schema.Elements)
        {
            switch (element)
            {
                case Csdl.SchemaElement.EntityTypeElement ete:
                    ctx.RegisterEntityType(ete.EntityType);
                    break;
                case Csdl.SchemaElement.ComplexTypeElement cte:
                    ctx.RegisterComplexType(cte.ComplexType);
                    break;
                case Csdl.SchemaElement.EnumTypeElement ete2:
                    ctx.RegisterEnumType(ete2.EnumType);
                    break;
                case Csdl.SchemaElement.TypeDefinitionElement tde:
                    ctx.RegisterTypeDefinition(tde.TypeDefinition);
                    break;
                case Csdl.SchemaElement.TermElement te:
                    ctx.RegisterTerm(te.Term);
                    break;
                case Csdl.SchemaElement.FunctionElement fe:
                    ctx.RegisterFunction(fe.Function);
                    break;
                case Csdl.SchemaElement.ActionElement ae:
                    ctx.RegisterAction(ae.Action);
                    break;
                case Csdl.SchemaElement.EntityContainerElement ece:
                    ctx.RegisterEntityContainer(ece.EntityContainer);
                    break;
            }
        }

        // Pass 2: Resolve all references
        ctx.ResolveAll();

        return ctx.BuildModel();
    }

    /// <summary>
    /// Internal resolution context holding the lookup tables and resolution logic.
    /// </summary>
    private sealed class ResolverContext
    {
        private readonly string _namespace;
        private readonly string? _alias;
        private readonly Dictionary<string, string> _externalAliases;

        // Lookup tables: unqualified name → resolved element
        private readonly Dictionary<string, Edm.EntityType> _entityTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.ComplexType> _complexTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.EnumType> _enumTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.TypeDefinition> _typeDefinitions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.Term> _terms = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.Function> _functions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Edm.Action> _actions = new(StringComparer.Ordinal);

        // Deferred resolution data
        private readonly List<(Csdl.EntityType Csdl, Edm.EntityType Edm)> _entityTypesPending = [];
        private readonly List<(Csdl.ComplexType Csdl, Edm.ComplexType Edm)> _complexTypesPending = [];
        private readonly List<(Csdl.Term Csdl, Edm.Term Edm)> _termsPending = [];
        private readonly List<(Csdl.Function Csdl, Edm.Function Edm)> _functionsPending = [];
        private readonly List<(Csdl.Action Csdl, Edm.Action Edm)> _actionsPending = [];

        // Container
        private Edm.EntityContainer? _entityContainer;
        private Csdl.EntityContainer? _csdlEntityContainer;

        // Schema elements in registration order
        private readonly List<Edm.SchemaElement> _elements = [];

        public ResolverContext(string ns, string? alias, Dictionary<string, string> externalAliases)
        {
            _namespace = ns;
            _alias = alias;
            _externalAliases = externalAliases;
        }

        public void RegisterEntityType(Csdl.EntityType csdl)
        {
            var edm = new Edm.EntityType
            {
                Name = csdl.Name,
                IsAbstract = csdl.Abstract ?? false,
            };
            _entityTypes[csdl.Name] = edm;
            _entityTypesPending.Add((csdl, edm));
            _elements.Add(new Edm.SchemaElement.EntityTypeElement(edm));
        }

        public void RegisterComplexType(Csdl.ComplexType csdl)
        {
            var edm = new Edm.ComplexType
            {
                Name = csdl.Name,
                IsAbstract = csdl.Abstract ?? false,
            };
            _complexTypes[csdl.Name] = edm;
            _complexTypesPending.Add((csdl, edm));
            _elements.Add(new Edm.SchemaElement.ComplexTypeElement(edm));
        }

        public void RegisterEnumType(Csdl.EnumType csdl)
        {
            var edm = new Edm.EnumType
            {
                Name = csdl.Name,
                Members = csdl.Members.Select(m => new Edm.EnumMember { Name = m.Name, Value = m.Value }).ToList(),
            };
            _enumTypes[csdl.Name] = edm;
            _elements.Add(new Edm.SchemaElement.EnumTypeElement(edm));
        }

        public void RegisterTypeDefinition(Csdl.TypeDefinition csdl)
        {
            if (!PrimitiveTypes.TryGetValue(csdl.UnderlyingType, out var primitiveType))
            {
                throw new UnknownTypeException(csdl.UnderlyingType);
            }

            var edm = new Edm.TypeDefinition
            {
                Name = csdl.Name,
                UnderlyingType = primitiveType,
            };
            _typeDefinitions[csdl.Name] = edm;
            _elements.Add(new Edm.SchemaElement.TypeDefinitionElement(edm));
        }

        public void RegisterTerm(Csdl.Term csdl)
        {
            var edm = new Edm.Term
            {
                Name = csdl.Name,
                IsCollection = csdl.IsCollection,
            };
            _terms[csdl.Name] = edm;
            _termsPending.Add((csdl, edm));
            _elements.Add(new Edm.SchemaElement.TermElement(edm));
        }

        public void RegisterFunction(Csdl.Function csdl)
        {
            var edm = new Edm.Function
            {
                Name = csdl.Name,
                IsBound = csdl.IsBound ?? false,
                IsComposable = csdl.IsComposable ?? false,
            };
            _functions[csdl.Name] = edm;
            _functionsPending.Add((csdl, edm));
            _elements.Add(new Edm.SchemaElement.FunctionElement(edm));
        }

        public void RegisterAction(Csdl.Action csdl)
        {
            var edm = new Edm.Action
            {
                Name = csdl.Name,
                IsBound = csdl.IsBound ?? false,
            };
            _actions[csdl.Name] = edm;
            _actionsPending.Add((csdl, edm));
            _elements.Add(new Edm.SchemaElement.ActionElement(edm));
        }

        public void RegisterEntityContainer(Csdl.EntityContainer csdl)
        {
            _csdlEntityContainer = csdl;
        }

        public void ResolveAll()
        {
            // Resolve entity type members (properties, nav props, keys, base types)
            foreach (var (csdl, edm) in _entityTypesPending)
            {
                ResolveEntityTypeMembers(csdl, edm);
            }

            // Resolve complex type members
            foreach (var (csdl, edm) in _complexTypesPending)
            {
                ResolveComplexTypeMembers(csdl, edm);
            }

            // Resolve keys (after all properties exist)
            foreach (var (csdl, edm) in _entityTypesPending)
            {
                ResolveEntityTypeKeys(csdl, edm);
            }

            // Resolve terms
            foreach (var (csdl, edm) in _termsPending)
            {
                ResolveTermType(csdl, edm);
            }

            // Resolve entity container
            if (_csdlEntityContainer is not null)
            {
                _entityContainer = ResolveEntityContainer(_csdlEntityContainer);
            }

            // Resolve nav-prop partners (after all nav props exist)
            foreach (var (csdl, edm) in _entityTypesPending)
            {
                ResolveNavPropPartners(csdl, edm);
            }

            // Resolve nav-prop bindings on entity sets (after container exists)
            if (_entityContainer is not null && _csdlEntityContainer is not null)
            {
                ResolveNavPropBindings(_csdlEntityContainer, _entityContainer);
            }
        }

        private void ResolveEntityTypeMembers(Csdl.EntityType csdl, Edm.EntityType edm)
        {
            // Base type
            if (csdl.BaseType is not null)
            {
                edm.BaseType = ResolveEntityTypeRef(csdl.BaseType);
            }

            // Properties
            edm.Properties = csdl.Properties.Select(p => ResolveProperty(p)).ToList();

            // Navigation properties
            edm.NavigationProperties = csdl.NavigationProperties.Select(n => ResolveNavigationProperty(n)).ToList();
        }

        private void ResolveComplexTypeMembers(Csdl.ComplexType csdl, Edm.ComplexType edm)
        {
            if (csdl.BaseType is not null)
            {
                edm.BaseType = ResolveComplexTypeRef(csdl.BaseType);
            }

            edm.Properties = csdl.Properties.Select(p => ResolveProperty(p)).ToList();
            edm.NavigationProperties = csdl.NavigationProperties.Select(n => ResolveNavigationProperty(n)).ToList();
        }

        private void ResolveEntityTypeKeys(Csdl.EntityType csdl, Edm.EntityType edm)
        {
            if (csdl.Key is null)
            {
                // Inherit keys from base type
                if (edm.BaseType is not null)
                {
                    edm.Keys = edm.BaseType.Keys;
                }
                return;
            }

            edm.Keys = csdl.Key.PropertyRefs.Select(pr => ResolveKeyPath(pr.Name, edm)).ToList();
        }

        private IReadOnlyList<KeyPathSegment> ResolveKeyPath(string path, Edm.EntityType entityType)
        {
            var segments = path.Split('/');
            var result = new List<KeyPathSegment>();

            // Walk through all properties including inherited ones
            var allProperties = GetAllProperties(entityType);

            foreach (var segment in segments)
            {
                var prop = allProperties.FirstOrDefault(p => p.Name == segment);
                if (prop is not null)
                {
                    result.Add(new KeyPathSegment.PropertySegment(prop));

                    // If the property is a complex type, descend into it
                    if (prop.Type is Edm.ResolvedType.Complex complex)
                    {
                        allProperties = GetAllProperties(complex.Type);
                    }
                }
                else
                {
                    result.Add(new KeyPathSegment.UnresolvedSegment(segment));
                }
            }

            return result;
        }

        private static List<Edm.Property> GetAllProperties(Edm.EntityType entityType)
        {
            var props = new List<Edm.Property>(entityType.Properties);
            var baseType = entityType.BaseType;
            while (baseType is not null)
            {
                props.AddRange(baseType.Properties);
                baseType = baseType.BaseType;
            }
            return props;
        }

        private static List<Edm.Property> GetAllProperties(Edm.ComplexType complexType)
        {
            var props = new List<Edm.Property>(complexType.Properties);
            var baseType = complexType.BaseType;
            while (baseType is not null)
            {
                props.AddRange(baseType.Properties);
                baseType = baseType.BaseType;
            }
            return props;
        }

        private Edm.Property ResolveProperty(Csdl.Property csdl)
        {
            var resolvedType = csdl.TypeName is not null
                ? ResolveType(csdl.TypeName)
                : new Edm.ResolvedType.Primitive(Edm.PrimitiveType.String);

            return new Edm.Property
            {
                Name = csdl.Name,
                Type = resolvedType,
                IsCollection = csdl.IsCollection,
            };
        }

        private Edm.NavigationProperty ResolveNavigationProperty(Csdl.NavigationProperty csdl)
        {
            var target = csdl.TypeName is not null
                ? ResolveEntityTypeRef(csdl.TypeName)
                : throw new MissingTypeNameException("NavigationProperty", csdl.Name);

            return new Edm.NavigationProperty
            {
                Name = csdl.Name,
                Target = target,
                IsCollection = csdl.IsCollection,
                ContainsTarget = csdl.ContainsTarget,
                OnDelete = csdl.OnDelete.HasValue
                    ? (Csdl.OnDeleteAction)csdl.OnDelete.Value
                    : null,
                ReferentialConstraints = csdl.ReferentialConstraints.Select(rc => new Edm.ReferentialConstraint
                {
                    Property = rc.Property,
                    ReferencedProperty = rc.ReferencedProperty,
                }).ToList(),
            };
        }

        private void ResolveNavPropPartners(Csdl.EntityType csdl, Edm.EntityType edm)
        {
            for (var i = 0; i < csdl.NavigationProperties.Count; i++)
            {
                var csdlNav = csdl.NavigationProperties[i];
                var edmNav = edm.NavigationProperties[i];

                if (csdlNav.Partner is not null)
                {
                    edmNav.Partner = ResolvePartnerPath(csdlNav.Partner, edmNav.Target);
                }
            }
        }

        private IReadOnlyList<BindingPathSegment> ResolvePartnerPath(string path, Edm.EntityType targetType)
        {
            var segments = path.Split('/');
            var result = new List<BindingPathSegment>();

            var currentEntityType = targetType;

            foreach (var segment in segments)
            {
                // Check nav props first
                var navProp = GetAllNavigationProperties(currentEntityType)
                    .FirstOrDefault(n => n.Name == segment);
                if (navProp is not null)
                {
                    result.Add(new BindingPathSegment.NavigationPropertySegment(navProp));
                    continue;
                }

                // Check structural properties
                var prop = GetAllProperties(currentEntityType).FirstOrDefault(p => p.Name == segment);
                if (prop is not null)
                {
                    result.Add(new BindingPathSegment.PropertySegment(prop));
                    continue;
                }

                result.Add(new BindingPathSegment.UnresolvedSegment(segment));
            }

            return result;
        }

        private static List<Edm.NavigationProperty> GetAllNavigationProperties(Edm.EntityType entityType)
        {
            var navs = new List<Edm.NavigationProperty>(entityType.NavigationProperties);
            var baseType = entityType.BaseType;
            while (baseType is not null)
            {
                navs.AddRange(baseType.NavigationProperties);
                baseType = baseType.BaseType;
            }
            return navs;
        }

        private void ResolveTermType(Csdl.Term csdl, Edm.Term edm)
        {
            if (csdl.TypeName is not null)
            {
                edm.Type = ResolveTermTypeRef(csdl.TypeName);
            }

            if (csdl.BaseTerm is not null)
            {
                var name = StripNamespace(csdl.BaseTerm);
                if (_terms.TryGetValue(name, out var baseTerm))
                {
                    edm.BaseTerm = baseTerm;
                }
            }
        }

        private Edm.EntityContainer ResolveEntityContainer(Csdl.EntityContainer csdl)
        {
            var container = new Edm.EntityContainer { Name = csdl.Name };

            foreach (var es in csdl.EntitySets)
            {
                var entityTypeName = es.EntityType is not null
                    ? StripNamespace(es.EntityType)
                    : throw new MissingTypeNameException("EntitySet", es.Name);

                if (!_entityTypes.TryGetValue(entityTypeName, out var target))
                {
                    throw new UnknownEntityException(es.EntityType!);
                }

                var edmEs = new Edm.EntitySet { Name = es.Name, Target = target };
                container.Elements.Add(new Edm.EntityContainerElement.EntitySetElement(edmEs));
            }

            foreach (var s in csdl.Singletons)
            {
                var entityTypeName = s.TypeName is not null
                    ? StripNamespace(s.TypeName)
                    : throw new MissingTypeNameException("Singleton", s.Name);

                if (!_entityTypes.TryGetValue(entityTypeName, out var target))
                {
                    throw new UnknownEntityException(s.TypeName!);
                }

                var edmS = new Edm.Singleton { Name = s.Name, Target = target };
                container.Elements.Add(new Edm.EntityContainerElement.SingletonElement(edmS));
            }

            foreach (var fi in csdl.FunctionImports)
            {
                var edmFi = new Edm.FunctionImport
                {
                    Name = fi.Name,
                    Function = fi.Function ?? "",
                };
                container.Elements.Add(new Edm.EntityContainerElement.FunctionImportElement(edmFi));
            }

            foreach (var ai in csdl.ActionImports)
            {
                var edmAi = new Edm.ActionImport
                {
                    Name = ai.Name,
                    Action = ai.Action ?? "",
                };
                container.Elements.Add(new Edm.EntityContainerElement.ActionImportElement(edmAi));
            }

            return container;
        }

        private void ResolveNavPropBindings(Csdl.EntityContainer csdlContainer, Edm.EntityContainer edmContainer)
        {
            // Build lookup of container elements by name
            var containerElementsByName = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var elem in edmContainer.Elements)
            {
                switch (elem)
                {
                    case Edm.EntityContainerElement.EntitySetElement ese:
                        containerElementsByName[ese.EntitySet.Name] = ese.EntitySet;
                        break;
                    case Edm.EntityContainerElement.SingletonElement se:
                        containerElementsByName[se.Singleton.Name] = se.Singleton;
                        break;
                }
            }

            // Resolve bindings on entity sets
            for (var i = 0; i < csdlContainer.EntitySets.Count; i++)
            {
                var csdlEs = csdlContainer.EntitySets[i];
                var edmEs = ((Edm.EntityContainerElement.EntitySetElement)edmContainer.Elements
                    .First(e => e is Edm.EntityContainerElement.EntitySetElement ese && ese.EntitySet.Name == csdlEs.Name))
                    .EntitySet;

                edmEs.NavigationPropertyBindings = csdlEs.NavigationPropertyBindings
                    .Select(nb => ResolveBinding(nb, edmEs.Target, containerElementsByName))
                    .ToList();
            }

            // Resolve bindings on singletons
            for (var i = 0; i < csdlContainer.Singletons.Count; i++)
            {
                var csdlS = csdlContainer.Singletons[i];
                var edmS = ((Edm.EntityContainerElement.SingletonElement)edmContainer.Elements
                    .First(e => e is Edm.EntityContainerElement.SingletonElement se && se.Singleton.Name == csdlS.Name))
                    .Singleton;

                edmS.NavigationPropertyBindings = csdlS.NavigationPropertyBindings
                    .Select(nb => ResolveBinding(nb, edmS.Target, containerElementsByName))
                    .ToList();
            }
        }

        private Edm.NavigationPropertyBinding ResolveBinding(
            Csdl.NavigationPropertyBinding csdl,
            Edm.EntityType sourceType,
            Dictionary<string, object> containerElements)
        {
            // Resolve path (walks nav props on the source entity type)
            var pathSegments = ResolveBindingPath(csdl.Path, sourceType);

            // Resolve target (entity set or singleton name, possibly qualified by container)
            var targetSegments = ResolveBindingTarget(csdl.Target, containerElements);

            return new Edm.NavigationPropertyBinding
            {
                Path = pathSegments,
                Target = targetSegments,
            };
        }

        private List<BindingPathSegment> ResolveBindingPath(string path, Edm.EntityType entityType)
        {
            var segments = path.Split('/');
            var result = new List<BindingPathSegment>();
            var currentType = entityType;

            foreach (var segment in segments)
            {
                // Check if it's a type cast (contains a dot)
                if (segment.Contains('.'))
                {
                    var castName = StripNamespace(segment);
                    if (_entityTypes.TryGetValue(castName, out var castType))
                    {
                        result.Add(new BindingPathSegment.EntityTypeCastSegment(castType));
                        currentType = castType;
                        continue;
                    }

                    if (_complexTypes.TryGetValue(castName, out var complexCast))
                    {
                        result.Add(new BindingPathSegment.ComplexTypeCastSegment(complexCast));
                        continue;
                    }

                    result.Add(new BindingPathSegment.UnresolvedSegment(segment));
                    continue;
                }

                // Check nav props
                var navProp = GetAllNavigationProperties(currentType).FirstOrDefault(n => n.Name == segment);
                if (navProp is not null)
                {
                    result.Add(new BindingPathSegment.NavigationPropertySegment(navProp));
                    currentType = navProp.Target;
                    continue;
                }

                // Check structural properties
                var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == segment);
                if (prop is not null)
                {
                    result.Add(new BindingPathSegment.PropertySegment(prop));
                    continue;
                }

                result.Add(new BindingPathSegment.UnresolvedSegment(segment));
            }

            return result;
        }

        private List<BindingPathSegment> ResolveBindingTarget(string target, Dictionary<string, object> containerElements)
        {
            var segments = target.Split('/');
            var result = new List<BindingPathSegment>();

            foreach (var segment in segments)
            {
                if (containerElements.TryGetValue(segment, out var elem))
                {
                    switch (elem)
                    {
                        case Edm.EntitySet es:
                            result.Add(new BindingPathSegment.EntitySetSegment(es));
                            break;
                        case Edm.Singleton s:
                            result.Add(new BindingPathSegment.SingletonSegment(s));
                            break;
                    }
                }
                else
                {
                    result.Add(new BindingPathSegment.UnresolvedSegment(segment));
                }
            }

            return result;
        }

        // --- Type resolution helpers ---

        private Edm.ResolvedType ResolveType(string typeName)
        {
            // Check primitives first
            if (PrimitiveTypes.TryGetValue(typeName, out var primitive))
            {
                return new Edm.ResolvedType.Primitive(primitive);
            }

            var name = StripNamespace(typeName);

            if (_enumTypes.TryGetValue(name, out var enumType))
            {
                return new Edm.ResolvedType.Enum(enumType);
            }

            if (_complexTypes.TryGetValue(name, out var complexType))
            {
                return new Edm.ResolvedType.Complex(complexType);
            }

            if (_typeDefinitions.TryGetValue(name, out var typeDef))
            {
                return new Edm.ResolvedType.TypeDef(typeDef);
            }

            // Check entity types too (some models reference entity types in properties)
            if (_entityTypes.TryGetValue(name, out _))
            {
                // Entity types shouldn't appear as structural property types,
                // but we don't want to crash. Return as string-typed.
                return new Edm.ResolvedType.Primitive(Edm.PrimitiveType.String);
            }

            throw new UnknownTypeException(typeName);
        }

        private Edm.EntityType ResolveEntityTypeRef(string typeName)
        {
            var name = StripNamespace(typeName);
            if (_entityTypes.TryGetValue(name, out var entityType))
            {
                return entityType;
            }

            throw new UnknownEntityException(typeName);
        }

        private Edm.ComplexType? ResolveComplexTypeRef(string typeName)
        {
            var name = StripNamespace(typeName);
            return _complexTypes.GetValueOrDefault(name);
        }

        private Edm.TermType ResolveTermTypeRef(string typeName)
        {
            if (PrimitiveTypes.TryGetValue(typeName, out var primitive))
            {
                return new Edm.TermType.Primitive(primitive);
            }

            var name = StripNamespace(typeName);

            if (_entityTypes.TryGetValue(name, out var entityType))
            {
                return new Edm.TermType.Entity(entityType);
            }

            if (_complexTypes.TryGetValue(name, out var complexType))
            {
                return new Edm.TermType.Complex(complexType);
            }

            if (_enumTypes.TryGetValue(name, out var enumType))
            {
                return new Edm.TermType.Enum(enumType);
            }

            if (_typeDefinitions.TryGetValue(name, out var typeDef))
            {
                return new Edm.TermType.TypeDef(typeDef);
            }

            throw new UnknownTypeException(typeName);
        }

        /// <summary>
        /// Strips the namespace or alias prefix from a qualified name.
        /// E.g., "BuildingManagement.Room" → "Room", "Bm.Room" → "Room".
        /// </summary>
        private string StripNamespace(string qualifiedName)
        {
            var dotIdx = qualifiedName.LastIndexOf('.');
            if (dotIdx < 0)
            {
                return qualifiedName;
            }

            var prefix = qualifiedName.Substring(0, dotIdx);
            var localName = qualifiedName.Substring(dotIdx + 1);

            // Check if prefix matches our namespace or alias
            if (prefix == _namespace || prefix == _alias)
            {
                return localName;
            }

            // Check external aliases
            if (_externalAliases.ContainsKey(prefix))
            {
                return localName;
            }

            // Return local name anyway — best-effort
            return localName;
        }

        public Edm.Model BuildModel()
        {
            return new Edm.Model
            {
                Namespace = _namespace,
                Alias = _alias,
                Elements = _elements,
                EntityContainer = _entityContainer,
            };
        }
    }
}
