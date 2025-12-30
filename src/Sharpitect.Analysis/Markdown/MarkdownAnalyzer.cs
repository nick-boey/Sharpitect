using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Markdown;

/// <summary>
/// Markdig-based markdown analyzer that produces graph nodes and edges.
/// Extracts document structure (headings), content sections, and links.
/// </summary>
public sealed partial class MarkdownAnalyzer : IMarkdownAnalyzer
{
    private readonly MarkdownChunkingOptions _options;
    private readonly MarkdownPipeline _pipeline;

    // Regex for Obsidian-style wikilinks: [[target]] or [[target|display]]
    [GeneratedRegex(@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]", RegexOptions.Compiled)]
    private static partial Regex WikilinkRegex();

    /// <summary>
    /// Creates a new markdown analyzer with default options.
    /// </summary>
    public MarkdownAnalyzer() : this(new MarkdownChunkingOptions())
    {
    }

    /// <summary>
    /// Creates a new markdown analyzer with custom options.
    /// </summary>
    /// <param name="options">Chunking configuration options.</param>
    public MarkdownAnalyzer(MarkdownChunkingOptions options)
    {
        _options = options;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <inheritdoc />
    public async Task<MarkdownAnalysisResult> AnalyzeAsync(
        string filePath,
        string solutionRootDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(solutionRootDirectory);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Markdown file not found: {filePath}", filePath);
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var contentHash = ComputeHash(content);
        var documentId = GetRelativePath(filePath, solutionRootDirectory);
        var documentDir = Path.GetDirectoryName(documentId) ?? "";

        var markdownDocument = Markdig.Markdown.Parse(content, _pipeline);
        var title = ExtractTitle(markdownDocument, filePath);

        var nodes = new List<DeclarationNode>();
        var edges = new List<RelationshipEdge>();
        var sectionContents = new Dictionary<string, string>();

        // Create document node
        var metadata = JsonSerializer.Serialize(new { contentHash, title });
        var documentNode = new DeclarationNode
        {
            Id = documentId,
            Name = Path.GetFileName(filePath),
            Kind = DeclarationKind.MarkdownDocument,
            FilePath = documentId,
            StartLine = 1,
            StartColumn = 1,
            EndLine = content.Split('\n').Length,
            EndColumn = 1,
            Metadata = metadata
        };
        nodes.Add(documentNode);

        // Extract headings and create hierarchy
        var headingNodes = ExtractHeadings(markdownDocument, documentId, content);
        nodes.AddRange(headingNodes);

        // Create containment edges for heading hierarchy
        var containmentEdges = CreateHeadingContainmentEdges(documentId, headingNodes);
        edges.AddRange(containmentEdges);

        // Extract sections and create section nodes
        var (sectionNodes, sectionContentMap) = ExtractSections(markdownDocument, content, documentId, headingNodes);
        nodes.AddRange(sectionNodes);
        foreach (var (id, sectionContent) in sectionContentMap)
        {
            sectionContents[id] = sectionContent;
        }

        // Create containment edges for sections
        var sectionEdges = CreateSectionContainmentEdges(documentId, sectionNodes, headingNodes);
        edges.AddRange(sectionEdges);

        // Extract links
        var linkEdges = ExtractLinks(markdownDocument, content, documentId, documentDir, solutionRootDirectory);
        edges.AddRange(linkEdges);

        return new MarkdownAnalysisResult
        {
            ContentHash = contentHash,
            Nodes = nodes,
            Edges = edges,
            SectionContents = sectionContents
        };
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetRelativePath(string filePath, string rootDirectory)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fullRoot = Path.GetFullPath(rootDirectory);

        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath[fullRoot.Length..].Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
    }

    private static string? ExtractTitle(MarkdownDocument document, string filePath)
    {
        foreach (var block in document)
        {
            if (block is HeadingBlock heading && heading.Level == 1)
            {
                return GetHeadingText(heading);
            }
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    private static string GetHeadingText(HeadingBlock heading)
    {
        var sb = new StringBuilder();
        if (heading.Inline != null)
        {
            foreach (var inline in heading.Inline)
            {
                AppendInlineText(sb, inline);
            }
        }
        return sb.ToString().Trim();
    }

    private static void AppendInlineText(StringBuilder sb, Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                sb.Append(literal.Content);
                break;
            case ContainerInline container:
                foreach (var child in container)
                {
                    AppendInlineText(sb, child);
                }
                break;
        }
    }

    private static string CreateSlug(string text)
    {
        // Simple slug generation: lowercase, replace spaces with hyphens, remove special chars
        var slug = text.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('\t', '-');

        // Remove non-alphanumeric except hyphens
        var sb = new StringBuilder();
        foreach (var c in slug)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sb.Append(c);
            }
        }

        // Collapse multiple hyphens
        var result = sb.ToString();
        while (result.Contains("--"))
        {
            result = result.Replace("--", "-");
        }

        return result.Trim('-');
    }

    private List<DeclarationNode> ExtractHeadings(
        MarkdownDocument document,
        string documentId,
        string content)
    {
        var nodes = new List<DeclarationNode>();
        var lines = content.Split('\n');

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                var text = GetHeadingText(heading);
                var slug = CreateSlug(text);
                var headingId = $"{documentId}#{slug}";

                // Store heading level in metadata
                var metadata = JsonSerializer.Serialize(new { level = heading.Level });

                nodes.Add(new DeclarationNode
                {
                    Id = headingId,
                    Name = text,
                    Kind = DeclarationKind.MarkdownHeading,
                    FilePath = documentId,
                    StartLine = heading.Line + 1, // 0-based to 1-based
                    StartColumn = heading.Column + 1,
                    EndLine = heading.Line + 1,
                    EndColumn = lines.Length > heading.Line ? lines[heading.Line].Length + 1 : 1,
                    Metadata = metadata
                });
            }
        }

