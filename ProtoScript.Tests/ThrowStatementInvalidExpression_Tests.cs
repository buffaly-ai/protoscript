using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests;

[TestClass]
public sealed class ThrowStatementInvalidExpression_Tests
{
	[TestInitialize]
	public void Init()
	{
		Initializer.Initialize();
	}

	[TestMethod]
	public void ThrowStringInsideTerminalBranch_CurrentlySurfacesAsMethodDidNotReturnValue()
	{
		string code = @"
function main() : string
{
	if (true)
		throw ""not-an-exception"";
	else
		return ""ok"";
}";

		ProtoScript.File file = Files.ParseFileContents(code);
		Compiler compiler = new();
		compiler.Initialize();
		ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
		Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.ToString())));

		NativeInterpretter interpretter = new(compiler);
		interpretter.Evaluate(compiled);

		RuntimeException ex = Assert.ThrowsException<RuntimeException>(() => interpretter.RunMethodAsObject(null, "main", new List<object>()));
		Assert.AreEqual("Method did not return a value", ex.Message);
	}
}
