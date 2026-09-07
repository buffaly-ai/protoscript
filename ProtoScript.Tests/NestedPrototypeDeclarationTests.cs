using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ontology;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests;

[TestClass]
[DoNotParallelize]
public class NestedPrototypeDeclarationTests
{
    private static Compiler Compile(string source)
    {
        var compiler = new Compiler();
        compiler.Initialize();
        string directory = Path.Combine(Path.GetTempPath(), "nesting-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "Project.pts");
            System.IO.File.WriteAllText(path, source);
            var statements = compiler.CompileProject(path);
            var before = TemporaryPrototypes.GetAllTemporaryPrototypes().Where(x => x.NestedParent != null).Select(x => x.PrototypeName + "->" + x.NestedParent.PrototypeName).OrderBy(x => x).ToArray();
            new NativeInterpretter(compiler).InterpretStatements(statements);
            CollectionAssert.AreEqual(before, TemporaryPrototypes.GetAllTemporaryPrototypes().Where(x => x.NestedParent != null).Select(x => x.PrototypeName + "->" + x.NestedParent.PrototypeName).OrderBy(x => x).ToArray());
            return compiler;
        }
        finally { Directory.Delete(directory, true); }
    }

    private static Prototype P(string name) => TemporaryPrototypes.GetTemporaryPrototype(name);
    private static void Edge(string parent, string child)
    {
        Assert.AreEqual(P(parent).PrototypeID, P(child).NestedParent?.PrototypeID);
        Assert.AreEqual(1, P(parent).GetNestedChildren().Count(x => x.PrototypeID == P(child).PrototypeID));
    }

    [TestMethod]
    public void LexicalNesting_GrandchildrenAndSameLeafSiblings()
    {
        Compile("prototype NestA { prototype Child { prototype Grand; } prototype Other; } prototype NestB { prototype Child; }");
        Edge("NestA", "NestA.Child"); Edge("NestA.Child", "NestA.Child.Grand");
        Edge("NestA", "NestA.Other"); Edge("NestB", "NestB.Child");
        Assert.AreEqual(2, P("NestA").GetNestedChildren().Count());
        Assert.IsFalse(P("NestA").Children.Any(x => x.PrototypeID == P("NestA.Child").PrototypeID));
    }

    [TestMethod]
    public void GenericValuesAndDottedNames_DoNotInferNesting()
    {
        Compile("prototype ValueOwner;");
        for (int i = 0; i < 1000; i++)
        {
            var value = TemporaryPrototypes.GetOrCreateTemporaryPrototype("ValueOwner.value" + i);
            Assert.IsNull(value.NestedParent);
            var wrapped = NativeValuePrototype.GetOrCreateNativeValuePrototype("ValueOwner.value" + i);
            Assert.IsNull(wrapped.NestedParent);
        }
        Assert.AreEqual(0, P("ValueOwner").GetNestedChildren().Count());
    }

    [TestMethod]
    public void DeepAndWideDeclarations_LinkEveryImmediateEdge()
    {
        string children = string.Join(" ", Enumerable.Range(0, 1000).Select(i => "prototype C" + i + ";"));
        Compile("prototype Wide { " + children + " }");
        Assert.AreEqual(1000, P("Wide").GetNestedChildren().Count());
        for (int i = 0; i < 1000; i++) Edge("Wide", "Wide.C" + i);
    }

    [TestMethod]
    public void LazyNestedDeclarations_PreserveCoreAndAreIdempotent()
    {
        var compiler = Compile("prototype NestCore { prototype Existing; }");
        int id = P("NestCore.Existing").PrototypeID;
        string path = Path.Combine(Path.GetTempPath(), "nested-lazy-" + Guid.NewGuid().ToString("N") + ".pts");
        try
        {
            System.IO.File.WriteAllText(path, "prototype LazyNest { prototype Child { prototype Grand; } }");
            var statements = compiler.CompileAndAppendModule(path);
            Edge("LazyNest", "LazyNest.Child"); Edge("LazyNest.Child", "LazyNest.Child.Grand");
            var before = TemporaryPrototypes.GetAllTemporaryPrototypes().Where(x => x.NestedParent != null).Select(x => x.PrototypeName + "->" + x.NestedParent.PrototypeName).OrderBy(x => x).ToArray();
            new NativeInterpretter(compiler).InterpretStatements(statements);
            CollectionAssert.AreEqual(before, TemporaryPrototypes.GetAllTemporaryPrototypes().Where(x => x.NestedParent != null).Select(x => x.PrototypeName + "->" + x.NestedParent.PrototypeName).OrderBy(x => x).ToArray());
            Assert.AreEqual(0, compiler.CompileAndAppendModule(path).Count);
            Assert.AreEqual(id, P("NestCore.Existing").PrototypeID);
            Edge("NestCore", "NestCore.Existing");
        }
        finally { System.IO.File.Delete(path); }
    }

