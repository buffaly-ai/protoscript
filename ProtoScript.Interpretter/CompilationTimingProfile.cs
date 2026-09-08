using System.Diagnostics;
using System.Text.Json;

namespace ProtoScript.Interpretter
{
	// Opt-in diagnostics only: never parses, resolves or loads additional modules.
	public sealed class CompilationTimingProfile
	{
		public string Id { get; } = Guid.NewGuid().ToString("N");
		private readonly Action<string> _write;
		private int _details; private readonly Dictionary<string, double> _inner = new();
		public CompilationTimingProfile(Action<string> write) { _write = write ?? throw new ArgumentNullException(nameof(write)); }
		public IDisposable Measure(string kind, string name, string file = "", bool detail = false)
			=> new TimingScope(this, kind, name, file, detail);
		private sealed class TimingScope : IDisposable
		{
			private readonly CompilationTimingProfile _owner;
			private readonly string _kind, _name, _file;
			private readonly bool _detail;
			private readonly long _start = Stopwatch.GetTimestamp();
			private int _disposed;
			public TimingScope(CompilationTimingProfile owner, string kind, string name, string file, bool detail)
			{ _owner = owner; _kind = kind; _name = name; _file = file; _detail = detail; }
			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
				double milliseconds = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
				if (_kind == "Inner") { string key = _name + "|" + _file; _owner._inner.TryGetValue(key, out double prior); _owner._inner[key] = prior + milliseconds; return; }
if (_kind == "Project") foreach (var entry in _owner._inner) { var parts = entry.Key.Split('|', 2); _owner._write(JsonSerializer.Serialize(new { ProfileId = _owner.Id, Kind = "InnerTotal", Name = parts[0], Prototype = parts[1], ElapsedMs = entry.Value })); }
if (_detail && milliseconds < 10) return;
				if (_detail && Interlocked.Increment(ref _owner._details) > 100) return;
				_owner._write(JsonSerializer.Serialize(new { ProfileId = _owner.Id, ProcessId = Environment.ProcessId, Kind = _kind, Name = _name, File = _file, ElapsedMs = milliseconds, DetailLimit = 100, DetailThresholdMs = 10 }));
			}
		}
	}
}
