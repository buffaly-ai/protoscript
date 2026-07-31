using ProtoScript.Parsers;
using ProtoScript.Interpretter;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class IncludeStatementTests
	{
		[TestMethod]
		public void ParseIncludeStatement_WithQuotedPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include \"Critic/CriticOps.pts\";");
			Assert.AreEqual("Critic/CriticOps.pts", statement.FileName);
			Assert.IsFalse(statement.Recursive);
			Assert.IsFalse(statement.Lazy);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithLazyQuotedPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include lazy \"Skills/FFmpeg/index.pts\";");

			Assert.AreEqual("Skills/FFmpeg/index.pts", statement.FileName);
			Assert.IsTrue(statement.Lazy);
			Assert.IsFalse(statement.Recursive);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithLazyRecursive_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				IncludeStatements.Parse("include lazy recursive \"Skills/**/*.pts\";"));

			Assert.AreEqual("non-recursive lazy include", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot be recursive", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void CompileProject_WithLazyInclude_RecordsDeclarationWithoutParsingTarget(bool allowParallelism)
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), "ProtoScriptLazyIncludeTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDirectory);
			string projectPath = Path.Combine(tempDirectory, "Project.pts");
			string missingModulePath = Path.Combine(tempDirectory, "MissingSkill", "index.pts");
			string prototypeName = "LazyTraversalRoot_" + Guid.NewGuid().ToString("N");
			System.IO.File.WriteAllText(projectPath, $"include lazy \"MissingSkill/index.pts\";{Environment.NewLine}prototype {prototypeName};");

			try
			{
				Compiler compiler = new Compiler { AllowParallelism = allowParallelism };
				compiler.Initialize();
				compiler.CompileProject(projectPath);

				Assert.AreEqual(1, compiler.Files.Count);
				Assert.AreEqual(1, compiler.LazyIncludeDeclarations.Count);
				Assert.AreEqual(Path.GetFullPath(projectPath), compiler.LazyIncludeDeclarations[0].SourceFilePath);
				Assert.AreEqual(Path.GetFullPath(missingModulePath), compiler.LazyIncludeDeclarations[0].ModuleFilePath);
				Assert.AreEqual(Path.GetFullPath(projectPath), compiler.LazyIncludeDeclarations[0].SourceInfo.File);
			}
			finally
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}

		[TestMethod]
		public void ParseIncludeStatement_WithUnquotedForwardSlashPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include Path/File.pts;");
			Assert.AreEqual("Path/File.pts", statement.FileName);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithUnquotedBackslashPath_Succeeds()
		{
			ProtoScript.IncludeStatement statement = IncludeStatements.Parse("include Path\\File.pts;");
			Assert.AreEqual("Path\\File.pts", statement.FileName);
		}

		[TestMethod]
		public void ParseIncludeStatement_WithWhitespaceInUnquotedPath_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				IncludeStatements.Parse("include Path With Space/File.pts;"));

			Assert.AreEqual("path literal", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot contain whitespace", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import Path/File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_BackslashPath_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import Path\\File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithImportPathAlias_FileNameOnly_ThrowsHelpfulError()
		{
			ProtoScriptParsingException err = Assert.ThrowsException<ProtoScriptParsingException>(() =>
				ProtoScript.Parsers.Files.ParseFileContents("import File.pts;"));

			Assert.AreEqual("assembly alias", err.Expected);
			Assert.IsTrue(err.Explanation?.Contains("cannot target files", StringComparison.OrdinalIgnoreCase) ?? false);
			Assert.IsTrue(err.Explanation?.Contains("Use include", StringComparison.OrdinalIgnoreCase) ?? false);
		}

		[TestMethod]
		public void ParseFile_WithLegacyAssemblyImport_StaysImport()
		{
			ProtoScript.File file = ProtoScript.Parsers.Files.ParseFileContents("import Ontology Ontology.Collection Collection;");
			Assert.AreEqual(0, file.Includes.Count);
			Assert.AreEqual(1, file.Imports.Count);
			Assert.AreEqual("Ontology", file.Imports[0].Reference);
		}

		[TestMethod]
		public void TryParseImportAsInclude_LegacyAssemblyImport_ReturnsFalseAndRestoresCursor()
		{
			Tokenizer tok = new Tokenizer("import Ontology Ontology.Collection Collection;");
			int startCursor = tok.getCursor();

			bool parsed = IncludeStatements.TryParseImportAsInclude(tok, out ProtoScript.IncludeStatement includeStatement);

			Assert.IsFalse(parsed);
			Assert.IsNull(includeStatement);
			Assert.AreEqual(startCursor, tok.getCursor());
		}

		[TestMethod]
		public void TryParseImportAsInclude_PathImport_ReturnsIncludeWithoutExceptions()
		{
			Tokenizer tok = new Tokenizer("import Path/File.pts;");

			bool parsed = IncludeStatements.TryParseImportAsInclude(tok, out ProtoScript.IncludeStatement includeStatement);

			Assert.IsTrue(parsed);
			Assert.IsNotNull(includeStatement);
			Assert.AreEqual("Path/File.pts", includeStatement.FileName);
		}
	}
}
