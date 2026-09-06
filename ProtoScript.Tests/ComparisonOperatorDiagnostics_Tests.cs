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
		[TestMethod]
		public void RunMethod_BarePrototypeNameComparisonInDeclaredPrototypeMethod_ReturnsCurrentPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase;

prototype IdentityRegressionChild : IdentityRegressionBase
{
	function Execute() : string
	{
		if (PrototypeName == @""IdentityRegressionChild"")
		{
			return PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparisonInDeclaredPrototypeMethod_ReturnsCurrentPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase;

prototype IdentityRegressionChild : IdentityRegressionBase
{
	function Execute() : string
	{
		if (this.PrototypeName == @""IdentityRegressionChild"")
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparedToVerbatimStringInDeclaredMethod_ReturnsCurrentPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase;

prototype IdentityRegressionChild : IdentityRegressionBase
{
	function Execute() : string
	{
		if (this.PrototypeName == @""IdentityRegressionChild"")
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_BarePrototypeNameComparisonInInheritedPrototypeMethod_ReturnsConcreteChildPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		if (PrototypeName == @""IdentityRegressionChild"")
		{
			return PrototypeName;
		}

		return ""wrong"";
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparisonInInheritedPrototypeMethod_ReturnsConcreteChildPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		if (this.PrototypeName == @""IdentityRegressionChild"")
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_BarePrototypeNameComparisonInInheritedPrototypeMethod_OnInstance_ReturnsInstancePrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		if (PrototypeName == @""IdentityRegressionInstance"")
		{
			return PrototypeName;
		}

		return ""wrong"";
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;
prototype IdentityRegressionInstance : IdentityRegressionChild;";

			object result = RunPrototypeMethod(code, "IdentityRegressionInstance", "Execute");

			Assert.AreEqual("IdentityRegressionInstance", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparisonInInheritedPrototypeMethod_OnInstance_ReturnsInstancePrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		if (this.PrototypeName == @""IdentityRegressionInstance"")
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;
prototype IdentityRegressionInstance : IdentityRegressionChild;";

			object result = RunPrototypeMethod(code, "IdentityRegressionInstance", "Execute");

			Assert.AreEqual("IdentityRegressionInstance", result);
		}

		[TestMethod]
		public void RunMethod_QualifiedChildPrototypeNameComparison_ReturnsQualifiedPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase;
prototype IdentityRegressionChild : IdentityRegressionBase;

prototype IdentityRegressionRunner
{
	function Execute() : string
	{
		if (IdentityRegressionChild.PrototypeName == @""IdentityRegressionChild"")
		{
			return IdentityRegressionChild.PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionRunner", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		[Ignore("Documents the separate normal string-literal wrapper equality behavior; not part of the PrototypeName lookup fix.")]
		public void RunMethod_GenericRunnerEquivalentSelectsPrototypeByPrototypeNameComparison()
		{
			const string code = @"
prototype IdentityRegressionCandidate
{
	String Name = ""expected"";
}

prototype IdentityRegressionRunner
{
	function Execute() : string
	{
		IdentityRegressionCandidate candidate = IdentityRegressionCandidate;
		string selected = ""none"";

		if (candidate.PrototypeName == ""IdentityRegressionCandidate"" && candidate.Name == ""expected"")
		{
			selected = candidate.PrototypeName;
		}

		return selected;
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionRunner", "Execute");

			Assert.AreEqual("IdentityRegressionCandidate", result);
		}

		[TestMethod]
		public void RunMethod_DirectReturnThisPrototypeNameInInheritedPrototypeMethod_OnChild_ReturnsConcreteChildPrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		return this.PrototypeName;
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_DirectReturnThisPrototypeNameInInheritedPrototypeMethod_OnInstance_ReturnsInstancePrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Execute() : string
	{
		return this.PrototypeName;
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;
prototype IdentityRegressionInstance : IdentityRegressionChild;";

			object result = RunPrototypeMethod(code, "IdentityRegressionInstance", "Execute");

			Assert.AreEqual("IdentityRegressionInstance", result);
		}

		[TestMethod]
		public void RunMethod_GenericRunnerEquivalentSelectsPrototypeByPrototypeNameComparedToClrStringParameter()
		{
			const string code = @"
prototype IdentityRegressionCandidate;

prototype IdentityRegressionRunner
{
	function Execute(string expectedPrototypeName) : string
	{
		IdentityRegressionCandidate candidate = IdentityRegressionCandidate;
		string selected = ""none"";

		if (candidate.PrototypeName == expectedPrototypeName)
		{
			selected = candidate.PrototypeName;
		}

		return selected;
	}
}";

			object result = RunPrototypeMethod(
				code,
				"IdentityRegressionRunner",
				"Execute",
				new Dictionary<string, object> { ["expectedPrototypeName"] = "IdentityRegressionCandidate" });

			Assert.AreEqual("IdentityRegressionCandidate", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparedToNull_DoesNotMatchNullAndReturnsIdentity()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute() : string
	{
		if (this.PrototypeName == null)
		{
			return ""null"";
		}

		return this.PrototypeName;
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparedToClrEmptyStringParameter_BranchesAsNotEmpty()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute(string emptyValue) : string
	{
		if (this.PrototypeName == emptyValue)
		{
			return ""empty"";
		}

		if (this.PrototypeName != emptyValue)
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(
				code,
				"IdentityRegressionChild",
				"Execute",
				new Dictionary<string, object> { ["emptyValue"] = string.Empty });

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_ThisPrototypeNameComparedToNormalEmptyStringLiteral_BranchesAsNotEmpty()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute() : string
	{
		if (this.PrototypeName == """")
		{
			return ""empty"";
		}

		if (this.PrototypeName != """")
		{
			return this.PrototypeName;
		}

		return ""wrong"";
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("IdentityRegressionChild", result);
		}

		[TestMethod]
		public void RunMethod_LocalVariableNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute() : string
	{
		string PrototypeName = @""shadow"";
		return PrototypeName;
	}
}";

			object result = RunPrototypeMethod(code, "IdentityRegressionChild", "Execute");

			Assert.AreEqual("shadow", result);
		}

		[TestMethod]
		public void RunMethod_ParameterNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute(string PrototypeName) : string
	{
		return PrototypeName;
	}
}";

			object result = RunPrototypeMethod(
				code,
				"IdentityRegressionChild",
				"Execute",
				new Dictionary<string, object> { ["PrototypeName"] = "parameter-shadow" });

			Assert.AreEqual("parameter-shadow", result);
		}

		[TestMethod]
		public void Compile_UnknownBareIdentifierInPrototypeMethod_EmitsCannotFindIdentifierDiagnostic()
		{
			const string code = @"
prototype IdentityRegressionChild
{
	function Execute() : string
	{
		return MissingIdentitySymbol;
	}
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));

			Assert.IsTrue(
				compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("Cannot find identifier MissingIdentitySymbol", StringComparison.Ordinal)),
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
		}

		private static object RunPrototypeMethod(string code, string prototypeName, string methodName)
		{
			return RunPrototypeMethod(code, prototypeName, methodName, new Dictionary<string, object>());
		}

		private static object RunPrototypeMethod(string code, string prototypeName, string methodName, Dictionary<string, object> parameters)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(Files.ParseFileContents(code));

			string diagnostics = string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message));
			Assert.AreEqual(string.Empty, diagnostics);

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			PrototypeTypeInfo protoInfo = (PrototypeTypeInfo)compiler.Symbols.GetGlobalScope().GetSymbol(prototypeName);

			return interpretter.RunMethodAsObject(protoInfo.Prototype, methodName, parameters);
		}
	}
}