    [TestMethod]
    public void CanonicalReload_PreservesExplicitRelationships()
    {
        Compile("prototype ReloadNest { prototype Child; }");
        int child = P("ReloadNest.Child").PrototypeID;
        Initializer.ReloadCache();
        Edge("ReloadNest", "ReloadNest.Child");
        Assert.AreEqual(child, P("ReloadNest.Child").PrototypeID);
    }
    [TestMethod]
    public void NestedInheritance_DoesNotReplaceLexicalOwnership()
    {
        Compile("prototype NestBase; prototype NestOwner { prototype Child : NestBase; }");
        Edge("NestOwner", "NestOwner.Child");
        Assert.IsTrue(P("NestOwner.Child").GetAllParents().Contains(P("NestBase").PrototypeID));
    }

    [TestMethod]
    public void TwentyLevels_PreserveImmediateParents()
    {
        string source = string.Join(" ", Enumerable.Range(0, 20).Select(i => "prototype D" + i + " {")) + new string('}', 20);
        Compile(source);
        string name = "D0";
        for (int i = 1; i < 20; i++) { string child = name + ".D" + i; Edge(name, child); name = child; }
    }

    [TestMethod]
    public void ResetThenRecompile_DoesNotRetainOldChildren()
    {
        Compile("prototype ResetOwner { prototype Old; }");
        Initializer.ResetCache();
        Compile("prototype ResetOwner { prototype New; }");
        Edge("ResetOwner", "ResetOwner.New");
        Assert.AreEqual(1, P("ResetOwner").GetNestedChildren().Count());
    }

    [TestMethod]
    public void ExplicitRelationship_RemoveAndReparent()
    {
        Compile("prototype MoveA { prototype Child; } prototype MoveB;");
        var child = P("MoveA.Child");
        child.RemoveNestedParent();
        Assert.IsNull(child.NestedParent);
        Assert.AreEqual(0, P("MoveA").GetNestedChildren().Count());
        child.InsertNestedParent(P("MoveB").PrototypeID);
        Edge("MoveB", "MoveA.Child");
    }

    [TestMethod]
    public void ReopenedNestedDeclaration_PreservesExistingDiagnostic()
    {
        var error = Assert.ThrowsException<ProtoScript.Parsers.ProtoScriptCompilerException>(() =>
            Compile("prototype PartOwner { prototype First; } prototype PartOwner { prototype Second; }"));
        StringAssert.Contains(error.Message, "Second");
    }

    [TestMethod]
    public void DottedTopLevelName_IsNotLexicalDeclaration()
    {
        Compile("prototype DotOwner; prototype DotOwner.Child;");
        Assert.IsNull(P("DotOwner.Child").NestedParent);
    }

    [TestMethod]
    public void Clone_RetainsNestedParentIdentity()
    {
        Compile("prototype CloneOwner { prototype Child; }");
        Assert.AreEqual(P("CloneOwner").PrototypeID, P("CloneOwner.Child").Clone().NestedParent?.PrototypeID);
    }
    [TestMethod]
    public void EagerIncludesAndMissingLazyTarget_DoNotChangeOwnership()
    {
        string root = Path.Combine(Path.GetTempPath(), "nestinc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            System.IO.File.WriteAllText(Path.Combine(root, "child.pts"), "prototype IncludedOwner { prototype Child; }");
            System.IO.File.WriteAllText(Path.Combine(root, "Project.pts"), "include \"child.pts\"; include lazy \"missing.pts\";");
            var c = new Compiler(); c.Initialize(); c.CompileProject(Path.Combine(root, "Project.pts"));
            Edge("IncludedOwner", "IncludedOwner.Child");
            Assert.AreEqual(1, c.LazyIncludeDeclarations.Count);
        }
        finally { Directory.Delete(root, true); }
    }

