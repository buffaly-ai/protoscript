using ProtoScript.Interpretter;
using ProtoScript.Interpretter.Compiling;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeCompilerDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void DefinePrototypes_MissingResolvedPrototype_WrapsWithPrototypeContext()
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();

			ProtoScript.File file = Files.ParseFileContents("prototype Missing;");

			ProtoScriptCompilerException ex = Assert.ThrowsException<ProtoScriptCompilerException>(() =>
			{
				PrototypeCompiler.DefinePrototypes(file, compiler);
			});

			Assert.IsTrue(
				ex.Explanation.Contains("Failed to define prototype 'Missing'", StringComparison.Ordinal),
				"Expected prototype-specific context in explanation, got: " + ex.Explanation);
		}

		[TestMethod]
		public void Compile_SessionManagementAnnotationSnippet_DoesNotThrowDefinePrototypesNullReference()
		{
			const string code = @"
prototype SessionManagementSkillAction : OpsAction
{
	Description = @""Base action type for Session Management skill tools."";
}

[SemanticEntity(""session management skill"")]
prototype SessionManagementSkill : SkillEntity, CoreEntity
{
	Description = ""Session management and maintenance actions for agent session directories and metadata."";
	ActionRoot = SessionManagementSkillAction;
}

[SemanticProgram.InfinitivePhrase(""to remove guid named buffaly sessions"")]
[SemanticProgram.InfinitivePhrase(""to delete guid named opsagent sessions"")]
prototype ToRemoveGuidNamedBuffalySessions : SessionManagementSkillAction
{
	function Execute(bool dryRun = true) : string
	{
		return ""ok"";
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			try
			{
				compiler.Compile(Files.ParseFileContents(code));
			}
			catch (ProtoScriptCompilerException ex)
			{
				Assert.IsFalse(
					ex.Explanation.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase),
					"Unexpected NullReferenceException surfaced from DefinePrototypes: " + ex.Explanation);
				throw;
			}
		}

		[TestMethod]
		public void CompileFunctionAnnotations_MissingFunctionSymbol_ReturnsEmptyStatementsWithDiagnostic()
		{
			// Purpose: Cold lazy compilation must preserve a missing-function diagnostic without returning a null collection to stage aggregation.
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(@"
[SemanticProgram.InfinitivePhrase(""to run a missing function"")]
function MissingFunction() : string
{
	return ""ok"";
}");
			FunctionDefinition function = file.Statements.OfType<FunctionDefinition>().Single();

			List<ProtoScript.Interpretter.Compiled.Statement> statements = compiler.CompileFunctionAnnotations(function);

			Assert.AreEqual(0, statements.Count);
			Assert.IsTrue(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty).Contains("Could not find method: MissingFunction", StringComparison.Ordinal)),
				"Expected the missing function symbol to remain an explicit compiler diagnostic.");
		}

		[TestMethod]
		public void CompileFunctionAnnotations_NonGlobalActiveScope_ResolvesDeclaredGlobalFunction()
		{
			// Purpose: Cold lazy annotation compilation must resolve file-level functions from their authoritative global declaration scope.
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(@"
[SemanticProgram.InfinitivePhrase(""to run a globally declared function"")]
function GlobalFunction() : string
{
	return ""ok"";
}");
			FunctionDefinition function = file.Statements.OfType<FunctionDefinition>().Single();
			compiler.DeclareFunction(function);
			compiler.Symbols.EnterScope(new ProtoScript.Interpretter.Symbols.Scope(ProtoScript.Interpretter.Symbols.Scope.ScopeTypes.Method));

			try
			{
				compiler.CompileFunctionAnnotations(function);

				Assert.IsFalse(
					compiler.Diagnostics.Any(x =>
						(x.Diagnostic?.Message ?? string.Empty).Contains("Could not find method: GlobalFunction", StringComparison.Ordinal)),
					"Expected annotation compilation to resolve the function from global scope.");
			}
			finally
			{
				compiler.Symbols.LeaveScope();
			}
		}

		[TestMethod]
		public void Compile_PrototypeInheritsFromString_ShowsValueTypeGuidance()
		{
			const string code = @"
prototype ResultType;
prototype HtmlResult : ResultType, String
{
}

function TestHtml() : HtmlResult
{
	return ""Buffaly"";
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));

			string combinedDiagnostics = string.Join(
				"\n",
				compiler.Diagnostics.Select(x => x.Diagnostic.Message));

			Assert.IsTrue(
				combinedDiagnostics.Contains("Type Of is not found: String", StringComparison.Ordinal),
				"Expected missing type-of diagnostic for String. Actual: " + combinedDiagnostics);
			Assert.IsTrue(
				combinedDiagnostics.Contains("Prototype inheritance only supports prototype base types", StringComparison.Ordinal),
				"Expected inheritance guidance for value/native types. Actual: " + combinedDiagnostics);
			Assert.IsTrue(
				combinedDiagnostics.Contains("Use a field/property for value types instead", StringComparison.Ordinal),
				"Expected actionable field/property guidance. Actual: " + combinedDiagnostics);
		}

	}
}
