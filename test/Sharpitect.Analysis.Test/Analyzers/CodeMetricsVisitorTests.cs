using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class CodeMetricsVisitorTests
{
    [Test]
    public void Should_Calculate_Method_Metrics()
    {
        // Arrange
        var code = @"
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Calculator.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("Add"));
        Assert.That(methodMetrics, Is.Not.Null);
        Assert.That(methodMetrics.Kind, Is.EqualTo(DeclarationKind.CodeMetrics));
        Assert.That(methodMetrics.Name, Is.EqualTo("Code Metrics"));
        Assert.That(methodMetrics.FilePath, Is.EqualTo("Calculator.cs"));

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(1));
        Assert.That(metadata["metricType"].GetString(), Is.EqualTo("method"));
    }

    [Test]
    public void Should_Calculate_Cyclomatic_Complexity_For_If_Statement()
    {
        // Arrange
        var code = @"
            public class Validator
            {
                public bool IsValid(int value)
                {
                    if (value > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Validator.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("IsValid"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(2)); // 1 base + 1 if
    }

    [Test]
    public void Should_Calculate_Cyclomatic_Complexity_For_Loops()
    {
        // Arrange
        var code = @"
            public class Processor
            {
                public void Process(int[] values)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i] > 0)
                        {
                            while (values[i] > 10)
                            {
                                values[i]--;
                            }
                        }
                    }
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Processor.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("Process") && !n.Id.Contains("Processor$METRICS"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(4)); // 1 base + 1 for + 1 if + 1 while
    }

    [Test]
    public void Should_Calculate_Cyclomatic_Complexity_For_Switch()
    {
        // Arrange
        var code = @"
            public class Handler
            {
                public string Handle(int value)
                {
                    return value switch
                    {
                        1 => ""One"",
                        2 => ""Two"",
                        3 => ""Three"",
                        _ => ""Other""
                    };
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Handler.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("Handle") && !n.Id.Contains("Handler$METRICS"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(5)); // 1 base + 4 switch arms
    }

    [Test]
    public void Should_Calculate_Cyclomatic_Complexity_For_Logical_Operators()
    {
        // Arrange
        var code = @"
            public class Checker
            {
                public bool Check(int a, int b, int c)
                {
                    return a > 0 && b > 0 || c > 0;
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Checker.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("Check") && !n.Id.Contains("Checker$METRICS"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(3)); // 1 base + 1 && + 1 ||
    }

    [Test]
    public void Should_Calculate_Type_Metrics()
    {
        // Arrange
        var code = @"
            public class Person
            {
                private string name;
                private int age;

                public string Name { get; set; }
                public int Age { get; set; }

                public Person(string name, int age)
                {
                    this.name = name;
                    this.age = age;
                }

                public void Greet()
                {
                    Console.WriteLine(""Hello"");
                }

                public void Farewell()
                {
                    Console.WriteLine(""Goodbye"");
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Person.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var typeMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.EndsWith("Person$METRICS"));
        Assert.That(typeMetrics, Is.Not.Null);
        Assert.That(typeMetrics.Kind, Is.EqualTo(DeclarationKind.CodeMetrics));

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(typeMetrics.Metadata);
        Assert.That(metadata["methodCount"].GetInt32(), Is.EqualTo(3)); // Constructor + 2 methods
        Assert.That(metadata["propertyCount"].GetInt32(), Is.EqualTo(2)); // Name, Age
        Assert.That(metadata["fieldCount"].GetInt32(), Is.EqualTo(2)); // name, age
        Assert.That(metadata["totalMembers"].GetInt32(), Is.EqualTo(7)); // 3 + 2 + 2
        Assert.That(metadata["metricType"].GetString(), Is.EqualTo("type"));
    }

    [Test]
    public void Should_Create_Containment_Edges()
    {
        // Arrange
        var code = @"
            public class Sample
            {
                public void DoWork()
                {
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Sample.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        Assert.That(visitor.ContainmentEdges, Has.Count.GreaterThan(0));
        
        var edge = visitor.ContainmentEdges.First();
        Assert.That(edge.Kind, Is.EqualTo(RelationshipKind.Contains));
        Assert.That(edge.SourceFilePath, Is.EqualTo("Sample.cs"));
        Assert.That(edge.SourceId, Does.Not.Contain("$METRICS"));
        Assert.That(edge.TargetId, Does.Contain("$METRICS"));
    }

    [Test]
    public void Should_Calculate_Lines_Of_Code()
    {
        // Arrange
        var code = @"
            public class MultiLine
            {
                public void LongMethod()
                {
                    var x = 1;
                    var y = 2;
                    var z = 3;
                    var result = x + y + z;
                    Console.WriteLine(result);
                }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "MultiLine.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("LongMethod"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["linesOfCode"].GetInt32(), Is.GreaterThan(1));
    }

    [Test]
    public void Should_Handle_Expression_Bodied_Members()
    {
        // Arrange
        var code = @"
            public class Calculator
            {
                public int Double(int x) => x * 2;
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Calculator.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var methodMetrics = visitor.MetricsNodes.FirstOrDefault(n => n.Id.Contains("Double"));
        Assert.That(methodMetrics, Is.Not.Null);

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(methodMetrics.Metadata);
        Assert.That(metadata["cyclomaticComplexity"].GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public void Should_Calculate_Metrics_For_Struct()
    {
        // Arrange
        var code = @"
            public struct Point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Point.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var typeMetrics = visitor.MetricsNodes.FirstOrDefault();
        Assert.That(typeMetrics, Is.Not.Null);
        Assert.That(typeMetrics.Kind, Is.EqualTo(DeclarationKind.CodeMetrics));

        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(typeMetrics.Metadata);
        Assert.That(metadata["propertyCount"].GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public void Should_Calculate_Metrics_For_Record()
    {
        // Arrange
        var code = @"
            public record Person(string Name, int Age);
        ";

        var (syntaxTree, semanticModel, symbolToNodeId) = CreateTestContext(code);
        var visitor = new CodeMetricsVisitor(semanticModel, "Person.cs", symbolToNodeId);

        // Act
        visitor.Visit(syntaxTree.GetRoot());

        // Assert
        var typeMetrics = visitor.MetricsNodes.FirstOrDefault();
        Assert.That(typeMetrics, Is.Not.Null);
        Assert.That(typeMetrics.Kind, Is.EqualTo(DeclarationKind.CodeMetrics));
    }

    private static (SyntaxTree syntaxTree, SemanticModel semanticModel, Dictionary<ISymbol, string> symbolToNodeId) 
        CreateTestContext(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        // Build symbol to node ID map
        var symbolToNodeId = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        var root = syntaxTree.GetRoot();
        var declarationVisitor = new DeclarationVisitor(semanticModel, "Test.cs", visitLocals: false);
        declarationVisitor.Visit(root);

        foreach (var kvp in declarationVisitor.SymbolToNodeId)
        {
            symbolToNodeId[kvp.Key] = kvp.Value;
        }

        return (syntaxTree, semanticModel, symbolToNodeId);
    }
}
