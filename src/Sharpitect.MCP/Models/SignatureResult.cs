namespace Sharpitect.MCP.Models;

/// <summary>
/// A method parameter.
/// </summary>
public sealed record ParameterInfo(
    string Name,
    string Type,
    bool IsOptional);

/// <summary>
/// Result of a get_signature operation.
/// </summary>
public sealed record SignatureResult(
    string Id,
    string Name,
    string Kind,
    string? ReturnType,
    IReadOnlyList<ParameterInfo> Parameters,
    IReadOnlyList<string> Modifiers,
    bool IsAsync,
    bool IsStatic,
    IReadOnlyList<string> TypeParameters,
    string? Documentation);
