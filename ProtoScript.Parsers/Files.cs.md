# Files.cs Change History

## Import Path Strictness (2026-03-12)
- Updated `import` parsing to reject file-path usage such as `import "x.pts";`.
- Design Decision: enforce one clear contract where file inclusion must use `include ...;` and `import` is reserved for type imports.

## Quiet memory validation
Added a per-input logParsingErrors overload preserving the existing CLR signature. Quiet parses rethrow before logging and do not enter best-effort recovery. Default parser consumers remain unchanged.

## 2026-10-02 — Approved per-input quiet memory parsing
ParseFileContents keeps its one-argument CLR signature and adds bool logParsingErrors overload. Tokenizer carries the policy through nested parsing. Existing default remains true; no process-global suppression.
Validation: parser 4/4, critic 11/11, NLMemory runtime suite 69/69; fresh Release worker/NLMemory publish succeeded. Packaged parser hashes match; packaged probe asserts malformed memory rejection with no application log and ordinary parsing with error file. No live deployment or worker restart. Full parser suite not run.

## Continuation: full parser regression and baseline comparison
Full parser suite executed: 245 total, 239 passed, 5 failed, 1 skipped. The five failures (two CLI execution, shadow assembly identity, two Workbench serializer TypeLoadException cases) were rerun against a separately built HEAD baseline parser without the quiet-flag changes: all five failed there too. Evidence: parser-full-regression.log and parser-baseline-failures.log. These are demonstrated baseline failures, not ignored new failures. Focused flag tests remain 4/4; critic 11/11; memory runtime suite 69/69. No live deployment. Parser repository explicitly requires review followed by commit approval; source changes remain uncommitted pending that gate.

## 2026-10-14 — Admit reviewed quiet API to canonical builds
Matt directed committing the existing reviewed correction and building staging from that committed source. The installer archives HEAD over its copied parser source, so uncommitted Files/Statements/Tokenizer changes were excluded, causing installed NLMemory's bool-overload MissingMethodException. Preserve ordinary one-argument ABI, per-input quiet policy and fail-fast behavior; no fallback parsing or global suppression. Current validation: QuietValidationParsing_Tests 4/4 passed; NLMemory non-integration 75/75 passed. Staging validation will be recorded separately after canonical build. No Matt-local restart or DLL edit.
