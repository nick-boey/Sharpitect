using System.Text;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Formatting;

/// <summary>
/// Formats results as human-readable text.
/// </summary>
public sealed class TextOutputFormatter : IOutputFormatter
{
    public string FormatName => OutputFormat.Text;

    public string Format<T>(T result) where T : notnull
    {
        return result switch
        {
            ErrorResponse error => FormatError(error),
            SearchResults search => FormatSearchResults(search),
            NodeDetail node => FormatNodeDetail(node),
            NodeSummary summary => FormatNodeSummary(summary),
            ChildrenResult children => FormatChildren(children),
            AncestorsResult ancestors => FormatAncestors(ancestors),
            RelationshipsResult relationships => FormatRelationships(relationships),
            CallersResult callers => FormatCallers(callers),
            CalleesResult callees => FormatCallees(callees),
            InheritanceResult inheritance => FormatInheritance(inheritance),
            ListByKindResult listByKind => FormatListByKind(listByKind),
            DependenciesResult dependencies => FormatDependencies(dependencies),
            DependentsResult dependents => FormatDependents(dependents),
            FileDeclarationsResult fileDeclarations => FormatFileDeclarations(fileDeclarations),
            UsagesResult usages => FormatUsages(usages),
            SignatureResult signature => FormatSignature(signature),
            CodeResult code => FormatCodeResult(code),
            TreeResult tree => FormatTree(tree),
            _ => result.ToString() ?? string.Empty
        };
    }

    private static string FormatError(ErrorResponse error)
    {
        return $"ERROR [{error.ErrorCode}]: {error.Message}";
    }

    private static string FormatLocation(string? filePath, int? startLine, int? endLine)
    {
        if (filePath == null) return string.Empty;

        if (startLine.HasValue && endLine.HasValue && endLine.Value > startLine.Value)
        {
            return $"{filePath}:{startLine}-{endLine}";
        }

        if (startLine.HasValue)
        {
            return $"{filePath}:{startLine}";
        }

        return filePath;
    }

