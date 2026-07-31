using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class LazyModuleCompilationTests
	{
		[TestMethod]
		public void CompileAndAppendModule_PreservesCoreAndReturnsOnlyModuleStatements()
		{
			string root = CreateTestRoot();
			try
			{
				string coreName = "LazyCore_" + Guid.NewGuid().ToString("N");
				string moduleName = "LazyModule_" + Guid.NewGuid().ToString("N");
				string projectPath = Write(root, "Project.pts", $"prototype {coreName} {{ function Execute() : string {{ return \"core\"; }} }}");
				string modulePath = Write(root, "Skill/index.pts", $"prototype {moduleName} {{ function Execute() : string {{ return \"module\"; }} }}");

				Compiler compiler = new Compiler();
				compiler.Initialize();
				List<ProtoScript.Interpretter.Compiled.Statement> coreStatements = compiler.CompileProject(projectPath);
				NativeInterpretter interpreter = new NativeInterpretter(compiler);
				interpreter.InterpretStatements(coreStatements);
				int originalFileCount = compiler.Files.Count;

				List<ProtoScript.Interpretter.Compiled.Statement> moduleStatements = compiler.CompileAndAppendModule(modulePath);
				interpreter.InterpretStatements(moduleStatements);

				Assert.AreEqual(originalFileCount + 1, compiler.Files.Count);
				Assert.IsTrue(moduleStatements.Count > 0);
				Assert.AreEqual("core", Run(interpreter, compiler, coreName));
				Assert.AreEqual("module", Run(interpreter, compiler, moduleName));
			}
			finally { Directory.Delete(root, true); }
		}

		[TestMethod]
		public void CompileAndAppendModule_LoadsPrivateEagerClosureAndSkipsNestedLazyInclude()
		{
			string root = CreateTestRoot();
			try
			{
				string projectPath = Write(root, "Project.pts", "prototype AppendClosureCore;");
				string modulePath = Write(root, "Skill/index.pts", "include \"Private.pts\"; include lazy \"Nested/index.pts\"; prototype AppendClosureModule;");
				string privatePath = Write(root, "Skill/Private.pts", "prototype AppendClosurePrivate;");

				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.CompileProject(projectPath);
				compiler.CompileAndAppendModule(modulePath);

				Assert.IsTrue(compiler.Files.Any(x => string.Equals(x.Info.FullName, Path.GetFullPath(modulePath), StringComparison.OrdinalIgnoreCase)));
				Assert.IsTrue(compiler.Files.Any(x => string.Equals(x.Info.FullName, Path.GetFullPath(privatePath), StringComparison.OrdinalIgnoreCase)));
				Assert.AreEqual(1, compiler.LazyIncludeDeclarations.Count);
				Assert.AreEqual(Path.GetFullPath(Path.Combine(root, "Skill/Nested/index.pts")), compiler.LazyIncludeDeclarations[0].ModuleFilePath);
			}
			finally { Directory.Delete(root, true); }
		}

		[TestMethod]
		public void CompileAndAppendModule_WhenAlreadyTracked_ReturnsEmptyDelta()
		{
			string root = CreateTestRoot();
			try
			{
				string projectPath = Write(root, "Project.pts", "prototype AppendIdempotentCore;");
				string modulePath = Write(root, "Skill/index.pts", "prototype AppendIdempotentModule;");
				Compiler compiler = new Compiler();
				compiler.Initialize();
				compiler.CompileProject(projectPath);
				compiler.CompileAndAppendModule(modulePath);
				int count = compiler.Files.Count;

				List<ProtoScript.Interpretter.Compiled.Statement> second = compiler.CompileAndAppendModule(modulePath);

				Assert.AreEqual(0, second.Count);
				Assert.AreEqual(count, compiler.Files.Count);
			}
			finally { Directory.Delete(root, true); }
		}

		private static object Run(NativeInterpretter interpreter, Compiler compiler, string prototypeName)
		{
			Ontology.Prototype prototype = Ontology.Prototypes.GetPrototypeByPrototypeName(prototypeName);
			return interpreter.RunMethodAsObject(prototype, "Execute", new List<object>());
		}

		private static string CreateTestRoot()
		{
			string root = Path.Combine(Path.GetTempPath(), "ProtoScriptLazyModuleTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			return root;
		}

		private static string Write(string root, string relativePath, string content)
		{
			string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			System.IO.File.WriteAllText(path, content);
			return path;
		}
	}
}
