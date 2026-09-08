using ProtoScript.Interpretter;
using System.Text.Json;
namespace ProtoScript.Tests
{
	[TestClass]
	public class CompilationTimingProfileTests
	{
		[TestMethod]
		public void EnabledProfileDoesNotParseMissingLazyTarget()
		{
			string root = Path.Combine(Path.GetTempPath(), "TimingLazy", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				string path = Path.Combine(root, "Project.pts");
				System.IO.File.WriteAllText(path, "include lazy \"Missing.pts\"; prototype TimingRoot;");
				var rows = new List<string>();
				var compiler = new Compiler { TimingProfile = new CompilationTimingProfile(rows.Add) };
				compiler.Initialize();
				compiler.CompileProject(path);
				Assert.AreEqual(1, compiler.LazyIncludeDeclarations.Count);
				Assert.IsTrue(rows.Any(x => x.Contains("CompileProject")));
				Assert.IsFalse(compiler.Files.Any(x => x.Info.FullName.EndsWith("Missing.pts")));
			}
			finally { Directory.Delete(root, true); }
		}
		[TestMethod]
		public void ScopeEmitsOnceWithStableCorrelation()
		{
			var rows = new List<string>(); var profile = new CompilationTimingProfile(rows.Add);
			var scope = profile.Measure("Stage", "DefinePrototypes", "fixture.pts"); scope.Dispose(); scope.Dispose();
			Assert.AreEqual(1, rows.Count);
			using var row = JsonDocument.Parse(rows[0]);
			Assert.AreEqual(profile.Id, row.RootElement.GetProperty("ProfileId").GetString());
			Assert.AreEqual("DefinePrototypes", row.RootElement.GetProperty("Name").GetString());
			Assert.IsTrue(row.RootElement.GetProperty("ElapsedMs").GetDouble() >= 0);
		}
		[TestMethod]
		public void ExceptionUnwindStillEmitsTiming()
		{
			var rows = new List<string>(); var profile = new CompilationTimingProfile(rows.Add);
			try { using var scope = profile.Measure("Stage", "Failure"); throw new InvalidOperationException("fixture"); }
			catch (InvalidOperationException) { }
			Assert.AreEqual(1, rows.Count);
		}
	}
}
