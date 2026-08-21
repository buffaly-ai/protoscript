using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeInitializerDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_InitializerUnresolvedRightHandIdentifier_ReportsInitializerRhsDiagnostic_NotNullExpression()
		{
			const string code = @"
prototype MemoryBase {}

prototype BrokenMemory : MemoryBase
{
	MemoryBase SiblingMemory = new MemoryBase();
	SiblingMemory = MissingSiblingMemory;
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(Files.ParseFileContents(code));

			string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
			Assert.IsTrue(
				diagnostics.Contains("Cannot compile initializer right side for property SiblingMemory", StringComparison.OrdinalIgnoreCase)
				&& diagnostics.Contains("Cannot find identifier MissingSiblingMemory", StringComparison.OrdinalIgnoreCase),
				"Expected initializer RHS unresolved identifier diagnostic, got: " + diagnostics);
			Assert.IsFalse(
				diagnostics.Contains("Expression is null", StringComparison.OrdinalIgnoreCase),
				"Should not surface null-expression failure: " + diagnostics);
			Assert.IsFalse(
				diagnostics.Contains("ArgumentNullException", StringComparison.OrdinalIgnoreCase),
				"Should not surface ArgumentNullException: " + diagnostics);
		}

		[TestMethod]
		public void Compile_InitializerUppercaseBooleanLiteral_ReportsBooleanLiteralCaseDiagnostic_NotNullExpression()
		{
			const string code = @"
prototype PromptAction
{
	Bool IsPromptAction = true;
}

prototype ToCurateLearnedOnlineActionsSkill : PromptAction
{
	IsPromptAction = True;
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(Files.ParseFileContents(code));

			string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
			Assert.IsTrue(
				diagnostics.Contains("Boolean literal 'True' must be lowercase", StringComparison.OrdinalIgnoreCase),
				"Expected boolean literal casing diagnostic, got: " + diagnostics);
			Assert.IsFalse(
				diagnostics.Contains("Expression is null", StringComparison.OrdinalIgnoreCase),
				"Should not surface null-expression failure: " + diagnostics);
			Assert.IsFalse(
				diagnostics.Contains("ArgumentNullException", StringComparison.OrdinalIgnoreCase),
				"Should not surface ArgumentNullException: " + diagnostics);
		}

		[TestMethod]
		public void CompileProject_InitializerForwardReference_StillResolvesLaterPrototype()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScriptInitializerForwardReference_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Project.pts"),
					@"include ""Memory.pts"";");
				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Memory.pts"),
					@"
prototype MemoryBase {}

prototype ForwardReferenceMemory : MemoryBase
{
	MemoryBase SiblingMemory = new MemoryBase();
	SiblingMemory = LaterMemory;
}

prototype LaterMemory : MemoryBase {}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));

				string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
				Assert.AreEqual(string.Empty, diagnostics);
			}
			finally
			{
				Directory.Delete(tempDir, true);
			}
		}
	}
}
