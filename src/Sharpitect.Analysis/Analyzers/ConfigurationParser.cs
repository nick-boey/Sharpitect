using Sharpitect.Analysis.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Parses YAML configuration files (.sln.c4, .csproj.c4).
/// </summary>
public class ConfigurationParser
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParser"/> class.
    /// </summary>
    public ConfigurationParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses a system configuration from YAML content.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed system configuration, or null if the input is empty.</returns>
    public SystemConfiguration? ParseSystemConfiguration(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return null;
        }

        return _deserializer.Deserialize<SystemConfiguration>(yaml);
    }

    /// <summary>
    /// Parses a container configuration from YAML content.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed container configuration, or null if the input is empty.</returns>
    public ContainerConfiguration? ParseContainerConfiguration(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return null;
        }

        return _deserializer.Deserialize<ContainerConfiguration>(yaml);
    }
}
