using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NullConditionalOperator_Tests
	{
		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		// Purpose: Verify null-conditional member access returns null when the receiver is null.
		[TestMethod]
		public void NullConditionalProperty_ReturnsNull()
		{
			string code = @"
prototype Person
{
String Name = ""Homer"";
}

function main() : Prototype
{
Person p = null;
return p?.Name;
}
";
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interp = new NativeInterpretter(compiler);
			interp.Evaluate(compiled);
			object? res = interp.RunMethodAsObject(null, "main", new List<object>());
			Assert.IsNull(res);
		}

		// Purpose: Verify null-conditional method invocation returns null when the receiver is null.
		[TestMethod]
		public void NullConditionalMethod_ReturnsNull()
		{
			string code = @"
function main() : string
{
String s = null;
return s?.GetStringValue();
}
";
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Compiler compiler = new Compiler();
			compiler.Initialize();
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			NativeInterpretter interp = new NativeInterpretter(compiler);
			interp.Evaluate(compiled);
			object? res = interp.RunMethodAsObject(null, "main", new List<object>());
			Assert.IsNull(res);
		}

		// Purpose: Guard member invocation after an indexed expression, the shape that previously
		// overran ParseDotOperators' independent operator and term lists.
		[TestMethod]
		public void IndexedValueMethod_PreservesIndexAsMemberTarget()
		{
			BinaryOperator memberAccess = ParseSingleOperator("values[0].Trim()");
			Assert.AreEqual(".", memberAccess.Value);
			Assert.IsInstanceOfType<IndexOperator>(memberAccess.Left);
			Assert.IsInstanceOfType<MethodEvaluation>(memberAccess.Right);
		}

		// Purpose: Ensure null-conditional member access follows an indexed expression through
		// the same postfix-member parser path without losing the index expression.
		[TestMethod]
		public void IndexedValueNullConditionalMethod_PreservesIndexAsMemberTarget()
		{
			BinaryOperator memberAccess = ParseSingleOperator("values[0]?.Trim()");
			Assert.AreEqual("?.", memberAccess.Value);
			Assert.IsInstanceOfType<IndexOperator>(memberAccess.Left);
			Assert.IsInstanceOfType<MethodEvaluation>(memberAccess.Right);
		}

		// Purpose: Reproduce the original inline-prototype parser failure in a nested while body.
		[TestMethod]
		public void InlinePrototypeWithIndexedValueMethodInWhileBody_Parses()
		{
			string code = @"
prototype ToGitAddMultiplePathspecs
{
	function Execute(string pathspecs) : string
	{
		string[] paths = pathspecs.Split(';');
		int i = 0;
		while (i < paths.Length)
		{
			string trimmed = paths[i].Trim();
			i = i + 1;
		}
		return ""done"";
	}
}
";
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents(code);
			Assert.IsNotNull(file);
		}

		// Purpose: A less-than operator after a member identifier is not necessarily a generic
		// method invocation. Failed lookahead must preserve the identifier and comparison tokens.
		[TestMethod]
		public void MemberFollowedByLessThan_FallsBackToIdentifier()
		{
			Expression expression = ProtoScript.Parsers.Expressions.Parse("value.Member < limit");
			Assert.AreEqual(1, expression.Terms.Count);
			BinaryOperator comparison = (BinaryOperator)expression.Terms[0];
			Assert.AreEqual("<", comparison.Value);
			BinaryOperator memberAccess = (BinaryOperator)comparison.Left;
			Assert.AreEqual(".", memberAccess.Value);
			Assert.IsInstanceOfType<Identifier>(memberAccess.Right);
		}

		private static BinaryOperator ParseSingleOperator(string code)
		{
			Expression expression = ProtoScript.Parsers.Expressions.Parse(code);
			Assert.AreEqual(1, expression.Terms.Count);
			return (BinaryOperator)expression.Terms[0];
		}
	}
}
