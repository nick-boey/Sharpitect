using Sharpitect.Analysis.Configuration;
using Sharpitect.Analysis.Configuration.Definitions;
using Sharpitect.Analysis.Model;

namespace Sharpitect.Analysis.Analyzers;

/// <summary>
/// Main entry point for analyzing a solution.
/// Orchestrates parsing of YAML configs and C# code.
/// </summary>
public class SolutionAnalyzer
{
    private readonly ISourceProvider _sourceProvider;
    private readonly ConfigurationParser _configParser;
    private readonly CodeAnalyzer _codeAnalyzer;
    private readonly ModelBuilder _modelBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionAnalyzer"/> class.
    /// </summary>
    /// <param name="sourceProvider">The source provider for accessing files.</param>
    public SolutionAnalyzer(ISourceProvider sourceProvider)
    {
        _sourceProvider = sourceProvider;
        _configParser = new ConfigurationParser();
        _codeAnalyzer = new CodeAnalyzer();
        _modelBuilder = new ModelBuilder();
    }

    /// <summary>
    /// Analyzes a solution and builds the architecture model.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>The complete architecture model.</returns>
    public ArchitectureModel Analyze(string solutionPath)
    {
        var model = new ArchitectureModel();

        // Parse system-level configuration if it exists, otherwise adopt the same name as the solution.
        var systemYaml = _sourceProvider.GetYamlConfiguration(solutionPath + ".yml");
        var systemConfig = _configParser.ParseSystemConfiguration(systemYaml ?? string.Empty) ?? new SystemConfiguration
        {
            System = new SystemDefinition
            {
                Name = Path.GetFileNameWithoutExtension(solutionPath),
            }
        };

        // Build system from config
        var system = ModelBuilder.BuildSystem(systemConfig);
        model.AddSystem(system);

        // Build maps for relationship resolution
        var peopleMap = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase);
        var componentMap = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);

        // Add people, external systems, external containers
        foreach (var personDef in systemConfig.People ?? Enumerable.Empty<PersonDefinition>())
        {
            var person = new Person(personDef.Name, personDef.Description);
            model.AddPerson(person);
            peopleMap[personDef.Name] = person;
        }

        foreach (var extSysDef in systemConfig.ExternalSystems ?? Enumerable.Empty<ExternalSystemDefinition>())
        {
            model.AddExternalSystem(new ExternalSystem(extSysDef.Name, extSysDef.Description));
        }

        foreach (var extContDef in systemConfig.ExternalContainers ??
                                   Enumerable.Empty<ExternalContainerDefinition>())
        {
            model.AddExternalContainer(new ExternalContainer(extContDef.Name, extContDef.Technology,
                extContDef.Description));
        }

        // Analyse each executable project (container)
        var projects = _sourceProvider.GetProjects(solutionPath)
            .Where(p => _sourceProvider.IsExecutableProject(p));
        var allTypes = new List<TypeAnalysisResult>();

        foreach (var projectPath in projects)
        {
            var (container, types) = AnalyzeProject(projectPath);
            system.AddContainer(container);
            allTypes.AddRange(types);

            // Build component map for relationship resolution
            foreach (var component in container.Components)
            {
                componentMap[component.Name] = component;
            }
        }

        // Build relationships
        _modelBuilder.BuildRelationships(model, allTypes, componentMap, peopleMap);

        return model;
    }

    private (Container Container, List<TypeAnalysisResult> Types) AnalyzeProject(string projectPath)
    {
        // Parse container configuration
        var containerYaml = _sourceProvider.GetYamlConfiguration(projectPath + ".yml");
        var containerConfig = _configParser.ParseContainerConfiguration(containerYaml ?? string.Empty);

        var container = ModelBuilder.BuildContainer(containerConfig, projectPath);

        // Analyze all source files
        var sourceFiles = _sourceProvider.GetSourceFiles(projectPath);
        var allTypes = new List<TypeAnalysisResult>();

        foreach (var sourceFile in sourceFiles)
        {
            var source = _sourceProvider.GetSourceCode(sourceFile);
            var result = _codeAnalyzer.AnalyzeSource(source, sourceFile);
            allTypes.AddRange(result.Types);
        }

        // Build components from analysis
        ModelBuilder.BuildComponents(container, allTypes, containerConfig?.Components);

        return (container, allTypes);
    }
}