        return nodes;
    }

    private List<RelationshipEdge> CreateHeadingContainmentEdges(
        string documentId,
        List<DeclarationNode> headingNodes)
    {
        var edges = new List<RelationshipEdge>();
        var headingStack = new Stack<(int Level, string Id)>();

        foreach (var heading in headingNodes)
        {
            var level = GetHeadingLevel(heading);

            // Pop headings that are same level or deeper
            while (headingStack.Count > 0 && headingStack.Peek().Level >= level)
            {
                headingStack.Pop();
            }

            // Parent is either the top of stack or the document
            var parentId = headingStack.Count > 0 ? headingStack.Peek().Id : documentId;

            edges.Add(new RelationshipEdge
            {
                Id = $"{parentId}->contains->{heading.Id}",
                SourceId = parentId,
                TargetId = heading.Id,
                Kind = RelationshipKind.Contains,
                SourceFilePath = documentId,
                SourceLine = heading.StartLine
            });

            headingStack.Push((level, heading.Id));
        }

        return edges;
    }

    private static int GetHeadingLevel(DeclarationNode heading)
    {
        if (heading.Metadata == null)
        {
            return 1;
        }

        try
        {
            using var doc = JsonDocument.Parse(heading.Metadata);
            if (doc.RootElement.TryGetProperty("level", out var levelElement))
            {
                return levelElement.GetInt32();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 1;
    }

    private (List<DeclarationNode> Nodes, Dictionary<string, string> Contents) ExtractSections(
        MarkdownDocument document,
        string content,
        string documentId,
        List<DeclarationNode> headingNodes)
    {
        var nodes = new List<DeclarationNode>();
        var contents = new Dictionary<string, string>();
        var lines = content.Split('\n');

        // Find content ranges between headings
        var headingLines = headingNodes.Select(h => h.StartLine - 1).ToList(); // Convert to 0-based
        headingLines.Insert(0, -1); // Before first heading
        headingLines.Add(lines.Length); // After last heading

        var chunkIndex = 0;

        for (int i = 0; i < headingLines.Count - 1; i++)
        {
            var startLine = headingLines[i] + 1; // Start after the heading line
            var endLine = headingLines[i + 1]; // End before the next heading

            if (startLine >= endLine)
            {
                continue;
            }

            // Extract content between headings
            var sectionLines = lines.Skip(startLine).Take(endLine - startLine).ToArray();
            var sectionContent = string.Join("\n", sectionLines).Trim();

            if (sectionContent.Length < _options.MinChunkSize)
            {
                continue;
            }

            // Determine parent (heading above this section or document)
            string parentId;
            if (i == 0)
            {
                parentId = documentId;
            }
            else
            {
                var headingIndex = i - 1;
                parentId = headingIndex < headingNodes.Count ? headingNodes[headingIndex].Id : documentId;
            }

            // Get slug from parent heading
            var parentSlug = parentId.Contains('#') ? parentId.Split('#')[1] : "";

            // Chunk the section if needed
            var chunks = ChunkText(sectionContent, _options.MaxChunkSize, _options.ChunkOverlap);

            foreach (var chunk in chunks)
            {
                var sectionId = string.IsNullOrEmpty(parentSlug)
                    ? $"{documentId}_chunk_{chunkIndex}"
                    : $"{documentId}#{parentSlug}_chunk_{chunkIndex}";

                var sectionNode = new DeclarationNode
                {
                    Id = sectionId,
                    Name = $"Section {chunkIndex}",
                    Kind = DeclarationKind.MarkdownSection,
                    FilePath = documentId,
                    StartLine = startLine + 1, // 1-based
                    StartColumn = 1,
                    EndLine = endLine, // 1-based (exclusive becomes inclusive)
                    EndColumn = 1
                };

                nodes.Add(sectionNode);
                contents[sectionId] = chunk;
                chunkIndex++;
            }
        }

        return (nodes, contents);
    }

    private List<string> ChunkText(string text, int maxSize, int overlap)
    {
        var chunks = new List<string>();

        if (text.Length <= maxSize)
        {
            chunks.Add(text);
            return chunks;
        }

        var position = 0;
        while (position < text.Length)
        {
            var remaining = text.Length - position;
            var chunkSize = Math.Min(maxSize, remaining);

            // Try to break at a sentence or word boundary
            if (position + chunkSize < text.Length)
            {
                var breakPoint = FindBreakPoint(text, position, position + chunkSize);
                if (breakPoint > position)
                {
                    chunkSize = breakPoint - position;
                }
            }

            var chunkContent = text.Substring(position, chunkSize).Trim();

            if (chunkContent.Length >= _options.MinChunkSize)
            {
                chunks.Add(chunkContent);
            }

            position += chunkSize - overlap;

            if (chunkSize <= overlap)
            {
                break;
            }
        }

        return chunks;
    }

    private static int FindBreakPoint(string text, int start, int end)
    {
        // Try sentence boundary
        for (int i = end - 1; i >= start + (end - start) / 2; i--)
        {
            if (text[i] is '.' or '!' or '?' && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
            {
                return i + 1;
            }
        }

        // Try paragraph boundary
        for (int i = end - 1; i >= start + (end - start) / 2; i--)
        {
            if (text[i] == '\n' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                return i + 1;
            }
        }

        // Fall back to word boundary
        for (int i = end - 1; i >= start + (end - start) / 2; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        return end;
    }

    private List<RelationshipEdge> CreateSectionContainmentEdges(
        string documentId,
        List<DeclarationNode> sectionNodes,
        List<DeclarationNode> headingNodes)
    {
        var edges = new List<RelationshipEdge>();

        foreach (var section in sectionNodes)
        {
            // Find the parent heading for this section based on line numbers
            DeclarationNode? parentHeading = null;
            foreach (var heading in headingNodes)
            {
                if (heading.StartLine < section.StartLine)
                {
                    if (parentHeading == null || heading.StartLine > parentHeading.StartLine)
                    {
                        parentHeading = heading;
                    }
                }
            }

            var parentId = parentHeading?.Id ?? documentId;

            edges.Add(new RelationshipEdge
            {
                Id = $"{parentId}->contains->{section.Id}",
                SourceId = parentId,
                TargetId = section.Id,
                Kind = RelationshipKind.Contains,
                SourceFilePath = documentId,
                SourceLine = section.StartLine
            });
        }

        return edges;
    }

    private List<RelationshipEdge> ExtractLinks(
        MarkdownDocument document,
        string content,
        string documentId,
        string documentDir,
        string solutionRootDirectory)
    {
        var edges = new List<RelationshipEdge>();
        var linkIndex = 0;

        // Extract standard markdown links
        foreach (var block in document.Descendants())
        {
            if (block is LinkInline link && !string.IsNullOrEmpty(link.Url))
            {
                var targetId = ResolveLinkTarget(link.Url, documentDir, solutionRootDirectory);
                if (targetId != null)
                {
                    var metadata = JsonSerializer.Serialize(new
                    {
                        linkText = GetLinkText(link),
                        isWikilink = false
                    });

                    edges.Add(new RelationshipEdge
                    {
                        Id = $"{documentId}->links->{linkIndex++}",
                        SourceId = documentId,
                        TargetId = targetId,
                        Kind = RelationshipKind.LinksTo,
                        SourceFilePath = documentId,
                        SourceLine = link.Line + 1,
                        Metadata = metadata
                    });
                }
            }
        }

        // Extract Obsidian-style wikilinks
        var wikilinkMatches = WikilinkRegex().Matches(content);
        foreach (Match match in wikilinkMatches)
        {
            var target = match.Groups[1].Value;
            var targetId = ResolveWikilinkTarget(target, documentDir, solutionRootDirectory);

            var metadata = JsonSerializer.Serialize(new
            {
                linkText = target,
                isWikilink = true
            });

            // Find line number for this match
            var lineNumber = content[..match.Index].Count(c => c == '\n') + 1;

            edges.Add(new RelationshipEdge
            {
                Id = $"{documentId}->links->{linkIndex++}",
                SourceId = documentId,
                TargetId = targetId,
                Kind = RelationshipKind.LinksTo,
                SourceFilePath = documentId,
                SourceLine = lineNumber,
                Metadata = metadata
            });
        }

        return edges;
    }

    private static string GetLinkText(LinkInline link)
    {
        var sb = new StringBuilder();
        if (link.FirstChild != null)
        {
            foreach (var child in link)
            {
                AppendInlineText(sb, child);
            }
        }
        return sb.ToString().Trim();
    }

    private static string? ResolveLinkTarget(string url, string documentDir, string solutionRootDirectory)
    {
        // Skip external links
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Skip anchors-only links
        if (url.StartsWith('#'))
        {
            return null;
        }

        // Handle relative paths
        var targetPath = url;
        var anchor = "";

        // Split path and anchor
        var anchorIndex = targetPath.IndexOf('#');
        if (anchorIndex >= 0)
        {
            anchor = targetPath[anchorIndex..];
            targetPath = targetPath[..anchorIndex];
        }

        // Resolve relative to document directory
        if (!string.IsNullOrEmpty(documentDir))
        {
            targetPath = Path.Combine(documentDir, targetPath);
        }

        // Normalize path separators
        targetPath = targetPath.Replace('\\', '/');

        // Normalize path (handle ../)
        var parts = targetPath.Split('/').ToList();
        var normalized = new List<string>();
        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (normalized.Count > 0)
                {
                    normalized.RemoveAt(normalized.Count - 1);
                }
            }
            else if (part != "." && !string.IsNullOrEmpty(part))
            {
                normalized.Add(part);
            }
        }

        targetPath = string.Join("/", normalized);

        return targetPath + anchor;
    }

    private static string ResolveWikilinkTarget(string target, string documentDir, string solutionRootDirectory)
    {
        var anchor = "";

        // Split path and anchor
        var anchorIndex = target.IndexOf('#');
        if (anchorIndex >= 0)
        {
            anchor = "#" + CreateSlug(target[(anchorIndex + 1)..]);
            target = target[..anchorIndex];
        }

        // Wikilinks are typically relative to document or a docs root
        // Add .md extension if not present
        if (!target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            target += ".md";
        }

        // Try to resolve relative to document directory
        var targetPath = string.IsNullOrEmpty(documentDir)
            ? target
            : Path.Combine(documentDir, target).Replace('\\', '/');

        return targetPath + anchor;
    }
}
