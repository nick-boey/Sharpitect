using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sharpitect.Analysis.Analyzers;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Test.Analyzers;

[TestFixture]
public class DeclarationVisitorTests
{
    private const string TestFilePath = "test.cs";

    #region Class Declarations

    [Test]
    public void Visit_SimpleClass_ExtractsClassNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass { }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var classNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Class);
        Assert.That(classNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(classNode!.Name, Is.EqualTo("MyClass"));
            Assert.That(classNode.Id, Is.EqualTo("TestNamespace.MyClass"));
        });
    }

    [Test]
    public void Visit_NestedClass_ExtractsNestedClassNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class OuterClass
                                {
                                    public class InnerClass { }
                                }
                            }
                            """;

        var (nodes, edges) = AnalyzeCode(code);

        var classes = nodes.Where(n => n.Kind == DeclarationKind.Class).ToList();
        Assert.That(classes, Has.Count.EqualTo(2));

        var outerClass = classes.Single(n => n.Name == "OuterClass");
        var innerClass = classes.Single(n => n.Name == "InnerClass");

        Assert.That(innerClass.Id, Is.EqualTo("TestNamespace.OuterClass.InnerClass"));

        // Verify containment relationship
        var containmentEdge = edges.SingleOrDefault(e =>
            e.SourceId == outerClass.Id &&
            e.TargetId == innerClass.Id &&
            e.Kind == RelationshipKind.Contains);
        Assert.That(containmentEdge, Is.Not.Null);
    }

    [Test]
    public void Visit_GenericClass_ExtractsClassWithTypeParameter()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class GenericClass<T> { }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var classNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Class);
        Assert.That(classNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(classNode!.Name, Is.EqualTo("GenericClass"));
            Assert.That(classNode.Id, Is.EqualTo("TestNamespace.GenericClass<T>"));
        });

        var typeParam = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.TypeParameter);
        Assert.That(typeParam, Is.Not.Null);
        Assert.That(typeParam!.Name, Is.EqualTo("T"));
    }

    #endregion

    #region Interface Declarations

    [Test]
    public void Visit_Interface_ExtractsInterfaceNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public interface IMyInterface
                                {
                                    void DoSomething();
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var interfaceNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Interface);
        Assert.That(interfaceNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(interfaceNode!.Name, Is.EqualTo("IMyInterface"));
            Assert.That(interfaceNode.Id, Is.EqualTo("TestNamespace.IMyInterface"));
        });
    }

    #endregion

    #region Struct Declarations

    [Test]
    public void Visit_Struct_ExtractsStructNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public struct MyStruct
                                {
                                    public int Value;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var structNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Struct);
        Assert.That(structNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(structNode!.Name, Is.EqualTo("MyStruct"));
            Assert.That(structNode.Id, Is.EqualTo("TestNamespace.MyStruct"));
        });
    }

    #endregion

    #region Record Declarations

    [Test]
    public void Visit_Record_ExtractsRecordNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public record MyRecord(string Name, int Age);
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var recordNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Record);
        Assert.That(recordNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(recordNode!.Name, Is.EqualTo("MyRecord"));
            Assert.That(recordNode.Id, Is.EqualTo("TestNamespace.MyRecord"));
        });
    }

    [Test]
    public void Visit_RecordStruct_ExtractsRecordNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public record struct MyRecordStruct(int X, int Y);
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var recordNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Record);
        Assert.That(recordNode, Is.Not.Null);
        Assert.That(recordNode!.Name, Is.EqualTo("MyRecordStruct"));
    }

    #endregion

    #region Enum Declarations

    [Test]
    public void Visit_Enum_ExtractsEnumNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public enum Status
                                {
                                    Active,
                                    Inactive,
                                    Pending
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var enumNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Enum);
        Assert.That(enumNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(enumNode!.Name, Is.EqualTo("Status"));
            Assert.That(enumNode.Id, Is.EqualTo("TestNamespace.Status"));
        });
    }

    [Test]
    public void Visit_EnumMembers_ExtractsEnumMemberNodes()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public enum Status
                                {
                                    Active,
                                    Inactive,
                                    Pending
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var enumMembers = nodes.Where(n => n.Kind == DeclarationKind.EnumMember).ToList();
        Assert.That(enumMembers, Has.Count.EqualTo(3));
        Assert.That(enumMembers.Select(m => m.Name), Is.EquivalentTo(new[] { "Active", "Inactive", "Pending" }));
    }

    #endregion

    #region Delegate Declarations

    [Test]
    public void Visit_Delegate_ExtractsDelegateNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public delegate void MyDelegate(string message);
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var delegateNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Delegate);
        Assert.That(delegateNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(delegateNode!.Name, Is.EqualTo("MyDelegate"));
            Assert.That(delegateNode.Id, Is.EqualTo("TestNamespace.MyDelegate"));
        });
    }

    #endregion

    #region Method Declarations

    [Test]
    public void Visit_Method_ExtractsMethodNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var methodNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Method);
        Assert.That(methodNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(methodNode!.Name, Is.EqualTo("MyMethod"));
            Assert.That(methodNode.Id, Is.EqualTo("TestNamespace.MyClass.MyMethod()"));
        });
    }

    [Test]
    public void Visit_MethodWithParameters_ExtractsMethodAndParameters()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public int Add(int a, int b) => a + b;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var methodNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Method);
        Assert.That(methodNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(methodNode!.Name, Is.EqualTo("Add"));
            Assert.That(methodNode.Id, Is.EqualTo("TestNamespace.MyClass.Add(int, int)"));
        });

        var parameters = nodes.Where(n => n.Kind == DeclarationKind.Parameter).ToList();
        Assert.That(parameters, Has.Count.EqualTo(2));
        Assert.That(parameters.Select(p => p.Name), Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public void Visit_GenericMethod_ExtractsMethodWithTypeParameter()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public T Identity<T>(T value) => value;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var methodNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Method);
        Assert.That(methodNode, Is.Not.Null);
        Assert.That(methodNode!.Name, Is.EqualTo("Identity"));

        var typeParams = nodes.Where(n => n.Kind == DeclarationKind.TypeParameter).ToList();
        Assert.That(typeParams.Any(tp => tp.Name == "T"), Is.True);
    }

    [Test]
    public void Visit_StaticMethod_ExtractsMethodNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public static class Utilities
                                {
                                    public static int Square(int x) => x * x;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var methodNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Method);
        Assert.That(methodNode, Is.Not.Null);
        Assert.That(methodNode!.Name, Is.EqualTo("Square"));
    }

    #endregion

    #region Constructor Declarations

    [Test]
    public void Visit_Constructor_ExtractsConstructorNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public MyClass() { }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var ctorNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Constructor);
        Assert.That(ctorNode, Is.Not.Null);
        Assert.That(ctorNode!.Name, Is.EqualTo(".ctor"));
    }

    [Test]
    public void Visit_ConstructorWithParameters_ExtractsConstructorAndParameters()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class Person
                                {
                                    public Person(string name, int age) { }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var ctorNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Constructor);
        Assert.That(ctorNode, Is.Not.Null);

        var parameters = nodes.Where(n => n.Kind == DeclarationKind.Parameter).ToList();
        Assert.That(parameters, Has.Count.EqualTo(2));
        Assert.That(parameters.Select(p => p.Name), Is.EquivalentTo(new[] { "name", "age" }));
    }

    #endregion

    #region Property Declarations

    [Test]
    public void Visit_Property_ExtractsPropertyNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public string Name { get; set; }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var propNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Property);
        Assert.That(propNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(propNode!.Name, Is.EqualTo("Name"));
            Assert.That(propNode.Id, Is.EqualTo("TestNamespace.MyClass.Name"));
        });
    }

    [Test]
    public void Visit_AutoProperty_ExtractsPropertyNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public int Count { get; }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var propNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Property);
        Assert.That(propNode, Is.Not.Null);
        Assert.That(propNode!.Name, Is.EqualTo("Count"));
    }

    [Test]
    public void Visit_PropertyWithBackingField_ExtractsBothPropertyAndField()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    private string _name;
                                    public string Name
                                    {
                                        get => _name;
                                        set => _name = value;
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var propNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Property);
        Assert.That(propNode, Is.Not.Null);
        Assert.That(propNode!.Name, Is.EqualTo("Name"));

        var fieldNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Field);
        Assert.That(fieldNode, Is.Not.Null);
        Assert.That(fieldNode!.Name, Is.EqualTo("_name"));
    }

    #endregion

    #region Field Declarations

    [Test]
    public void Visit_Field_ExtractsFieldNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    private int _count;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var fieldNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Field);
        Assert.That(fieldNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fieldNode!.Name, Is.EqualTo("_count"));
            Assert.That(fieldNode.Id, Is.EqualTo("TestNamespace.MyClass._count"));
        });
    }

    [Test]
    public void Visit_MultipleFieldsInSingleDeclaration_ExtractsAllFields()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    private int x, y, z;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var fields = nodes.Where(n => n.Kind == DeclarationKind.Field).ToList();
        Assert.That(fields, Has.Count.EqualTo(3));
        Assert.That(fields.Select(f => f.Name), Is.EquivalentTo(new[] { "x", "y", "z" }));
    }

    [Test]
    public void Visit_ConstField_ExtractsFieldNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public const int MaxValue = 100;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var fieldNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Field);
        Assert.That(fieldNode, Is.Not.Null);
        Assert.That(fieldNode!.Name, Is.EqualTo("MaxValue"));
    }

    #endregion

    #region Event Declarations

    [Test]
    public void Visit_EventField_ExtractsEventNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public event EventHandler MyEvent;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var eventNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Event);
        Assert.That(eventNode, Is.Not.Null);
        Assert.That(eventNode!.Name, Is.EqualTo("MyEvent"));
    }

    [Test]
    public void Visit_EventWithAccessors_ExtractsEventNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    private EventHandler _handler;
                                    public event EventHandler MyEvent
                                    {
                                        add { _handler += value; }
                                        remove { _handler -= value; }
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var eventNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Event);
        Assert.That(eventNode, Is.Not.Null);
        Assert.That(eventNode!.Name, Is.EqualTo("MyEvent"));
    }

    #endregion

    #region Indexer Declarations

    [Test]
    public void Visit_Indexer_ExtractsIndexerNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyCollection
                                {
                                    private int[] _items = new int[10];
                                    public int this[int index]
                                    {
                                        get => _items[index];
                                        set => _items[index] = value;
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var indexerNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Indexer);
        Assert.That(indexerNode, Is.Not.Null);
        Assert.That(indexerNode!.Name, Is.EqualTo("this[]"));
    }

    #endregion

    #region Local Function Declarations

    [Test]
    public void Visit_LocalFunction_ExtractsLocalFunctionNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public int Calculate(int n)
                                    {
                                        int LocalAdd(int a, int b) => a + b;
                                        return LocalAdd(n, n);
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var localFuncNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.LocalFunction);
        Assert.That(localFuncNode, Is.Not.Null);
        Assert.That(localFuncNode!.Name, Is.EqualTo("LocalAdd"));
    }

    [Test]
    public void Visit_NestedLocalFunctions_ExtractsAllLocalFunctions()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void Process()
                                    {
                                        void Outer()
                                        {
                                            void Inner() { }
                                            Inner();
                                        }
                                        Outer();
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var localFuncs = nodes.Where(n => n.Kind == DeclarationKind.LocalFunction).ToList();
        Assert.That(localFuncs, Has.Count.EqualTo(2));
        Assert.That(localFuncs.Select(f => f.Name), Is.EquivalentTo(new[] { "Outer", "Inner" }));
    }

    #endregion

    #region Local Variable Declarations

    [Test]
    public void Visit_LocalVariable_ExtractsLocalVariableNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void Process()
                                    {
                                        int count = 0;
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var localVarNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.LocalVariable);
        Assert.That(localVarNode, Is.Not.Null);
        Assert.That(localVarNode!.Name, Is.EqualTo("count"));
    }

    [Test]
    public void Visit_MultipleLocalVariables_ExtractsAllVariables()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void Process()
                                    {
                                        int a = 1, b = 2, c = 3;
                                    }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        var localVars = nodes.Where(n => n.Kind == DeclarationKind.LocalVariable).ToList();
        Assert.That(localVars, Has.Count.EqualTo(3));
        Assert.That(localVars.Select(v => v.Name), Is.EquivalentTo(new[] { "a", "b", "c" }));
    }

    #endregion

    #region Namespace Declarations

    [Test]
    public void Visit_Namespace_ExtractsNamespaceNode()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass { }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var nsNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Namespace);
        Assert.That(nsNode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(nsNode!.Name, Is.EqualTo("TestNamespace"));
            Assert.That(nsNode.Id, Is.EqualTo("TestNamespace"));
        });
    }

    [Test]
    public void Visit_NestedNamespaces_ExtractsAllNamespaces()
    {
        const string code = """
                            namespace Outer
                            {
                                namespace Inner
                                {
                                    public class MyClass { }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var namespaces = nodes.Where(n => n.Kind == DeclarationKind.Namespace).ToList();
        Assert.That(namespaces, Has.Count.EqualTo(2));
        Assert.That(namespaces.Select(ns => ns.Name), Is.EquivalentTo(new[] { "Outer", "Inner" }));
    }

    [Test]
    public void Visit_FileScopedNamespace_ExtractsNamespaceNode()
    {
        const string code = """
                            namespace TestNamespace;

                            public class MyClass { }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var nsNode = nodes.SingleOrDefault(n => n.Kind == DeclarationKind.Namespace);
        Assert.That(nsNode, Is.Not.Null);
        Assert.That(nsNode!.Name, Is.EqualTo("TestNamespace"));
    }

    #endregion

    #region Containment Relationships

    [Test]
    public void Visit_ClassContainsMethod_CreatesContainmentEdge()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (nodes, edges) = AnalyzeCode(code);

        var classNode = nodes.Single(n => n.Kind == DeclarationKind.Class);
        var methodNode = nodes.Single(n => n.Kind == DeclarationKind.Method);

        var containsEdge = edges.SingleOrDefault(e =>
            e.SourceId == classNode.Id &&
            e.TargetId == methodNode.Id &&
            e.Kind == RelationshipKind.Contains);

        Assert.That(containsEdge, Is.Not.Null);
    }

    [Test]
    public void Visit_NamespaceContainsClass_CreatesContainmentEdge()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass { }
                            }
                            """;

        var (nodes, edges) = AnalyzeCode(code);

        var nsNode = nodes.Single(n => n.Kind == DeclarationKind.Namespace);
        var classNode = nodes.Single(n => n.Kind == DeclarationKind.Class);

        var containsEdge = edges.SingleOrDefault(e =>
            e.SourceId == nsNode.Id &&
            e.TargetId == classNode.Id &&
            e.Kind == RelationshipKind.Contains);

        Assert.That(containsEdge, Is.Not.Null);
    }

    [Test]
    public void Visit_MethodContainsLocalFunction_CreatesContainmentEdge()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod()
                                    {
                                        void LocalFunc() { }
                                    }
                                }
                            }
                            """;

        var (nodes, edges) = AnalyzeCode(code, true);

        var methodNode = nodes.Single(n => n.Kind == DeclarationKind.Method);
        var localFuncNode = nodes.Single(n => n.Kind == DeclarationKind.LocalFunction);

        var containsEdge = edges.SingleOrDefault(e =>
            e.SourceId == methodNode.Id &&
            e.TargetId == localFuncNode.Id &&
            e.Kind == RelationshipKind.Contains);

        Assert.That(containsEdge, Is.Not.Null);
    }

    #endregion

    #region Location Information

    [Test]
    public void Visit_Declaration_CapturesCorrectLineNumbers()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var classNode = nodes.Single(n => n.Kind == DeclarationKind.Class);
        Assert.That(classNode.StartLine, Is.EqualTo(3));

        var methodNode = nodes.Single(n => n.Kind == DeclarationKind.Method);
        Assert.That(methodNode.StartLine, Is.EqualTo(5));
    }

    [Test]
    public void Visit_Declaration_CapturesFilePath()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass { }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var classNode = nodes.Single(n => n.Kind == DeclarationKind.Class);
        Assert.That(classNode.FilePath, Is.EqualTo(TestFilePath));
    }

    #endregion

    #region Symbol to Node ID Mapping

    [Test]
    public void Visit_Declaration_PopulatesSymbolToNodeIdMapping()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class MyClass
                                {
                                    public void MyMethod() { }
                                }
                            }
                            """;

        var (tree, semanticModel) = CreateCompilation(code);
        var visitor = new DeclarationVisitor(semanticModel, TestFilePath);
        visitor.Visit(tree.GetRoot());

        Assert.That(visitor.SymbolToNodeId, Is.Not.Empty);
        Assert.That(visitor.SymbolToNodeId.Count, Is.GreaterThanOrEqualTo(3)); // At least namespace, class, method
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public void Visit_FullClassWithAllMemberTypes_ExtractsAllDeclarations()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class CompleteClass
                                {
                                    private int _field;
                                    public const string Constant = "value";

                                    public event EventHandler Changed;

                                    public CompleteClass() { }

                                    public string Name { get; set; }

                                    public void DoWork(int param)
                                    {
                                        int localVar = 0;
                                        void LocalFunc() { }
                                    }

                                    public int this[int index] => _field;
                                }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code, true);

        Assert.Multiple(() =>
        {
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Namespace), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Class), Is.True);
            Assert.That(nodes.Count(n => n.Kind == DeclarationKind.Field), Is.EqualTo(2)); // _field and Constant
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Event), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Constructor), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Property), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Method), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Parameter), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.LocalVariable), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.LocalFunction), Is.True);
            Assert.That(nodes.Any(n => n.Kind == DeclarationKind.Indexer), Is.True);
        });
    }

    [Test]
    public void Visit_MultipleClassesInSameNamespace_ExtractsAllClasses()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public class ClassA { }
                                public class ClassB { }
                                public class ClassC { }
                            }
                            """;

        var (nodes, _) = AnalyzeCode(code);

        var classes = nodes.Where(n => n.Kind == DeclarationKind.Class).ToList();
        Assert.That(classes, Has.Count.EqualTo(3));
        Assert.That(classes.Select(c => c.Name), Is.EquivalentTo(new[] { "ClassA", "ClassB", "ClassC" }));
    }

    #endregion

    #region Helper Methods

    private static (List<DeclarationNode> Nodes, List<RelationshipEdge> Edges) AnalyzeCode(string code,
        bool visitLocals = false)
    {
        var (tree, semanticModel) = CreateCompilation(code);
        var visitor = new DeclarationVisitor(semanticModel, TestFilePath, visitLocals);
        visitor.Visit(tree.GetRoot());
        return (visitor.Nodes, visitor.ContainmentEdges);
    }

    private static (SyntaxTree Tree, SemanticModel SemanticModel) CreateCompilation(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EventHandler).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        return (tree, semanticModel);
    }

    #endregion
}