using Sharpitect.Analysis.Configuration;
using Sharpitect.Analysis.Configuration.Definitions;
using Sharpitect.Analysis.Model;
using Sharpitect.Analysis.Model.Code;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Builds C4 model elements from analysis results and configuration.
/// </summary>
public class ModelBuilder
{
    /// <summary>
    /// Builds a software system from configuration.
    /// </summary>
    /// <param name="config">The system configuration from YAML.</param>
    /// <returns>A new software system.</returns>
    public static SoftwareSystem BuildSystem(SystemConfiguration? config)
    {
        var name = config?.System?.Name ?? "Unnamed System";
        var description = config?.System?.Description;
        return new SoftwareSystem(name, description);
    }

    /// <summary>
    /// Builds a container from configuration or project path.
    /// </summary>
    /// <param name="config">The container configuration from YAML.</param>
    /// <param name="projectPath">The project path for fallback naming.</param>
    /// <returns>A new container.</returns>
    public static Container BuildContainer(ContainerConfiguration? config, string projectPath)
    {
        var name = config?.Container?.Name ?? Path.GetFileNameWithoutExtension(projectPath);
        var description = config?.Container?.Description;
        var technology = config?.Container?.Technology;
        return new Container(name, description, technology);
    }

    /// <summary>
    /// Builds components from analysis results and adds them to a container.
    /// </summary>
    /// <param name="container">The container to add components to.</param>
    /// <param name="types">The analyzed types.</param>
    /// <param name="namespaceComponents">Optional namespace-based component definitions from YAML.</param>
    public static void BuildComponents(
        Container container,
        List<TypeAnalysisResult> types,
        List<ComponentDefinition>? namespaceComponents)
    {
        var componentMap = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);

        // Create components from [Component] attributes on interfaces/classes
        foreach (var type in types.Where(t => t.ComponentName != null))
        {
            if (componentMap.ContainsKey(type.ComponentName!)) continue;
            var component = new Component(type.ComponentName!, type.ComponentDescription);
            componentMap[type.ComponentName!] = component;
            container.AddComponent(component);
        }

        // Create components from namespace mappings in YAML
        if (namespaceComponents != null)
        {
            foreach (var compDef in namespaceComponents)
            {
                if (componentMap.ContainsKey(compDef.Name)) continue;
                var component = new Component(compDef.Name, compDef.Description);
                componentMap[compDef.Name] = component;
                container.AddComponent(component);
            }
        }

        // Map classes to components
        foreach (var type in types.Where(t => t.IsClass))
        {
            Component? targetComponent = null;

            // Priority 1: Check if class itself has [Component] attribute
            if (type.ComponentName != null && componentMap.TryGetValue(type.ComponentName, out var directComp))
            {
                targetComponent = directComp;
            }

            // Priority 2: Check if class implements a [Component] interface
            if (targetComponent == null)
            {
                foreach (var interfaceType in type.BaseTypes.Select(baseType =>
                             types.FirstOrDefault(t => t.IsInterface && t.Name == baseType)))
                {
                    if (interfaceType?.ComponentName == null ||
                        !componentMap.TryGetValue(interfaceType.ComponentName, out var comp))
                    {
                        continue;
                    }

                    targetComponent = comp;
                    break;
                }
            }

            // Check namespace mapping
            if (targetComponent == null && namespaceComponents != null && type.Namespace != null)
            {
                var nsMapping = namespaceComponents
                    .Where(c => c.Namespace != null)
                    .FirstOrDefault(c => type.Namespace.StartsWith(c.Namespace!, StringComparison.OrdinalIgnoreCase));

                if (nsMapping != null && componentMap.TryGetValue(nsMapping.Name, out var comp))
                {
                    targetComponent = comp;
                }
            }

            // Add class to component
            if (targetComponent == null) continue;
            var classCode = new ClassCode(type.Name, type.Namespace);
            foreach (var method in type.Methods)
            {
                classCode.AddMethod(new MethodCode(method.Name, method.ReturnType));
            }

            foreach (var prop in type.Properties)
            {
                classCode.AddProperty(new PropertyCode(prop.Name, prop.Type));
            }

            targetComponent.AddCodeElement(classCode);
        }
    }

    /// <summary>
    /// Builds relationships from analysed types.
    /// </summary>
    /// <param name="model">The architecture model to add relationships to.</param>
    /// <param name="types">The analyzed types.</param>
    /// <param name="componentMap">Map of component names to components.</param>
    /// <param name="peopleMap">Map of person names to people.</param>
    public void BuildRelationships(
        ArchitectureModel model,
        List<TypeAnalysisResult> types,
        Dictionary<string, Component> componentMap,
        Dictionary<string, Person> peopleMap)
    {
        foreach (var type in types)
        {
            // Find the component this type belongs to
            Component? sourceComponent = null;
            if (type.ComponentName != null && componentMap.TryGetValue(type.ComponentName, out var comp))
            {
                sourceComponent = comp;
            }

            if (sourceComponent == null)
            {
                continue;
            }

            foreach (var method in type.Methods)
            {
                // Handle [UserAction] - creates relationship from person to component
                if (method.UserActionPerson == null || method.UserActionDescription == null) continue;
                if (!peopleMap.TryGetValue(method.UserActionPerson, out var person)) continue;
                var relationship = new Relationship(
                    person,
                    sourceComponent,
                    method.UserActionDescription);
                model.AddRelationship(relationship);
            }
        }
    }
}