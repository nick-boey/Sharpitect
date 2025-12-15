using Sharpitect.Analysis.Model;
using Sharpitect.Analysis.Model.Code;

namespace Sharpitect.Analysis.Output;

/// <summary>
/// Outputs an architecture model as a plain text tree for debugging purposes.
/// </summary>
public class DebugPrinter : IOutput
{
    private const string Branch = "├── ";
    private const string LastBranch = "└── ";
    private const string Vertical = "│   ";
    private const string Space = "    ";

    /// <inheritdoc />
    public void Write(ArchitectureModel model, TextWriter writer)
    {
        writer.WriteLine("Architecture Model");

        var topLevelElements = new List<IElement>();
        topLevelElements.AddRange(model.People);
        topLevelElements.AddRange(model.Systems);
        topLevelElements.AddRange(model.ExternalSystems);
        topLevelElements.AddRange(model.ExternalContainers);

        for (var i = 0; i < topLevelElements.Count; i++)
        {
            var isLast = i == topLevelElements.Count - 1;
            PrintElement(topLevelElements[i], writer, "", isLast, model.Relationships);
        }
    }

    private void PrintElement(IElement element, TextWriter writer, string indent, bool isLast,
        IReadOnlyList<Relationship> relationships)
    {
        var branch = isLast ? LastBranch : Branch;
        var childIndent = indent + (isLast ? Space : Vertical);

        var label = FormatElement(element);
        writer.WriteLine($"{indent}{branch}{label}");

        var outgoingRelationships = relationships
            .Where(r => ReferenceEquals(r.Source, element))
            .ToList();

        var children = element.Children;
        var totalItems = outgoingRelationships.Count + children.Count;
        var currentIndex = 0;

        foreach (var relationship in outgoingRelationships)
        {
            currentIndex++;
            var relIsLast = currentIndex == totalItems;
            var relBranch = relIsLast ? LastBranch : Branch;
            var relLabel = FormatRelationship(relationship);
            writer.WriteLine($"{childIndent}{relBranch}{relLabel}");
        }

        foreach (var child in children)
        {
            currentIndex++;
            var childIsLast = currentIndex == totalItems;
            PrintElement(child, writer, childIndent, childIsLast, relationships);
        }
    }

    private static string FormatElement(IElement element)
    {
        return element switch
        {
            SoftwareSystem system => FormatSystem(system),
            Container container => FormatContainer(container),
            Component component => FormatComponent(component),
            ExternalSystem externalSystem => FormatExternalSystem(externalSystem),
            ExternalContainer externalContainer => FormatExternalContainer(externalContainer),
            Person person => FormatPerson(person),
            ClassCode classCode => FormatClass(classCode),
            MethodCode methodCode => FormatMethod(methodCode),
            PropertyCode propertyCode => FormatProperty(propertyCode),
            _ => $"[Unknown] {element.Name}"
        };
    }

    private static string FormatSystem(SoftwareSystem system)
    {
        return $"[System] {system.Name}";
    }

    private static string FormatContainer(Container container)
    {
        var tech = string.IsNullOrEmpty(container.Technology) ? "" : $" ({container.Technology})";
        return $"[Container] {container.Name}{tech}";
    }

    private static string FormatComponent(Component component)
    {
        return $"[Component] {component.Name}";
    }

    private static string FormatExternalSystem(ExternalSystem externalSystem)
    {
        return $"[External System] {externalSystem.Name}";
    }

    private static string FormatExternalContainer(ExternalContainer externalContainer)
    {
        var tech = string.IsNullOrEmpty(externalContainer.Technology) ? "" : $" ({externalContainer.Technology})";
        return $"[External Container] {externalContainer.Name}{tech}";
    }

    private static string FormatPerson(Person person)
    {
        return $"[Person] {person.Name}";
    }

    private static string FormatClass(ClassCode classCode)
    {
        var ns = string.IsNullOrEmpty(classCode.Namespace) ? "" : $" ({classCode.Namespace})";
        return $"[Class] {classCode.Name}{ns}";
    }

    private static string FormatMethod(MethodCode methodCode)
    {
        var returnType = string.IsNullOrEmpty(methodCode.ReturnType) ? "" : $": {methodCode.ReturnType}";
        return $"[Method] {methodCode.Name}(){returnType}";
    }

    private static string FormatProperty(PropertyCode propertyCode)
    {
        var type = string.IsNullOrEmpty(propertyCode.Type) ? "" : $": {propertyCode.Type}";
        return $"[Property] {propertyCode.Name}{type}";
    }

    private static string FormatRelationship(Relationship relationship)
    {
        var tech = string.IsNullOrEmpty(relationship.Technology) ? "" : $" [{relationship.Technology}]";
        return $"-> {relationship.Destination.Name}: {relationship.Description}{tech}";
    }
}