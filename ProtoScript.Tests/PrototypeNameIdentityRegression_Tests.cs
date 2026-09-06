using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class PrototypeNameIdentityRegression_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void RunMethod_BarePrototypeNameInDeclaredPrototypeMethod_ReturnsCurrentPrototypeName()
		{
			const string code = @"
prototype PrototypeNameDeclaredBase;

prototype PrototypeNameDeclaredChild : PrototypeNameDeclaredBase
{
	function Execute() : string
	{
		return PrototypeName;
	}
}";

			object? result = RunPrototypeMethod(code, "PrototypeNameDeclaredChild", "Execute");

			Assert.AreEqual("PrototypeNameDeclaredChild", result);
		}

		[TestMethod]
		public void RunMethod_BareAndExplicitPrototypeNameInInheritedPrototypeMethod_OnChild_ReturnSamePrototypeName()
		{
			const string code = @"
prototype PrototypeNameInheritedChildBase
{
	function Bare() : string
	{
		return PrototypeName;
	}

	function Explicit() : string
	{
		return this.PrototypeName;
	}
}

prototype PrototypeNameInheritedChild : PrototypeNameInheritedChildBase;";

			object? bare = RunPrototypeMethod(code, "PrototypeNameInheritedChild", "Bare");
			object? explicitReceiver = RunPrototypeMethod(code, "PrototypeNameInheritedChild", "Explicit");

			Assert.AreEqual("PrototypeNameInheritedChild", bare);
			Assert.AreEqual(explicitReceiver, bare);
		}

		[TestMethod]
		public void RunMethod_BareAndExplicitPrototypeNameOnNewRuntimeInstance_ReturnSamePrototypeName()
		{
			const string code = @"
prototype IdentityRegressionBase
{
	function Bare() : string
	{
		return PrototypeName;
	}

	function Explicit() : string
	{
		return this.PrototypeName;
	}
}

prototype IdentityRegressionChild : IdentityRegressionBase;

prototype IdentityRegressionRunner
{
	function Execute() : string
	{
		IdentityRegressionChild instance = new IdentityRegressionChild();
		return instance.Bare() + @""|"" + instance.Explicit();
	}
}";

			object? result = RunPrototypeMethod(code, "IdentityRegressionRunner", "Execute");

			Assert.IsInstanceOfType(result, typeof(string));
			string resultText = (string)result;
			string[] parts = resultText.Split('|');
			Assert.AreEqual(2, parts.Length);
			Assert.AreEqual(parts[1], parts[0]);
			StringAssert.StartsWith(parts[0], "IdentityRegressionChild#");
		}

		[TestMethod]
		public void Compile_QualifiedPrototypeNamePath_CompilesWithoutUsingImplicitReceiverFallback()
		{
			const string code = @"
prototype PrototypeNameQualifiedBase;
prototype PrototypeNameQualifiedChild : PrototypeNameQualifiedBase;

string qualifiedName = PrototypeNameQualifiedChild.PrototypeName;";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));

			Assert.AreEqual(string.Empty, string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
		}

		[TestMethod]
		public void RunMethod_GenericRunnerEquivalentComparesPrototypeNameToClrStringParameter()
		{
			const string code = @"
prototype PrototypeNameGenericCandidate;

prototype PrototypeNameGenericRunner
{
	function Execute(string expectedPrototypeName) : string
	{
		PrototypeNameGenericCandidate candidate = PrototypeNameGenericCandidate;
		if (candidate.PrototypeName == expectedPrototypeName)
		{
			return candidate.PrototypeName;
		}

		return @""none"";
	}
}";

			object? result = RunPrototypeMethod(
				code,
				"PrototypeNameGenericRunner",
				"Execute",
				new Dictionary<string, object> { ["expectedPrototypeName"] = "PrototypeNameGenericCandidate" });

			Assert.AreEqual("PrototypeNameGenericCandidate", result);
		}

		[TestMethod]
		public void RunMethod_LocalVariableNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
prototype PrototypeNameLocalShadowChild
{
	function Execute() : string
	{
		string PrototypeName = @""local-shadow"";
		return PrototypeName;
	}
}";

			object? result = RunPrototypeMethod(code, "PrototypeNameLocalShadowChild", "Execute");

			Assert.AreEqual("local-shadow", result);
		}

		[TestMethod]
		public void RunMethod_ParameterNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
prototype PrototypeNameParameterShadowChild
{
	function Execute(string PrototypeName) : string
	{
		return PrototypeName;
	}
}";

			object? result = RunPrototypeMethod(
				code,
				"PrototypeNameParameterShadowChild",
				"Execute",
				new Dictionary<string, object> { ["PrototypeName"] = "parameter-shadow" });

			Assert.AreEqual("parameter-shadow", result);
		}

		[TestMethod]
		public void RunMethod_FieldNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
prototype PrototypeNameFieldShadowChild
{
	String PrototypeName = @""field-shadow"";

	function Execute() : string
	{
		return PrototypeName;
	}
}";

			object? result = RunPrototypeMethod(code, "PrototypeNameFieldShadowChild", "Execute");

			Assert.AreEqual("field-shadow", result);
		}

		[TestMethod]
		public void RunMethod_GlobalFunctionNamedPrototypeName_ShieldsImplicitReceiverNativeMember()
		{
			const string code = @"
function PrototypeName() : string
{
	return @""global-function"";
}

prototype PrototypeNameGlobalShadowChild
{
	function Execute() : string
	{
		return PrototypeName();
	}
}";

			object? result = RunPrototypeMethod(code, "PrototypeNameGlobalShadowChild", "Execute");

			Assert.AreEqual("global-function", result);
		}

		[TestMethod]
		public void Compile_UnknownBareIdentifierInPrototypeMethod_EmitsCannotFindIdentifierDiagnostic()
		{
			const string code = @"
prototype PrototypeNameUnknownIdentifierChild
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

		[TestMethod]
		public void Compile_BarePrototypeNameOutsidePrototypeMethod_StaysUnknownIdentifier()
		{
			const string code = @"
function Execute() : string
{
	return PrototypeName;
}";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			compiler.Compile(Files.ParseFileContents(code));

			Assert.IsTrue(
				compiler.Diagnostics.Any(x => (x.Diagnostic?.Message ?? string.Empty).Contains("Cannot find identifier PrototypeName", StringComparison.Ordinal)),
				string.Join("; ", compiler.Diagnostics.Select(x => x.Diagnostic?.Message)));
		}

		private static object? RunPrototypeMethod(string code, string prototypeName, string methodName)
		{
			return RunPrototypeMethod(code, prototypeName, methodName, new Dictionary<string, object>());
		}

		private static object? RunPrototypeMethod(string code, string prototypeName, string methodName, Dictionary<string, object> parameters)
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
