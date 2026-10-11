using ProtoScript.Interpretter;
using ProtoScript.Parsers;

namespace ProtoScript.Tests;

[TestClass]
[DoNotParallelize]
public sealed class IncludeDeduplicationTests
{
	// Deleting an already parsed target at a lazy callback proves later aliases never read it again.
	[DataTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void DuplicateLiteral_IsNotReadAgain(bool parallel)
	{
		WithFiles(root =>
		{
			string leaf = Path.Combine(root, "Leaf.pts");
			System.IO.File.WriteAllText(leaf, "prototype Leaf;");
			var project = Project(root, "include \"Leaf.pts\"; include lazy \"Signal.pts\"; include \"./Leaf.pts\";");
			var files = Compiler.GetAllIncludedFiles(project, parallel, lazyIncludeHandler: (_, _, _) => System.IO.File.Delete(leaf));
			Assert.AreEqual(1, files.Count(f => f.Info.Name == "Leaf.pts"));
		});
	}

	// Overlapping wildcard/literal imports preserve depth-first first-seen order and parse a diamond once.
	[TestMethod]
	public void DiamondWildcard_PreservesOrderAndSkipsDeletedDuplicate()
	{
		WithFiles(root =>
		{
			Directory.CreateDirectory(Path.Combine(root, "Modules"));
			string leaf = Path.Combine(root, "Modules", "Leaf.pts");
			System.IO.File.WriteAllText(leaf, "prototype Leaf;");
			System.IO.File.WriteAllText(Path.Combine(root, "A.pts"), "include \"Modules/Leaf.pts\"; prototype A;");
			System.IO.File.WriteAllText(Path.Combine(root, "B.pts"), "include \"Modules/*.pts\"; include lazy \"Signal.pts\"; include \"Modules/Leaf.pts\"; prototype B;");
			var files = Compiler.GetAllIncludedFiles(Project(root, "include \"A.pts\"; include \"B.pts\";"), false,
				lazyIncludeHandler: (_, _, _) => System.IO.File.Delete(leaf));
			CollectionAssert.AreEqual(new[] { "A.pts", "Leaf.pts", "B.pts" }, files.Select(f => f.Info.Name).ToArray());
		});
	}

	// A cycle terminates without changing the serial traversal's established root-in-result behavior.
	[TestMethod]
	public void Cycle_Terminates()
	{
		WithFiles(root =>
		{
			System.IO.File.WriteAllText(Path.Combine(root, "A.pts"), "include \"Project.pts\"; prototype A;");
			var project = Project(root, "include \"A.pts\";");
			var files = Compiler.GetAllIncludedFiles(project, false);
			CollectionAssert.AreEqual(new[] { "A.pts", "Project.pts" }, files.Select(f => f.Info.Name).ToArray());
		});
	}

	// Best-effort failed include sites retain callbacks and placeholders instead of being cached as successes.
	[DataTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void MissingIncludes_PreservePerSiteErrors(bool parallel)
	{
		WithFiles(root =>
		{
			int errors = 0;
			var files = Compiler.GetAllIncludedFiles(Project(root, "include \"Missing.pts\"; include \"Missing.pts\";"), parallel, true,
				(_, _) => Interlocked.Increment(ref errors));
			Assert.AreEqual(2, errors);
			Assert.AreEqual(2, files.Count(f => f.Info.Name == "Missing.pts"));
		});
	}

	// Strict mode still reports the original include-site missing-file diagnostic.
	[TestMethod]
	public void MissingInclude_StrictStillThrows()
	{
		WithFiles(root => Assert.ThrowsException<ProtoScriptCompilerException>(() =>
			Compiler.GetAllIncludedFiles(Project(root, "include \"Missing.pts\";"), false)));
	}

	// Precompiled include preference is preserved, but duplicate JSON inputs must not be reread.
	[TestMethod]
	public void PrecompiledDuplicate_IsNotReadAgain()
	{
		bool previous = Settings.AllowPrecompiled;
		try
		{
			Settings.AllowPrecompiled = true;
			WithFiles(root =>
			{
				string path = Path.Combine(root, "Leaf.pts.json");
				System.IO.File.WriteAllText(path, "{}");
				var files = Compiler.GetAllIncludedFiles(Project(root, "include \"Leaf.pts\"; include lazy \"Signal.pts\"; include \"./Leaf.pts\";"), false,
					lazyIncludeHandler: (_, _, _) => System.IO.File.Delete(path));
				Assert.AreEqual(1, files.Count);
				Assert.IsTrue(files[0].IsPrecompiled);
			});
		}
		finally { Settings.AllowPrecompiled = previous; }
	}

	// No cross-operation cache may hide edits made after a previous traversal.
	[TestMethod]
	public void SeparateOperations_ReadChangedFile()
	{
		WithFiles(root =>
		{
			string path = Path.Combine(root, "Leaf.pts");
			System.IO.File.WriteAllText(path, "prototype Before;");
			var project = Project(root, "include \"Leaf.pts\";");
			string before = Compiler.GetAllIncludedFiles(project, false)[0].RawCode;
			System.IO.File.WriteAllText(path, "prototype After;");
			string after = Compiler.GetAllIncludedFiles(project, false)[0].RawCode;
			Assert.AreNotEqual(before, after);
		});
	}

	// Use temporary source files only; no runtime project or installed .pts file is modified.
	private static ProtoScript.File Project(string root, string text)
	{
		string path = Path.Combine(root, "Project.pts");
		System.IO.File.WriteAllText(path, text);
		return ProtoScript.Parsers.Files.Parse(path);
	}

	private static void WithFiles(Action<string> test)
	{
		string root = Path.Combine(Path.GetTempPath(), "include-dedup-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try { test(root); }
		finally { Directory.Delete(root, true); }
	}
}
