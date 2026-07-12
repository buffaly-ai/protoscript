using System.Collections.Concurrent;
using ProtoScript.Interpretter;
using ProtoScript.Interpretter.RuntimeInfo;
using ProtoScript.Parsers;

namespace ProtoScript.Tests
{
	[TestClass]
	public sealed class NativeInterpretterConcurrentRunMethod_Tests
	{
		public sealed class CoordinatedReceiver
		{
			public static readonly ManualResetEventSlim AEntered = new(false);
			public static readonly ManualResetEventSlim BEntered = new(false);
			public static readonly ManualResetEventSlim AllowBToReturn = new(false);
			public static readonly ConcurrentQueue<string> Events = new();

			public static void Reset()
			{
				AEntered.Reset();
				BEntered.Reset();
				AllowBToReturn.Reset();
				while (Events.TryDequeue(out _)) { }
			}

			public string ReturnWithCoordinatedOverlap(string value)
			{
				Events.Enqueue(value + ":entered");
				if (value == "A")
				{
					AEntered.Set();
					if (!BEntered.Wait(TimeSpan.FromSeconds(5)))
						throw new TimeoutException("B did not enter before A returned.");

					Events.Enqueue("A:returning-while-B-scope-is-active");
					return "A";
				}

				if (value == "B")
				{
					BEntered.Set();
					if (!AllowBToReturn.Wait(TimeSpan.FromSeconds(5)))
						throw new TimeoutException("B was not released.");

					Events.Enqueue("B:returning-after-A-finished");
					return "B";
				}

				return value;
			}
		}

		[TestInitialize]
		public void Init()
		{
			Initializer.Initialize();
		}

		[TestMethod]
		[Timeout(10000)]
		public void RunMethodAsObject_ConcurrentCallsOnSameInterpreter_FailsFastWithClearDiagnostic()
		{
			string code = @"
extern CoordinatedReceiver ext;
function A() : string
{
	return ext.ReturnWithCoordinatedOverlap(""A"");
}
function B() : string
{
	return ext.ReturnWithCoordinatedOverlap(""B"");
}";

			ProtoScript.File file = Files.ParseFileContents(code);
			Compiler compiler = new();
			compiler.Initialize();
			compiler.Symbols.InsertSymbol("CoordinatedReceiver", new DotNetTypeInfo(typeof(CoordinatedReceiver)));
			ProtoScript.Interpretter.Compiled.File compiled = compiler.Compile(file);
			Assert.AreEqual(0, compiler.Diagnostics.Count, string.Join(Environment.NewLine, compiler.Diagnostics.Select(x => x.ToString())));

			NativeInterpretter interpretter = new(compiler);
			interpretter.InsertGlobalObject("ext", new CoordinatedReceiver());
			interpretter.Evaluate(compiled);

			CoordinatedReceiver.Reset();

			Task<object?> callA = Task.Run(() => interpretter.RunMethodAsObject(null, "A", new List<object>()));
			Assert.IsTrue(CoordinatedReceiver.AEntered.Wait(TimeSpan.FromSeconds(5)), "A did not enter the coordinated native method.");

			Task<object?> callB = Task.Run(() => interpretter.RunMethodAsObject(null, "B", new List<object>()));
			try
			{
				callB.Wait(TimeSpan.FromSeconds(5));
			}
			catch (AggregateException)
			{
				// Expected: Task.Wait surfaces the fail-fast guard exception from the task.
			}
			Assert.IsTrue(callB.IsFaulted, "B should fail fast instead of entering a concurrent call on the same interpreter.");

			InvalidOperationException? guardException = callB.Exception?.GetBaseException() as InvalidOperationException;
			Assert.IsNotNull(guardException, callB.Exception?.ToString());
			Assert.AreEqual(
				"NativeInterpretter does not support concurrent entry-point execution. Create a separate interpreter instance per concurrent call.",
				guardException!.Message);
			Assert.IsFalse(CoordinatedReceiver.BEntered.IsSet, "B should not enter the coordinated native method after the fail-fast guard rejects the call.");

			CoordinatedReceiver.BEntered.Set();
			Assert.IsTrue(callA.Wait(TimeSpan.FromSeconds(5)), "A did not finish after the test released its wait. Events: " + string.Join(", ", CoordinatedReceiver.Events));
			Assert.AreEqual("A", callA.Result, "Concurrent call A should still return its own native result. Events: " + string.Join(", ", CoordinatedReceiver.Events));
		}
	}
}
