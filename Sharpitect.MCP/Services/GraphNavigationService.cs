using Sharpitect.Analysis.Graph;
using Sharpitect.Analysis.Persistence;
using Sharpitect.Analysis.Search;
using Sharpitect.MCP.Models;

namespace Sharpitect.MCP.Services;

/// <summary>
/// Implementation of IGraphNavigationService using IGraphRepository.
/// </summary>
public sealed class GraphNavigationService(IGraphRepository repository) : IGraphNavigationService
{
    private readonly IGraphRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<SearchResults> SearchAsync(
        string query,
        SearchMatchMode matchMode = SearchMatchMode.Contains,
        IReadOnlyCollection<DeclarationKind>? kindFilter = null,
        bool caseSensitive = false,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var graph = await _repository.LoadGraphAsync(cancellationToken).ConfigureAwait(false);
        var source = new InMemoryGraphSource(graph);
        var searchService = new GraphSearchService(source);

        var searchQuery = new SearchQuery
        {
            SearchText = query,
            MatchMode = matchMode,
            KindFilter = kindFilter ?? [],
            CaseSensitive = caseSensitive
        };
        var nodes = await searchService.SearchAsync(searchQuery, cancellationToken).ConfigureAwait(false);

        var nodesList = nodes.ToList();
        var totalCount = nodesList.Count;
        var truncated = totalCount > limit;
        var limitedNodes = nodesList.Take(limit).ToList();

        return new SearchResults(
            limitedNodes.Select(NodeSummary.FromDeclarationNode).ToList(),
            totalCount,
            truncated);
    }

    public async Task<NodeDetail?> GetNodeAsync(string id, CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(id, cancellationToken).ConfigureAwait(false);
        return node == null ? null : NodeDetail.FromDeclarationNode(node);
    }

    public async Task<ChildrenResult?> GetChildrenAsync(
        string parentId,
        DeclarationKind? kindFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var parentNode = await _repository.GetNodeAsync(parentId, cancellationToken).ConfigureAwait(false);
        if (parentNode == null)
        {
            return null;
        }

        var edges = await _repository.GetOutgoingEdgesAsync(parentId, cancellationToken).ConfigureAwait(false);
        var containsEdges = edges.Where(e => e.Kind == RelationshipKind.Contains).ToList();

        var children = new List<NodeSummary>();
        foreach (var edge in containsEdges)
        {
            var childNode = await _repository.GetNodeAsync(edge.TargetId, cancellationToken).ConfigureAwait(false);
            if (childNode == null) continue;
            if (kindFilter == null || childNode.Kind == kindFilter.Value)
            {
                children.Add(NodeSummary.FromDeclarationNode(childNode));
            }
        }

        var totalCount = children.Count;
        var truncated = totalCount > limit;
        var limitedChildren = children.Take(limit).ToList();

        return new ChildrenResult(parentId, limitedChildren, totalCount, truncated);
    }

    public async Task<AncestorsResult?> GetAncestorsAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var ancestors = new List<NodeSummary>();
        var currentId = nodeId;
        var visited = new HashSet<string> { nodeId };

        while (true)
        {
            var incomingEdges =
                await _repository.GetIncomingEdgesAsync(currentId, cancellationToken).ConfigureAwait(false);
            var containsEdge = incomingEdges.FirstOrDefault(e => e.Kind == RelationshipKind.Contains);

            if (containsEdge == null)
            {
                break;
            }

            if (!visited.Add(containsEdge.SourceId))
            {
                break;
            }

            var parentNode = await _repository.GetNodeAsync(containsEdge.SourceId, cancellationToken)
                .ConfigureAwait(false);
            if (parentNode == null)
            {
                break;
            }

            ancestors.Insert(0, NodeSummary.FromDeclarationNode(parentNode));
            currentId = containsEdge.SourceId;
        }

