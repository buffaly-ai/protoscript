using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class CompileProjectLayoutRegression_Tests
	{
		[TestInitialize]
		public void Setup()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		public void CompileProject_WithIncludesImportsAndExternPrototype_Succeeds()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"include ""Imports.pts"";
include ""Skill.pts"";",
					importsContents:
@"reference Ontology.Simulation Ontology.Simulation;
import Ontology.Simulation Ontology.Simulation.StringWrapper String;
extern prototype ExternalThing;
extern String RuntimeMessage;",
					skillContents:
@"prototype Skill
{
	function Echo() : String
	{
		return RuntimeMessage;
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));

				Assert.AreEqual(0, compiler.Diagnostics.Count);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		[TestMethod]
		public void CompileProject_WithMalformedImportPath_ThrowsHelpfulParseError()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				WriteProjectFiles(
					tempDir,
					projectContents:
@"import Invalid Path/Skill.pts;
include ""Imports.pts"";
include ""Skill.pts"";",
					importsContents:
@"reference Ontology.Simulation Ontology.Simulation;
import Ontology.Simulation Ontology.Simulation.StringWrapper String;
extern prototype ExternalThing;
extern String RuntimeMessage;",
					skillContents:
@"prototype Skill
{
	function Echo() : String
	{
		return RuntimeMessage;
	}
}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				ProtoScript.Parsers.ProtoScriptParsingException err =
					Assert.ThrowsException<ProtoScript.Parsers.ProtoScriptParsingException>(
						() => compiler.CompileProject(Path.Combine(tempDir, "Project.pts")));

				Assert.IsNotNull(err.Expected);
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static void WriteProjectFiles(string tempDir, string projectContents, string importsContents, string skillContents)
		{
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Project.pts"), projectContents);
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Imports.pts"), importsContents);
			System.IO.File.WriteAllText(Path.Combine(tempDir, "Skill.pts"), skillContents);
		}

		[TestMethod]
		public void CompileProject_ResolvesIncludesFromProjectDirectoryWithoutProjectFileAlias()
		{
			string tempDir = CreateTempDirectory();
			try
			{
				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Project.pts"),
					"include \"Imports.pts\";" + Environment.NewLine + "include \"Nested/Skill.pts\";");
				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Imports.pts"),
					"reference Ontology.Simulation Ontology.Simulation;" + Environment.NewLine
					+ "import Ontology.Simulation Ontology.Simulation.StringWrapper String;" + Environment.NewLine
					+ "extern String RuntimeMessage;");
				Directory.CreateDirectory(Path.Combine(tempDir, "Nested"));
				System.IO.File.WriteAllText(
					Path.Combine(tempDir, "Nested", "Skill.pts"),
					"prototype Skill" + Environment.NewLine
					+ "{" + Environment.NewLine
					+ "\tfunction Echo() : String" + Environment.NewLine
					+ "\t{" + Environment.NewLine
					+ "\t\treturn RuntimeMessage;" + Environment.NewLine
					+ "\t}" + Environment.NewLine
					+ "}");

				Compiler compiler = new Compiler();
				compiler.Initialize();

				compiler.CompileProject(Path.Combine(tempDir, "Project.pts"));

				Assert.AreEqual(0, compiler.Diagnostics.Count);
				Assert.IsFalse(Directory.Exists(Path.Combine(tempDir, "Project.pts\\Nested")));
				Assert.IsFalse(System.IO.File.Exists(Path.Combine(tempDir, "Project.pts\\Nested", "Skill.pts")));
			}
			finally
			{
				DeleteDirectory(tempDir);
			}
		}

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "ProtoScriptCompileProject_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
