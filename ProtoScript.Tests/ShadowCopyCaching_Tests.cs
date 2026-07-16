using ProtoScript.Interpretter;
using System.Diagnostics;
using System.Reflection;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class ShadowCopyCaching_Tests
	{
		[TestMethod]
		public void PrepareShadowCopyDirectory_UsesSingleDirectoryPerSourceFolder()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ShadowDirReuse_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				string firstDll = Path.Combine(tempDir, "First.dll");
				string secondDll = Path.Combine(tempDir, "Second.dll");
				System.IO.File.WriteAllText(firstDll, "first");
				System.IO.File.WriteAllText(secondDll, "second");

				string firstShadowDir = InvokePrepareShadowCopyDirectory(firstDll);
				string secondShadowDir = InvokePrepareShadowCopyDirectory(secondDll);

				Assert.AreEqual(firstShadowDir, secondShadowDir);
				Assert.IsTrue(System.IO.File.Exists(Path.Combine(firstShadowDir, "First.dll")));
				Assert.IsTrue(System.IO.File.Exists(Path.Combine(firstShadowDir, "Second.dll")));
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void PrepareShadowCopyDirectory_UsesNewDirectoryWhenSourceDllChanges()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ShadowRefresh_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				string sourceDll = Path.Combine(tempDir, "HotSwap.dll");
				System.IO.File.WriteAllText(sourceDll, "version-1");

				string shadowDir = InvokePrepareShadowCopyDirectory(sourceDll);
				string shadowDll = Path.Combine(shadowDir, "HotSwap.dll");
				Assert.IsTrue(System.IO.File.Exists(shadowDll));
				string firstShadowContent = System.IO.File.ReadAllText(shadowDll);
				Assert.AreEqual("version-1", firstShadowContent);

				System.Threading.Thread.Sleep(1200);
				System.IO.File.WriteAllText(sourceDll, "version-2-with-more-bytes");

				string secondShadowDir = InvokePrepareShadowCopyDirectory(sourceDll);
				string secondShadowDll = Path.Combine(secondShadowDir, "HotSwap.dll");
				Assert.AreNotEqual(shadowDir, secondShadowDir);
				Assert.IsTrue(System.IO.File.Exists(secondShadowDll));
				string secondShadowContent = System.IO.File.ReadAllText(secondShadowDll);
				Assert.AreEqual("version-2-with-more-bytes", secondShadowContent);
				Assert.AreEqual("version-1", System.IO.File.ReadAllText(shadowDll));
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void LoadAssemblyFromResolvedPath_AfterSameIdentityDllChanges_LoadsNewImplementation()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ShadowAssemblyReload_" + Guid.NewGuid().ToString("N"));
			string projectDir = Path.Combine(tempDir, "project");
			string activeDir = Path.Combine(tempDir, "active");
			Directory.CreateDirectory(projectDir);
			Directory.CreateDirectory(activeDir);
			try
			{
				System.IO.File.WriteAllText(Path.Combine(projectDir, "ReloadFixture.csproj"),
					"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework><AssemblyName>ProtoScript.ReloadFixture</AssemblyName><Version>1.0.0</Version></PropertyGroup></Project>");
				string sourceFile = Path.Combine(projectDir, "ReloadValue.cs");
				string activeDll = Path.Combine(activeDir, "ProtoScript.ReloadFixture.dll");

				System.IO.File.WriteAllText(sourceFile, "namespace ReloadFixture; public static class ReloadValue { public static string Get() => \"version-1\"; }");
				BuildFixture(projectDir, Path.Combine(tempDir, "build1"));
				System.IO.File.Copy(Path.Combine(tempDir, "build1", "ProtoScript.ReloadFixture.dll"), activeDll, true);
				Assembly firstAssembly = InvokeLoadAssemblyFromResolvedPath(activeDll);
				Assert.AreEqual("version-1", InvokeFixture(firstAssembly));

				System.IO.File.WriteAllText(sourceFile, "namespace ReloadFixture; public static class ReloadValue { public static string Get() => \"version-2\"; }");
				BuildFixture(projectDir, Path.Combine(tempDir, "build2"));
				System.IO.File.Copy(Path.Combine(tempDir, "build2", "ProtoScript.ReloadFixture.dll"), activeDll, true);
				System.IO.File.SetLastWriteTimeUtc(activeDll, DateTime.UtcNow.AddSeconds(2));

				Assembly secondAssembly = InvokeLoadAssemblyFromResolvedPath(activeDll);
				Assert.AreEqual("version-2", InvokeFixture(secondAssembly),
					"A same-process ProtoScript reload executed the first loaded native assembly instead of the rebuilt DLL.");
				Assert.AreNotEqual(firstAssembly.ManifestModule.ModuleVersionId, secondAssembly.ManifestModule.ModuleVersionId,
					"The loader returned the old assembly identity/MVID after the DLL changed.");
			}
			finally
			{
				try
				{
					if (Directory.Exists(tempDir))
						Directory.Delete(tempDir, true);
				}
				catch
				{
					// Loaded fixture files can remain locked on some runtimes; temp cleanup is best effort.
				}
			}
		}

		[TestMethod]
		public void LoadAssemblyFromResolvedPath_ExactCopyOfLoadedAssembly_ReusesLoadedTypeIdentity()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_LoadedAssemblyIdentity_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			try
			{
				Assembly loadedAssembly = typeof(Compiler).Assembly;
				string copiedAssemblyPath = Path.Combine(tempDir, Path.GetFileName(loadedAssembly.Location));
				System.IO.File.Copy(loadedAssembly.Location, copiedAssemblyPath, true);

				Assembly resolvedAssembly = InvokeLoadAssemblyFromResolvedPath(copiedAssemblyPath);

				Assert.AreSame(loadedAssembly, resolvedAssembly,
					"An unchanged host assembly copy must reuse the default loaded assembly so injected runtime interfaces retain one type identity.");
			}
			finally
			{
				if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
		}

		[TestMethod]
		public void CompileAndRun_AfterSameProcessResetAndNativeDllChange_DoesNotKeepNullFromOldAssembly()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), "ProtoScript_ResetNativeNull_" + Guid.NewGuid().ToString("N"));
			string projectDir = Path.Combine(tempDir, "project");
			string activeDir = Path.Combine(projectDir, "lib");
			string assemblyName = "ProtoScript.ResetNullFixture." + Guid.NewGuid().ToString("N");
			Directory.CreateDirectory(projectDir);
			Directory.CreateDirectory(activeDir);
			try
			{
				string fixtureProject = Path.Combine(projectDir, "ReloadFixture.csproj");
				System.IO.File.WriteAllText(fixtureProject,
					$"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework><AssemblyName>{assemblyName}</AssemblyName><Version>1.0.0</Version><Nullable>enable</Nullable></PropertyGroup></Project>");
				string sourceFile = Path.Combine(projectDir, "ReloadValue.cs");
				string activeDll = Path.Combine(activeDir, assemblyName + ".dll");
				string protoScriptFile = Path.Combine(projectDir, "Action.pts");
				string projectFile = Path.Combine(projectDir, "Project.pts");
				System.IO.File.WriteAllText(protoScriptFile, $$"""
reference "lib/{{assemblyName}}.dll" ReloadAsm;
import ReloadAsm ReloadFixture.ReloadValue ReloadValue;
function Execute() : string
{
	return ReloadValue.Get();
}
""");
				System.IO.File.WriteAllText(projectFile, "include \"Action.pts\";");

				System.IO.File.WriteAllText(sourceFile, "namespace ReloadFixture; public static class ReloadValue { public static string? Get() => null; }");
				BuildFixture(projectDir, Path.Combine(tempDir, "build1"));
				System.IO.File.Copy(Path.Combine(tempDir, "build1", assemblyName + ".dll"), activeDll, true);
				object? firstResult = CompileAndRunProject(projectFile);
				Assert.IsNull(firstResult, "Version 1 establishes the null native return observed in the broken session.");

				System.IO.File.WriteAllText(sourceFile, "namespace ReloadFixture; public static class ReloadValue { public static string? Get() => \"version-2\"; }");
				BuildFixture(projectDir, Path.Combine(tempDir, "build2"));
				System.IO.File.Copy(Path.Combine(tempDir, "build2", assemblyName + ".dll"), activeDll, true);
				System.IO.File.SetLastWriteTimeUtc(activeDll, DateTime.UtcNow.AddSeconds(2));

				object? secondResult = CompileAndRunProject(projectFile);
				Assert.AreEqual("version-2", secondResult,
					"After a same-process reset, ProtoScript still invoked the old same-identity native DLL and propagated its null return.");
			}
			finally
			{
				try
				{
					if (Directory.Exists(tempDir))
						Directory.Delete(tempDir, true);
				}
				catch
				{
				}
			}
		}

		private static object? CompileAndRunProject(string projectFile)
		{
			Compiler compiler = new Compiler();
			compiler.Initialize();
			List<ProtoScript.Interpretter.Compiled.Statement> statements = compiler.CompileProject(projectFile);
			NativeInterpretter interpretter = new NativeInterpretter(compiler);
			foreach (ProtoScript.Interpretter.Compiled.Statement statement in statements)
				interpretter.Evaluate(statement);
			return interpretter.RunMethodAsObject(null, "Execute", new List<object>());
		}
		private static void BuildFixture(string projectDir, string outputDir)
		{
			var startInfo = new ProcessStartInfo("dotnet", $"build \"{Path.Combine(projectDir, "ReloadFixture.csproj")}\" -c Release -o \"{outputDir}\" --nologo")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start fixture build.");
			string standardOutput = process.StandardOutput.ReadToEnd();
			string standardError = process.StandardError.ReadToEnd();
			process.WaitForExit();
			Assert.AreEqual(0, process.ExitCode, standardOutput + Environment.NewLine + standardError);
		}

		private static Assembly InvokeLoadAssemblyFromResolvedPath(string sourceAssemblyPath)
		{
			MethodInfo? method = typeof(Compiler).GetMethod(
				"LoadAssemblyFromResolvedPath",
				BindingFlags.NonPublic | BindingFlags.Static,
				binder: null,
				types: new[] { typeof(string), typeof(string).MakeByRefType() },
				modifiers: null);
			Assert.IsNotNull(method);
			object?[] args = { sourceAssemblyPath, null };
			object? result = method!.Invoke(null, args);
			Assert.IsNotNull(result);
			return (Assembly)result!;
		}

		private static string InvokeFixture(Assembly assembly)
		{
			System.Type? fixtureType = assembly.GetType("ReloadFixture.ReloadValue", throwOnError: false);
			Assert.IsNotNull(fixtureType);
			MethodInfo? getMethod = fixtureType!.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
			Assert.IsNotNull(getMethod);
			return (string)(getMethod!.Invoke(null, null) ?? string.Empty);
		}

		private static string InvokePrepareShadowCopyDirectory(string sourceAssemblyPath)
		{
			MethodInfo? method = typeof(Compiler).GetMethod(
				"PrepareShadowCopyDirectory",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(method);
			object? result = method!.Invoke(null, new object[] { sourceAssemblyPath });
			Assert.IsNotNull(result);
			return (string)result!;
		}
	}
}