        return new AncestorsResult(nodeId, ancestors);
    }

    public async Task<RelationshipsResult?> GetRelationshipsAsync(
        string nodeId,
        RelationshipDirection direction = RelationshipDirection.Both,
        RelationshipKind? kindFilter = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var outgoing = new List<RelationshipInfo>();
        var incoming = new List<IncomingRelationshipInfo>();

        if (direction == RelationshipDirection.Outgoing || direction == RelationshipDirection.Both)
        {
            var outgoingEdges =
                await _repository.GetOutgoingEdgesAsync(nodeId, cancellationToken).ConfigureAwait(false);
            foreach (var edge in outgoingEdges.Where(e => e.Kind != RelationshipKind.Contains))
            {
                if (kindFilter.HasValue && edge.Kind != kindFilter.Value)
                {
                    continue;
                }

                var targetNode = await _repository.GetNodeAsync(edge.TargetId, cancellationToken).ConfigureAwait(false);
                if (targetNode != null)
                {
                    outgoing.Add(RelationshipInfo.FromEdge(edge, targetNode));
                }

                if (outgoing.Count >= limit)
                {
                    break;
                }
            }
        }

        if (direction == RelationshipDirection.Incoming || direction == RelationshipDirection.Both)
        {
            var incomingEdges =
                await _repository.GetIncomingEdgesAsync(nodeId, cancellationToken).ConfigureAwait(false);
            foreach (var edge in incomingEdges.Where(e => e.Kind != RelationshipKind.Contains))
            {
                if (kindFilter.HasValue && edge.Kind != kindFilter.Value)
                {
                    continue;
                }

                var sourceNode = await _repository.GetNodeAsync(edge.SourceId, cancellationToken).ConfigureAwait(false);
                if (sourceNode != null)
                {
                    incoming.Add(IncomingRelationshipInfo.FromEdge(edge, sourceNode));
                }

                if (incoming.Count >= limit)
                {
                    break;
                }
            }
        }

        return new RelationshipsResult(nodeId, outgoing, incoming);
    }

    public async Task<CallersResult?> GetCallersAsync(
        string nodeId,
        int depth = 1,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var maxDepth = Math.Min(depth, 5);
        var callers = new List<CallerEntry>();
        var visited = new HashSet<string> { nodeId };
        var queue = new Queue<(string Id, int Depth)>();
        queue.Enqueue((nodeId, 0));

        while (queue.Count > 0 && callers.Count < limit)
        {
            var (currentId, currentDepth) = queue.Dequeue();

            if (currentDepth >= maxDepth)
            {
                continue;
            }

            var incomingEdges =
                await _repository.GetIncomingEdgesAsync(currentId, cancellationToken).ConfigureAwait(false);
            var callEdges = incomingEdges.Where(e => e.Kind == RelationshipKind.Calls).ToList();

            foreach (var edge in callEdges)
            {
                if (!visited.Add(edge.SourceId))
                {
                    continue;
                }

                var callerNode = await _repository.GetNodeAsync(edge.SourceId, cancellationToken).ConfigureAwait(false);
                if (callerNode != null)
                {
                    callers.Add(new CallerEntry(
                        callerNode.Id,
                        callerNode.Name,
                        callerNode.Kind.ToString(),
                        callerNode.FilePath,
                        callerNode.StartLine > 0 ? callerNode.StartLine : null,
                        currentDepth + 1));

                    queue.Enqueue((edge.SourceId, currentDepth + 1));

                    if (callers.Count >= limit)
                    {
                        break;
                    }
                }
            }
        }

        return new CallersResult(nodeId, callers, callers.Count, depth > maxDepth);
    }

    public async Task<CalleesResult?> GetCalleesAsync(
        string nodeId,
        int depth = 1,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var maxDepth = Math.Min(depth, 5);
        var callees = new List<CalleeEntry>();
        var visited = new HashSet<string> { nodeId };
        var queue = new Queue<(string Id, int Depth)>();
        queue.Enqueue((nodeId, 0));

        while (queue.Count > 0 && callees.Count < limit)
        {
            var (currentId, currentDepth) = queue.Dequeue();

            if (currentDepth >= maxDepth)
            {
                continue;
            }

            var outgoingEdges =
                await _repository.GetOutgoingEdgesAsync(currentId, cancellationToken).ConfigureAwait(false);
            var callEdges = outgoingEdges.Where(e => e.Kind == RelationshipKind.Calls).ToList();

            foreach (var edge in callEdges)
            {
                if (!visited.Add(edge.TargetId))
                {
                    continue;
                }

                var calleeNode = await _repository.GetNodeAsync(edge.TargetId, cancellationToken).ConfigureAwait(false);
                if (calleeNode != null)
                {
                    callees.Add(new CalleeEntry(
                        calleeNode.Id,
                        calleeNode.Name,
                        calleeNode.Kind.ToString(),
                        calleeNode.FilePath,
                        calleeNode.StartLine > 0 ? calleeNode.StartLine : null,
                        currentDepth + 1));

                    queue.Enqueue((edge.TargetId, currentDepth + 1));

                    if (callees.Count >= limit)
                    {
                        break;
                    }
                }
            }
        }

        return new CalleesResult(nodeId, callees, callees.Count, depth > maxDepth);
    }

    public async Task<InheritanceResult?> GetInheritanceAsync(
        string nodeId,
        InheritanceDirection direction = InheritanceDirection.Both,
        int depth = 10,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var ancestors = new List<InheritanceEntry>();
        var descendants = new List<InheritanceEntry>();

        if (direction == InheritanceDirection.Ancestors || direction == InheritanceDirection.Both)
        {
            await GetInheritanceAncestorsAsync(nodeId, ancestors, depth, 1).ConfigureAwait(false);
        }

        if (direction == InheritanceDirection.Descendants || direction == InheritanceDirection.Both)
        {
            await GetInheritanceDescendantsAsync(nodeId, descendants, depth, 1).ConfigureAwait(false);
        }

        return new InheritanceResult(nodeId, ancestors, descendants);
    }

    private async Task GetInheritanceAncestorsAsync(
        string nodeId,
        List<InheritanceEntry> ancestors,
        int maxDepth,
        int currentDepth,
        HashSet<string>? visited = null)
    {
        visited ??= [nodeId];

        if (currentDepth > maxDepth)
        {
            return;
        }

        var outgoingEdges = await _repository.GetOutgoingEdgesAsync(nodeId).ConfigureAwait(false);
        var inheritanceEdges = outgoingEdges
            .Where(e => e.Kind == RelationshipKind.Inherits || e.Kind == RelationshipKind.Implements)
            .ToList();

        foreach (var edge in inheritanceEdges)
        {
            if (!visited.Add(edge.TargetId))
            {
                continue;
            }

            var targetNode = await _repository.GetNodeAsync(edge.TargetId).ConfigureAwait(false);
            if (targetNode != null)
            {
                ancestors.Add(new InheritanceEntry(
                    targetNode.Id,
                    targetNode.Name,
                    targetNode.Kind.ToString(),
                    edge.Kind.ToString(),
                    currentDepth));

                await GetInheritanceAncestorsAsync(edge.TargetId, ancestors, maxDepth, currentDepth + 1, visited)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task GetInheritanceDescendantsAsync(
        string nodeId,
        List<InheritanceEntry> descendants,
        int maxDepth,
        int currentDepth,
        HashSet<string>? visited = null)
    {
        visited ??= [nodeId];

        if (currentDepth > maxDepth)
        {
            return;
        }

        var incomingEdges = await _repository.GetIncomingEdgesAsync(nodeId).ConfigureAwait(false);
        var inheritanceEdges = incomingEdges
            .Where(e => e.Kind is RelationshipKind.Inherits or RelationshipKind.Implements)
            .ToList();

        foreach (var edge in inheritanceEdges)
        {
            if (!visited.Add(edge.SourceId))
            {
                continue;
            }

            var sourceNode = await _repository.GetNodeAsync(edge.SourceId).ConfigureAwait(false);
            if (sourceNode != null)
            {
                descendants.Add(new InheritanceEntry(
                    sourceNode.Id,
                    sourceNode.Name,
                    sourceNode.Kind.ToString(),
                    edge.Kind.ToString(),
                    currentDepth));

                await GetInheritanceDescendantsAsync(edge.SourceId, descendants, maxDepth, currentDepth + 1, visited)
                    .ConfigureAwait(false);
            }
        }
    }

    public async Task<ListByKindResult> ListByKindAsync(
        DeclarationKind kind,
        string? scopeId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _repository.GetNodesByKindAsync(kind, cancellationToken).ConfigureAwait(false);
        var nodesList = nodes.ToList();

        if (scopeId != null)
        {
            var scopeDescendants = await GetAllDescendantsAsync(scopeId).ConfigureAwait(false);
            nodesList = nodesList.Where(n => scopeDescendants.Contains(n.Id)).ToList();
        }

        var totalCount = nodesList.Count;
        var truncated = totalCount > limit;
        var limitedNodes = nodesList.Take(limit).ToList();

        return new ListByKindResult(
            kind.ToString(),
            scopeId,
            limitedNodes.Select(NodeSummary.FromDeclarationNode).ToList(),
            totalCount,
            truncated);
    }

    private async Task<HashSet<string>> GetAllDescendantsAsync(string scopeId)
    {
        var descendants = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(scopeId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!descendants.Add(currentId))
            {
                continue;
            }

            var outgoingEdges = await _repository.GetOutgoingEdgesAsync(currentId).ConfigureAwait(false);
            var containsEdges = outgoingEdges.Where(e => e.Kind == RelationshipKind.Contains);

            foreach (var edge in containsEdges)
            {
                queue.Enqueue(edge.TargetId);
            }
        }

        return descendants;
    }

    public async Task<DependenciesResult?> GetDependenciesAsync(
        string projectId,
        bool includeTransitive = false,
        CancellationToken cancellationToken = default)
    {
        var projectNode = await _repository.GetNodeAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (projectNode == null || projectNode.Kind != DeclarationKind.Project)
        {
            return null;
        }

        var dependencies = new List<DependencyEntry>();
        var visited = new HashSet<string> { projectId };

        await GetProjectDependenciesAsync(projectId, dependencies, visited, includeTransitive, null)
            .ConfigureAwait(false);

        return new DependenciesResult(projectId, dependencies);
    }

    private async Task GetProjectDependenciesAsync(
        string projectId,
        List<DependencyEntry> dependencies,
        HashSet<string> visited,
        bool includeTransitive,
        string? via)
    {
        var outgoingEdges = await _repository.GetOutgoingEdgesAsync(projectId).ConfigureAwait(false);
        var dependsOnEdges = outgoingEdges.Where(e => e.Kind == RelationshipKind.DependsOn).ToList();

        foreach (var edge in dependsOnEdges)
        {
            if (!visited.Add(edge.TargetId))
            {
                continue;
            }

            var targetNode = await _repository.GetNodeAsync(edge.TargetId).ConfigureAwait(false);
            if (targetNode != null)
            {
                dependencies.Add(new DependencyEntry(
                    targetNode.Id,
                    targetNode.Name,
                    targetNode.Kind.ToString(),
                    via != null,
                    via,
                    null));

                if (includeTransitive)
                {
                    await GetProjectDependenciesAsync(edge.TargetId, dependencies, visited, true, targetNode.Name)
                        .ConfigureAwait(false);
                }
            }
        }
    }

    public async Task<DependentsResult?> GetDependentsAsync(
        string projectId,
        bool includeTransitive = false,
        CancellationToken cancellationToken = default)
    {
        var projectNode = await _repository.GetNodeAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (projectNode is not { Kind: DeclarationKind.Project })
        {
            return null;
        }

        var dependents = new List<DependentEntry>();
        var visited = new HashSet<string> { projectId };

        await GetProjectDependentsAsync(projectId, dependents, visited, includeTransitive, false).ConfigureAwait(false);

        return new DependentsResult(projectId, dependents);
    }

    private async Task GetProjectDependentsAsync(
        string projectId,
        List<DependentEntry> dependents,
        HashSet<string> visited,
        bool includeTransitive,
        bool isTransitive)
    {
        var incomingEdges = await _repository.GetIncomingEdgesAsync(projectId).ConfigureAwait(false);
        var dependsOnEdges = incomingEdges.Where(e => e.Kind == RelationshipKind.DependsOn).ToList();

        foreach (var edge in dependsOnEdges)
        {
            if (!visited.Add(edge.SourceId))
            {
                continue;
            }

            var sourceNode = await _repository.GetNodeAsync(edge.SourceId).ConfigureAwait(false);
            if (sourceNode != null)
            {
                dependents.Add(new DependentEntry(
                    sourceNode.Id,
                    sourceNode.Name,
                    sourceNode.Kind.ToString(),
                    isTransitive));

                if (includeTransitive)
                {
                    await GetProjectDependentsAsync(edge.SourceId, dependents, visited, true, true)
                        .ConfigureAwait(false);
                }
            }
        }
    }

    public async Task<FileDeclarationsResult?> GetFileDeclarationsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _repository.GetNodesByFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        var nodesList = nodes.ToList();

        if (nodesList.Count == 0)
        {
            return null;
        }

        var nodeIds = nodesList.Select(n => n.Id).ToHashSet();
        var declarations = await BuildDeclarationTreeAsync(nodesList, nodeIds).ConfigureAwait(false);

        return new FileDeclarationsResult(filePath, declarations);
    }

    private async Task<List<DeclarationWithChildren>> BuildDeclarationTreeAsync(
        List<DeclarationNode> nodes,
        HashSet<string> nodeIds)
    {
        var rootNodes = new List<DeclarationWithChildren>();
        var nodeMap = nodes.ToDictionary(n => n.Id);
        var processed = new HashSet<string>();

        foreach (var node in nodes.OrderBy(n => n.StartLine))
        {
            if (processed.Contains(node.Id))
            {
                continue;
            }

            var parentId = await FindParentInFileAsync(node.Id, nodeIds).ConfigureAwait(false);
            if (parentId == null)
            {
                var declWithChildren =
                    await BuildDeclarationNodeAsync(node, nodeMap, nodeIds, processed).ConfigureAwait(false);
                rootNodes.Add(declWithChildren);
            }
        }

        return rootNodes;
    }

    private async Task<string?> FindParentInFileAsync(string nodeId, HashSet<string> fileNodeIds)
    {
        var incomingEdges = await _repository.GetIncomingEdgesAsync(nodeId).ConfigureAwait(false);
        var containsEdge = incomingEdges.FirstOrDefault(e => e.Kind == RelationshipKind.Contains);

        if (containsEdge != null && fileNodeIds.Contains(containsEdge.SourceId))
        {
            return containsEdge.SourceId;
        }

        return null;
    }

    private async Task<DeclarationWithChildren> BuildDeclarationNodeAsync(
        DeclarationNode node,
        Dictionary<string, DeclarationNode> nodeMap,
        HashSet<string> fileNodeIds,
        HashSet<string> processed)
    {
        processed.Add(node.Id);

        var childDeclarations = new List<DeclarationWithChildren>();
        var outgoingEdges = await _repository.GetOutgoingEdgesAsync(node.Id).ConfigureAwait(false);
        var containsEdges = outgoingEdges.Where(e => e.Kind == RelationshipKind.Contains);

        foreach (var edge in containsEdges)
        {
            if (!fileNodeIds.Contains(edge.TargetId) || processed.Contains(edge.TargetId))
            {
                continue;
            }

            if (nodeMap.TryGetValue(edge.TargetId, out var childNode))
            {
                var childDecl = await BuildDeclarationNodeAsync(childNode, nodeMap, fileNodeIds, processed)
                    .ConfigureAwait(false);
                childDeclarations.Add(childDecl);
            }
        }

        return new DeclarationWithChildren(
            node.Id,
            node.Name,
            node.Kind.ToString(),
            node.StartLine > 0 ? node.StartLine : null,
            childDeclarations.OrderBy(c => c.LineNumber ?? 0).ToList());
    }

    public async Task<UsagesResult?> GetUsagesAsync(
        string nodeId,
        UsageKind? usageKindFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        var usages = new List<UsageEntry>();
        var incomingEdges = await _repository.GetIncomingEdgesAsync(nodeId, cancellationToken).ConfigureAwait(false);

        foreach (var edge in incomingEdges)
        {
            if (edge.Kind == RelationshipKind.Contains)
            {
                continue;
            }

            var usageKind = MapEdgeKindToUsageKind(edge.Kind);
            if (usageKindFilter.HasValue && usageKindFilter != UsageKind.All &&
                usageKind != usageKindFilter.Value.ToString())
            {
                continue;
            }

            var sourceNode = await _repository.GetNodeAsync(edge.SourceId, cancellationToken).ConfigureAwait(false);
            if (sourceNode != null)
            {
                usages.Add(new UsageEntry(
                    sourceNode.Id,
                    sourceNode.Name,
                    sourceNode.Kind.ToString(),
                    usageKind,
                    edge.SourceFilePath ?? sourceNode.FilePath,
                    edge.SourceLine ?? (sourceNode.StartLine > 0 ? sourceNode.StartLine : null)));

                if (usages.Count >= limit)
                {
                    break;
                }
            }
        }

        return new UsagesResult(nodeId, usages, usages.Count, usages.Count >= limit);
    }

    private static string MapEdgeKindToUsageKind(RelationshipKind kind)
    {
        return kind switch
        {
            RelationshipKind.Calls => "Call",
            RelationshipKind.Constructs => "Instantiation",
            RelationshipKind.Inherits or RelationshipKind.Implements => "Inheritance",
            RelationshipKind.References or RelationshipKind.Uses => "TypeReference",
            _ => kind.ToString()
        };
    }

    public async Task<SignatureResult?> GetSignatureAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
        if (node == null)
        {
            return null;
        }

        // Parse metadata if available for detailed signature info
        var modifiers = new List<string>();
        var parameters = new List<ParameterInfo>();
        string? returnType = null;
        bool isAsync = false;
        bool isStatic = false;
        var typeParameters = new List<string>();
        string? documentation = null;

        // Try to extract info from metadata if present
        if (node.Metadata != null)
        {
            try
            {
                var metadataDoc = System.Text.Json.JsonDocument.Parse(node.Metadata);
                var root = metadataDoc.RootElement;

                if (root.TryGetProperty("modifiers", out var modifiersElement))
                {
                    modifiers = modifiersElement.EnumerateArray()
                        .Select(e => e.GetString()!)
                        .Where(s => s != null)
                        .ToList();
                }

                if (root.TryGetProperty("returnType", out var returnTypeElement))
                {
                    returnType = returnTypeElement.GetString();
                }

                if (root.TryGetProperty("isAsync", out var isAsyncElement))
                {
                    isAsync = isAsyncElement.GetBoolean();
                }

                if (root.TryGetProperty("isStatic", out var isStaticElement))
                {
                    isStatic = isStaticElement.GetBoolean();
                }

                if (root.TryGetProperty("parameters", out var parametersElement))
                {
                    foreach (var param in parametersElement.EnumerateArray())
                    {
                        var name = param.GetProperty("name").GetString() ?? "";
                        var type = param.GetProperty("type").GetString() ?? "";
                        var isOptional = param.TryGetProperty("isOptional", out var optElement) &&
                                         optElement.GetBoolean();
                        parameters.Add(new ParameterInfo(name, type, isOptional));
                    }
                }

                if (root.TryGetProperty("typeParameters", out var typeParamsElement))
                {
                    // TODO: Fix nullability
                    typeParameters = typeParamsElement.EnumerateArray()
                        .Select(e => e.GetString()!)
                        .Where(s => s != null)
                        .ToList();
                }

                if (root.TryGetProperty("documentation", out var docElement))
                {
                    documentation = docElement.GetString();
                }
            }
            catch
            {
                // Ignore metadata parsing errors
            }
        }

        return new SignatureResult(
            node.Id,
            node.Name,
            node.Kind.ToString(),
            returnType,
            parameters,
            modifiers,
            isAsync,
            isStatic,
            typeParameters,
            documentation ?? node.C4Description);
    }
}