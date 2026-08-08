using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class UnaryNotOperatorRegression_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileAndRun_DoubleMethodNegation_WithLogicalAnd_ReturnsExpected()
		{
			const string code = @"
function IsLabel(string text) : bool
{
	return text.StartsWith(""[label:"");
}

function IsTimelineLabel(string text) : bool
{
	return text.StartsWith(""[timeline-label:"");
}

function main() : bool
{
	string trimmedInstruction = ""normal text"";
	if (!IsLabel(trimmedInstruction)
		&& !IsTimelineLabel(trimmedInstruction))
	{
		return true;
	}

	return false;
}
";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());

			Assert.AreEqual(true, result);
		}

		[TestMethod]
		public void CompileAndRun_DoubleMemberCallNegation_WithLogicalAnd_ReturnsExpected()
		{
			const string code = @"
function main() : bool
{
	string trimmedInstruction = ""[timeline-label:abc"";
	if (!trimmedInstruction.StartsWith(""[label:"")
		&& !trimmedInstruction.StartsWith(""[timeline-label:""))
	{
		return true;
	}

	return false;
}
";

			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.File file = Files.ParseFileContents(code);
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);

			Assert.AreEqual(
				0,
				compiler.Diagnostics.Count,
				string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));

			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			interpretter.Evaluate(compiled);
			object? result = interpretter.RunMethodAsObject(null, "main", new List<object>());

			Assert.AreEqual(false, result);
		}

		[TestMethod]
		public void SimpleGenerator_PrefixUnaryExpressions_PreserveTheirOperands()
		{
			Assert.AreEqual(
				"!value.Contains(\"--- Turn Digest ---\")",
				SimpleGenerator.Generate(Expressions.Parse("!value.Contains(\"--- Turn Digest ---\")")));
			Assert.AreEqual("-1", SimpleGenerator.Generate(Expressions.Parse("-1")));
			Assert.AreEqual("~flags", SimpleGenerator.Generate(Expressions.Parse("~flags")));
		}

		[TestMethod]
		public void SimpleGenerator_PostfixUnaryExpression_PreservesOperandAndPosition()
		{
			ProtoScript.Expression parsed = Expressions.Parse("count++");
			string generated = SimpleGenerator.Generate(parsed);

			Assert.AreEqual("count++", generated);
			Assert.AreEqual("count++", SimpleGenerator.Generate(Expressions.Parse(generated)));
		}

		[TestMethod]
		public void UnaryOperator_Clone_PreservesOperandAndPostfixPosition()
		{
			var original = (ProtoScript.UnaryOperator)Expressions.Parse("count++").Terms.Single();
			var clone = (ProtoScript.UnaryOperator)original.Clone();

			Assert.AreNotSame(original, clone);
			Assert.AreNotSame(original.Right, clone.Right);
			Assert.IsTrue(clone.IsPostfix);
			Assert.AreEqual("count++", SimpleGenerator.Generate(clone));
		}

		[TestMethod]
		public void SimpleGenerator_MissingUnaryOperand_FailsInsteadOfWritingCorruptSource()
		{
			var malformed = new ProtoScript.UnaryOperator("!");

			ProtoScript.Parsers.GenerateFailedException error = Assert.ThrowsException<ProtoScript.Parsers.GenerateFailedException>(
				() => SimpleGenerator.Generate(malformed));

			StringAssert.Contains(error.Message, "missing its operand");
		}

		[TestMethod]
		public void SimpleGenerator_PrototypeMethodRoundTrip_PreservesUnaryExpressionsAndCompiles()
		{
			const string code = @"
prototype UnaryRoundTrip
{
	function Execute(string value) : int
	{
		if (!value.Contains(""--- Turn Digest ---""))
		{
			return -1;
		}

		return 0;
	}
}";

		ProtoScript.File parsed = Files.ParseFileContents(code);
		string generated = SimpleGenerator.Generate(parsed);

		StringAssert.Contains(generated, "!value.Contains(\"--- Turn Digest ---\")");
		StringAssert.Contains(generated, "return -1;");

		Compiler compiler = new Compiler();
		compiler.Initialize();
		compiler.Compile(Files.ParseFileContents(generated));

		Assert.AreEqual(
			0,
			compiler.Diagnostics.Count,
			string.Join("\n", compiler.Diagnostics.Select(d => d.Diagnostic?.Message ?? "(null)")));
	}

	}
}
