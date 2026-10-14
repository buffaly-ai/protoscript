using ProtoScript.Parsers;

namespace ProtoScript.Tests;

[TestClass]
[DoNotParallelize]
public sealed class QuietValidationParsing_Tests
{
	[TestMethod]
	// Missing terminators still fail with an actionable diagnostic in quiet memory validation.
	public void QuietParsing_MissingSemicolon_ThrowsSyntaxDiagnostic()
	{
		var error = Assert.ThrowsException<ProtoScriptTokenizingException>(() =>
			Files.ParseFileContents("prototype Memory { Notes = \"receipt\"\n}", logParsingErrors: false));
		StringAssert.Contains(error.Message, "Expected: [;]");
		// Logs.LogError stamps GUID/DateEntered before writing; neither may be added.
		Assert.IsFalse(error.Data.Contains("GUID"));
		Assert.IsFalse(error.Data.Contains("DateEntered"));
	}

	[TestMethod]
	// Valid inputs retain their initializer and quiet policy never leaks to unrelated parses.
	public void QuietParsing_ValidDefinition_PreservesAssignmentsAndDefaultPolicy()
	{
		var file = Files.ParseFileContents("prototype Memory { Notes = \"receipt\"; }", logParsingErrors: false);
		Assert.AreEqual(1, file.PrototypeDefinitions.Count);
		Assert.IsTrue(new Tokenizer("Notes = \"receipt\";").LogParsingErrors);
	}

	[TestMethod]
	// Quiet validation must not use the global best-effort branch or return partial parsed data.
	public void QuietStatementParsing_BestEffortEnabled_StillRejects()
	{
		bool previous = Settings.FailOnParsingErrors;
		try
		{
			Settings.FailOnParsingErrors = false;
			Assert.ThrowsException<ProtoScriptTokenizingException>(() =>
				Statements.Parse(new Tokenizer("Notes = \"receipt\"\n}") { LogParsingErrors = false }));
		}
		finally { Settings.FailOnParsingErrors = previous; }
	}
	[TestMethod]
	// The opt-out must prevent actual error files while ordinary parsing still logs.
	public void QuietParsing_ErrorFileSuppression_IsPerInput()
	{
		string directory = Path.Combine(Path.GetTempPath(), "quiet-parser-" + Guid.NewGuid().ToString("N"));
		string previousPath = Logs.ErrorLog.LogPath;
		string previousName = Logs.ErrorLog.Name;
		bool previousFail = Settings.FailOnParsingErrors;
		try
		{
			Directory.CreateDirectory(directory);
			Logs.ErrorLog.LogPath = directory;
			Logs.ErrorLog.Name = "QuietParserRegression";
			Settings.FailOnParsingErrors = true;
			foreach (string text in new[] { "prototype Memory { Notes = \"receipt\"\n}", "prototype Memory { Notes = \"receipt\"\n EntityName = \"memory\"; }" })
				Assert.ThrowsException<ProtoScriptTokenizingException>(() => Files.ParseFileContents(text, logParsingErrors: false));
			Assert.AreEqual(0, Directory.GetFiles(directory, "error*", SearchOption.AllDirectories).Length);
			Assert.ThrowsException<ProtoScriptTokenizingException>(() => Files.ParseFileContents("prototype Ordinary { Notes = \"receipt\"\n}"));
			Assert.IsTrue(Directory.GetFiles(directory, "error*", SearchOption.AllDirectories).Any(path => new FileInfo(path).Length > 0));
		}
		finally
		{
			Settings.FailOnParsingErrors = previousFail;
			Logs.ErrorLog.LogPath = previousPath;
			Logs.ErrorLog.Name = previousName;
		}
	}}