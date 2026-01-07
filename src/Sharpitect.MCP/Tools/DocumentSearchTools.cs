using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sharpitect.MCP.Formatting;
using Sharpitect.MCP.Models;
using Sharpitect.MCP.Services;

namespace Sharpitect.MCP.Tools;

/// <summary>
/// MCP tools for searching and navigating markdown documentation.
/// </summary>
[McpServerToolType]
public static class DocumentSearchTools
{
    /// <summary>
    /// Checks if the graph is still being built and returns an error response if so.
    /// </summary>
    private static CallToolResult? CheckBuildingState(
        IGraphBuildingService buildingService,
        ToolResultBuilder resultBuilder)
    {
        if (buildingService.IsBuilding)
        {
            return resultBuilder.BuildError(ErrorResponse.GraphBuilding(buildingService.BuildStatus));
        }

        if (buildingService.HasFailed)
        {
            return resultBuilder.BuildError(ErrorResponse.BuildFailed(
                buildingService.ErrorMessage ?? "Graph build failed with an unknown error."));
        }

        return null;
    }

    /// <summary>
    /// Searches for relevant documentation using semantic similarity.
    /// </summary>
    [McpServerTool,
     Description("Search markdown documentation using semantic similarity. Returns relevant sections ranked by relevance.")]
    public static async Task<CallToolResult> SearchDocuments(
        IDocumentSearchService documentSearchService,
        IGraphBuildingService buildingService,
        ToolResultBuilder resultBuilder,
        [Description("Natural language search query (e.g., 'how to configure authentication')")]
        string query,
        [Description("Maximum number of results. Defaults to 10.")]
        int limit = 10,
        [Description("Minimum similarity score (0.0 to 1.0). Defaults to 0.0.")]
        double minSimilarity = 0.0)
    {
        var buildError = CheckBuildingState(buildingService, resultBuilder);
        if (buildError != null) return buildError;

        if (!documentSearchService.IsSemanticSearchAvailable)
        {
            return resultBuilder.BuildError(ErrorResponse.SemanticSearchUnavailable());
        }

        var result = await documentSearchService.SearchAsync(query, limit, minSimilarity);
        return resultBuilder.Build(result);
    }

    /// <summary>
    /// Lists all indexed markdown documents.
    /// </summary>
    [McpServerTool,
     Description("List all indexed markdown documents in the solution.")]
    public static async Task<CallToolResult> ListDocuments(
        IDocumentSearchService documentSearchService,
        IGraphBuildingService buildingService,
        ToolResultBuilder resultBuilder,
        [Description("Maximum number of documents to return. Defaults to 100.")]
        int limit = 100)
    {
        var buildError = CheckBuildingState(buildingService, resultBuilder);
        if (buildError != null) return buildError;

        var result = await documentSearchService.ListDocumentsAsync(limit);
        return resultBuilder.Build(result);
    }

    /// <summary>
    /// Gets detailed information about a markdown document.
    /// </summary>
    [McpServerTool,
     Description("Get detailed information about a markdown document including headings and links.")]
    public static async Task<CallToolResult> GetDocument(
        IDocumentSearchService documentSearchService,
        IGraphBuildingService buildingService,
        ToolResultBuilder resultBuilder,
        [Description("Document ID (relative path, e.g., 'docs/README.md')")]
        string documentId)
    {
        var buildError = CheckBuildingState(buildingService, resultBuilder);
        if (buildError != null) return buildError;

        var result = await documentSearchService.GetDocumentAsync(documentId);
        if (result == null)
        {
            return resultBuilder.BuildError(ErrorResponse.NotFound(
                $"Document '{documentId}' was not found in the index."));
        }

        return resultBuilder.Build(result);
    }

    /// <summary>
    /// Gets the content of a specific section in a markdown document.
    /// </summary>
    [McpServerTool,
     Description("Get the content of a specific section from a markdown document.")]
    public static async Task<CallToolResult> GetSectionContent(
        IDocumentSearchService documentSearchService,
        IGraphBuildingService buildingService,
        ToolResultBuilder resultBuilder,
        [Description("Section ID from search results")]
        string sectionId)
    {
        var buildError = CheckBuildingState(buildingService, resultBuilder);
        if (buildError != null) return buildError;

        var content = await documentSearchService.GetSectionContentAsync(sectionId);
        if (content == null)
        {
            return resultBuilder.BuildError(ErrorResponse.NotFound(
                $"Section '{sectionId}' was not found in the index."));
        }

        return resultBuilder.Build(new SectionContentResult(sectionId, content));
    }
}

/// <summary>
/// Result containing section content.
/// </summary>
public sealed record SectionContentResult(string SectionId, string Content);
