using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ComparisonOperatorDiagnostics_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void Compile_NullComparedToInt_EmitsTypedOperandDiagnostic_NotNullReference()
		{
			const string code = @"
prototype CompareSkillAction
{
	function Execute() : bool
	{
		return null > 0;
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(Files.ParseFileContents(code));

			Assert.IsTrue(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty)
						.Contains("requires typed operands", StringComparison.OrdinalIgnoreCase)),
				"Expected typed operand diagnostic, got: " +
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
			Assert.IsFalse(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty)
						.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)),
				"Unexpected NullReferenceException diagnostic: " +
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
		}

		[TestMethod]
		public void Compile_ComparisonAndLogicalAnd_UsesExpectedPrecedence_NoBooleanRightOperandComparisonDiagnostic()
		{
			const string code = @"
prototype CompareSkillAction
{
	function Execute() : bool
	{
		int outputIndex = 0;
		string output = ""abcdefghij"";
		return outputIndex >= 0 && outputIndex + 7 < output.Length;
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();

			compiler.Compile(Files.ParseFileContents(code));

			Assert.IsFalse(
				compiler.Diagnostics.Any(x =>
					(x.Diagnostic?.Message ?? string.Empty)
						.Contains("Only numeric comparisons supported", StringComparison.OrdinalIgnoreCase)
					&& (x.Diagnostic?.Message ?? string.Empty)
						.Contains("right operand type is Boolean", StringComparison.OrdinalIgnoreCase)),
				"Expression should parse as '(outputIndex >= 0) && (...)' and avoid Boolean-as-right-comparison diagnostics.");
		}

		[TestMethod]
		public void RunMethod_DoubleComparisonThreshold_SucceedsWithoutNullExpression()
		{
			const string code = @"
prototype CompareSkillAction
{
	function Execute() : string
	{
		double topScore = 0.7;
		if (topScore >= 0.5)
		{
			return ""keep"";
		}

		return ""reject"";
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(Files.ParseFileContents(code));

			string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
			Assert.AreEqual(string.Empty, diagnostics);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			PrototypeTypeInfo protoInfo = (PrototypeTypeInfo)compiler.Symbols.GetGlobalScope().GetSymbol("CompareSkillAction");

			object result = interpretter.RunMethodAsObject(protoInfo.Prototype, "Execute", new Dictionary<string, object>());

			Assert.AreEqual("keep", result);
		}

		[TestMethod]
		public void RunMethod_StringAndDoubleComparisonCondition_SucceedsWithoutNullExpression()
		{
			const string code = @"
prototype CompareSkillAction
{
	function Execute() : string
	{
		string topPrototype = ""Widget"";
		double topScore = 0.7;
		if (topPrototype != """" && topScore >= 0.5)
		{
			return ""keep"";
		}

		return ""reject"";
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(Files.ParseFileContents(code));

			string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
			Assert.AreEqual(string.Empty, diagnostics);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			PrototypeTypeInfo protoInfo = (PrototypeTypeInfo)compiler.Symbols.GetGlobalScope().GetSymbol("CompareSkillAction");

			object result = interpretter.RunMethodAsObject(protoInfo.Prototype, "Execute", new Dictionary<string, object>());

			Assert.AreEqual("keep", result);
		}
	}
}