    private static string FormatSearchResults(SearchResults results)
    {
        var sb = new StringBuilder();
        var matchWord = results.TotalCount == 1 ? "match" : "matches";
        sb.AppendLine($"Search results ({results.TotalCount} {matchWord}):");

        if (results.Truncated)
        {
            sb.AppendLine($"  (showing {results.Results.Count}, truncated)");
        }

        sb.AppendLine();

        foreach (var node in results.Results)
        {
            AppendNodeSummaryLine(sb, node);
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatNodeDetail(NodeDetail node)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{node.Kind}] {node.Name}");
        sb.AppendLine($"  Full name: {node.FullyQualifiedName}");

        if (node.FilePath != null)
        {
            var location = FormatLocation(node.FilePath, node.LineNumber, node.EndLineNumber);
            sb.AppendLine($"  Path: {location}");
        }

        if (node.C4Level != null)
        {
            sb.AppendLine($"  C4 Level: {node.C4Level}");
        }

        if (node.Metadata != null)
        {
            sb.AppendLine($"  Metadata: {node.Metadata}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatNodeSummary(NodeSummary node)
    {
        var sb = new StringBuilder();
        AppendNodeSummaryLine(sb, node);
        return sb.ToString().TrimEnd();
    }

    private static void AppendNodeSummaryLine(StringBuilder sb, NodeSummary node)
    {
        sb.AppendLine($"[{node.Kind}] {node.Name}");
        sb.AppendLine($"  Full name: {node.FullyQualifiedName}");

        if (node.FilePath != null)
        {
            var location = FormatLocation(node.FilePath, node.LineNumber, node.EndLineNumber);
            sb.AppendLine($"  Path: {location}");
        }

        sb.AppendLine();
    }

    private static string FormatChildren(ChildrenResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Children of {result.ParentId} ({result.TotalCount} total):");

        if (result.Truncated)
        {
            sb.AppendLine($"  (showing {result.Children.Count}, truncated)");
        }

        sb.AppendLine();

        foreach (var child in result.Children)
        {
            sb.AppendLine($"  [{child.Kind}] {child.Name}");
            if (child.LineNumber.HasValue)
            {
                var lineRange = child.EndLineNumber.HasValue && child.EndLineNumber > child.LineNumber
                    ? $"{child.LineNumber}-{child.EndLineNumber}"
                    : $"{child.LineNumber}";
                sb.AppendLine($"    Lines: {lineRange}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatAncestors(AncestorsResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Ancestry of {result.NodeId}:");
        sb.AppendLine();

        var indent = "  ";
        foreach (var ancestor in result.Ancestors)
        {
            sb.AppendLine($"{indent}{ancestor.Kind}: {ancestor.Name}");
            indent += "  ";
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatRelationships(RelationshipsResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Relationships for {result.NodeId}:");
        sb.AppendLine();

        if (result.Outgoing.Count > 0)
        {
            sb.AppendLine("OUTGOING:");
            foreach (var rel in result.Outgoing)
            {
                sb.AppendLine($"  ──[{rel.Kind}]──> {rel.TargetName} ({rel.TargetKind})");
            }
        }

        if (result.Incoming.Count > 0)
        {
            if (result.Outgoing.Count > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine("INCOMING:");
            foreach (var rel in result.Incoming)
            {
                sb.AppendLine($"  <──[{rel.Kind}]── {rel.SourceName} ({rel.SourceKind})");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatCallers(CallersResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Callers of {result.TargetId} ({result.TotalCount} total):");

        if (result.MaxDepthReached)
        {
            sb.AppendLine("  (max depth reached)");
        }

        sb.AppendLine();

        var groupedByDepth = result.Callers.GroupBy(c => c.Depth).OrderBy(g => g.Key);
        foreach (var group in groupedByDepth)
        {
            sb.AppendLine($"Depth {group.Key}:");
            foreach (var caller in group)
            {
                sb.AppendLine($"  [{caller.Kind}] {caller.Name}");
                if (caller.FilePath != null)
                {
                    var location = caller.LineNumber.HasValue
                        ? $"{caller.FilePath}:{caller.LineNumber}"
                        : caller.FilePath;
                    sb.AppendLine($"    File: {location}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatCallees(CalleesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Callees of {result.SourceId} ({result.TotalCount} total):");

        if (result.MaxDepthReached)
        {
            sb.AppendLine("  (max depth reached)");
        }

        sb.AppendLine();

        var groupedByDepth = result.Callees.GroupBy(c => c.Depth).OrderBy(g => g.Key);
        foreach (var group in groupedByDepth)
        {
            sb.AppendLine($"Depth {group.Key}:");
            foreach (var callee in group)
            {
                sb.AppendLine($"  [{callee.Kind}] {callee.Name}");
                if (callee.FilePath != null)
                {
                    var location = callee.LineNumber.HasValue
                        ? $"{callee.FilePath}:{callee.LineNumber}"
                        : callee.FilePath;
                    sb.AppendLine($"    File: {location}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatInheritance(InheritanceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Inheritance hierarchy for {result.NodeId}:");
        sb.AppendLine();

        sb.AppendLine("BASE TYPES:");
        if (result.Ancestors.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var ancestor in result.Ancestors)
            {
                sb.AppendLine($"  [{ancestor.Kind}] {ancestor.Name} ({ancestor.Relationship})");
            }
        }

        sb.AppendLine();
        sb.AppendLine("DERIVED TYPES:");
        if (result.Descendants.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var descendant in result.Descendants)
            {
                sb.AppendLine($"  [{descendant.Kind}] {descendant.Name} ({descendant.Relationship})");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatListByKind(ListByKindResult result)
    {
        var sb = new StringBuilder();
        var scope = result.Scope != null ? $" in {result.Scope}" : "";
        sb.AppendLine($"{result.Kind}s{scope} ({result.TotalCount} total):");

        if (result.Truncated)
        {
            sb.AppendLine($"  (showing {result.Results.Count}, truncated)");
        }

        sb.AppendLine();

        foreach (var node in result.Results)
        {
            sb.AppendLine($"  {node.Name}");
            if (node.FilePath != null)
            {
                var location = FormatLocation(node.FilePath, node.LineNumber, node.EndLineNumber);
                sb.AppendLine($"    {location}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDependencies(DependenciesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dependencies of {result.ProjectId}:");
        sb.AppendLine();

        var direct = result.Dependencies.Where(d => !d.IsTransitive).ToList();
        var transitive = result.Dependencies.Where(d => d.IsTransitive).ToList();

        if (direct.Count > 0)
        {
            sb.AppendLine("DIRECT:");
            foreach (var dep in direct)
            {
                var version = dep.Version != null ? $" ({dep.Version})" : "";
                sb.AppendLine($"  [{dep.Kind}] {dep.Name}{version}");
            }
        }

        if (transitive.Count > 0)
        {
            if (direct.Count > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine("TRANSITIVE:");
            foreach (var dep in transitive)
            {
                var via = dep.Via != null ? $" (via {dep.Via})" : "";
                sb.AppendLine($"  [{dep.Kind}] {dep.Name}{via}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatDependents(DependentsResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dependents of {result.ProjectId}:");
        sb.AppendLine();

        foreach (var dep in result.Dependents)
        {
            var transitive = dep.IsTransitive ? " (transitive)" : "";
            sb.AppendLine($"  [{dep.Kind}] {dep.Name}{transitive}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatFileDeclarations(FileDeclarationsResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Declarations in {Path.GetFileName(result.FilePath)}:");
        sb.AppendLine();

        foreach (var decl in result.Declarations)
        {
            FormatDeclarationWithChildren(sb, decl, "");
        }

        return sb.ToString().TrimEnd();
    }

    private static void FormatDeclarationWithChildren(StringBuilder sb, DeclarationWithChildren decl, string indent)
    {
        var line = decl.LineNumber.HasValue ? $" (line {decl.LineNumber})" : "";
        sb.AppendLine($"{indent}[{decl.Kind}] {decl.Name}{line}");

        for (int i = 0; i < decl.Children.Count; i++)
        {
            var child = decl.Children[i];
            var isLast = i == decl.Children.Count - 1;
            var prefix = isLast ? "└─ " : "├─ ";
            var childIndent = indent + (isLast ? "   " : "│  ");

            var childLine = child.LineNumber.HasValue ? $" (line {child.LineNumber})" : "";
            sb.AppendLine($"{indent}{prefix}[{child.Kind}] {child.Name}{childLine}");

            if (child.Children.Count > 0)
            {
                foreach (var grandchild in child.Children)
                {
                    FormatDeclarationWithChildren(sb, grandchild, childIndent);
                }
            }
        }
    }

    private static string FormatUsages(UsagesResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Usages of {result.TargetId} ({result.TotalCount} found):");

        if (result.Truncated)
        {
            sb.AppendLine($"  (showing {result.Usages.Count}, truncated)");
        }

        sb.AppendLine();

        foreach (var usage in result.Usages)
        {
            sb.AppendLine($"  [{usage.UsageKind}] in {usage.LocationName}");
            if (usage.FilePath != null)
            {
                var location = usage.LineNumber.HasValue
                    ? $"{usage.FilePath}:{usage.LineNumber}"
                    : usage.FilePath;
                sb.AppendLine($"    {location}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatSignature(SignatureResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Signature of {result.Name}:");
        sb.AppendLine();

        var modifiers = result.Modifiers.Count > 0 ? string.Join(" ", result.Modifiers) + " " : "";
        var typeParams = result.TypeParameters.Count > 0 ? $"<{string.Join(", ", result.TypeParameters)}>" : "";
        var returnType = result.ReturnType != null ? $"{result.ReturnType} " : "";

        if (result.Kind == "Method" || result.Kind == "Constructor")
        {
            var parameters = string.Join(", ", result.Parameters.Select(p => $"{p.Type} {p.Name}"));
            sb.AppendLine($"  {modifiers}{returnType}{result.Name}{typeParams}({parameters})");
        }
        else if (result.Kind == "Property")
        {
            sb.AppendLine($"  {modifiers}{returnType}{result.Name} {{ get; set; }}");
        }
        else
        {
            sb.AppendLine($"  {modifiers}{returnType}{result.Name}{typeParams}");
        }

        if (result.Parameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  Parameters:");
            foreach (var param in result.Parameters)
            {
                var optional = param.IsOptional ? " (optional)" : "";
                sb.AppendLine($"    - {param.Name}: {param.Type}{optional}");
            }
        }

        if (result.Documentation != null)
        {
            sb.AppendLine();
            sb.AppendLine("  Documentation:");
            sb.AppendLine($"    {result.Documentation}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatCodeResult(CodeResult result)
    {
        var sb = new StringBuilder();

        // Format the node summary
        sb.AppendLine($"[{result.Node.Kind}] {result.Node.Name}");
        sb.AppendLine($"  Full name: {result.Node.FullyQualifiedName}");

        if (result.Node.FilePath != null)
        {
            var location = FormatLocation(result.Node.FilePath, result.Node.LineNumber, result.Node.EndLineNumber);
            sb.AppendLine($"  Path: {location}");
        }

        if (result.Node.C4Level != null)
        {
            sb.AppendLine($"  C4 Level: {result.Node.C4Level}");
        }

        sb.AppendLine();

        if (result.Error != null)
        {
            sb.AppendLine($"Error reading source code: {result.Error}");
        }
        else if (result.SourceCode != null)
        {
            sb.AppendLine("Source code:");
            sb.AppendLine("```");
            sb.AppendLine(result.SourceCode);
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine("No source code available.");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatTree(TreeResult result)
    {
        var sb = new StringBuilder();
        var header = result.RootId != null
            ? $"Tree from {result.RootId}"
            : "Containment tree";
        sb.AppendLine($"{header} ({result.TotalNodes} nodes, depth {result.MaxDepth}):");
        sb.AppendLine();

        foreach (var root in result.Roots)
        {
            FormatTreeNode(sb, root, 0);
        }

        return sb.ToString().TrimEnd();
    }

    private static void FormatTreeNode(StringBuilder sb, TreeNode node, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}[{node.Kind}] {node.Name}");

        foreach (var child in node.Children)
        {
            FormatTreeNode(sb, child, depth + 1);
        }
    }
}