    [TestMethod]
    public void RepeatedFreshCompiler_PreservesOwnership()
    {
        for (int i = 0; i < 3; i++)
        {
            Initializer.ResetCache();
            Compile("prototype RepeatOwner { prototype Child; }");
            Edge("RepeatOwner", "RepeatOwner.Child");
            Assert.AreEqual(1, P("RepeatOwner").GetNestedChildren().Count());
        }
    }

    [TestMethod]
    public void IndependentExecutionContexts_DoNotCrossLink()
    {
        Task task;
        using (ExecutionContext.SuppressFlow())
            task = Task.Run(() => { Compile("prototype IsolatedOwner { prototype Child; }"); Edge("IsolatedOwner", "IsolatedOwner.Child"); });
        task.GetAwaiter().GetResult();
        Initializer.ResetCache();
        Compile("prototype IsolatedOwner { prototype Different; }");
        Assert.AreEqual(1, P("IsolatedOwner").GetNestedChildren().Count());
        Edge("IsolatedOwner", "IsolatedOwner.Different");
    }
    [TestMethod]
    public void LazyNestedModule_IgnoresIncidentalCallerScope()
    {
        var c = Compile("prototype ScopeCore;");
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pts");
        try
        {
            System.IO.File.WriteAllText(path, "prototype ScopedLazy { prototype Child; }");
            c.Symbols.EnterScope(new ProtoScript.Interpretter.Symbols.Scope(ProtoScript.Interpretter.Symbols.Scope.ScopeTypes.Method));
            try { c.CompileAndAppendModule(path); } finally { c.Symbols.LeaveScope(); }
            Edge("ScopedLazy", "ScopedLazy.Child");
        }
        finally { System.IO.File.Delete(path); }
    }

    [TestMethod]
    public void ScalarWrappers_DoNotAcquireLexicalParents()
    {
        Compile("prototype ScalarOwner;");
        Assert.IsNull(NativeValuePrototype.GetOrCreateNativeValuePrototype(true).NestedParent);
        Assert.IsNull(NativeValuePrototype.GetOrCreateNativeValuePrototype(false).NestedParent);
        Assert.IsNull(NativeValuePrototype.GetOrCreateNativeValuePrototype(123).NestedParent);
        Assert.IsNull(NativeValuePrototype.GetOrCreateNativeValuePrototype(1.5).NestedParent);
    }

    [TestMethod]
    public void EagerFileOrder_DoesNotChangeIndependentLexicalTrees()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            System.IO.File.WriteAllText(Path.Combine(root, "a.pts"), "prototype OrderA { prototype Child; }");
            System.IO.File.WriteAllText(Path.Combine(root, "b.pts"), "prototype OrderB { prototype Child; }");
            foreach (string includes in new[] { "include \"a.pts\"; include \"b.pts\";", "include \"b.pts\"; include \"a.pts\";" })
            {
                Initializer.ResetCache();
                System.IO.File.WriteAllText(Path.Combine(root, "Project.pts"), includes);
                var c = new Compiler(); c.Initialize(); c.CompileProject(Path.Combine(root, "Project.pts"));
                Edge("OrderA", "OrderA.Child"); Edge("OrderB", "OrderB.Child");
            }
        }
        finally { Directory.Delete(root, true); }
    }
    [TestMethod]
    public void NestedMethod_ExecutesWithDirectConsumerRelationships()
    {
        var c = Compile("prototype ExecutableOwner { prototype Child { string ResultText = \"correct\"; function Execute() : string { return \"correct\"; } } }");
        Edge("ExecutableOwner", "ExecutableOwner.Child");
        var direct = P("ExecutableOwner").GetNestedChildren().Single();
        Assert.AreEqual("correct", new NativeInterpretter(c).RunMethodAsObject(direct, "Execute", new List<object>()));
    }

    [TestMethod]
    public void ConcurrentIndependentContexts_PreserveTheirOwnTrees()
    {
        Task[] tasks;
        using (ExecutionContext.SuppressFlow())
            tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
            {
                Compile("prototype ConcurrentOwner" + i + " { prototype Child; }");
                Edge("ConcurrentOwner" + i, "ConcurrentOwner" + i + ".Child");
            })).ToArray();
        Task.WaitAll(tasks);
    }
}