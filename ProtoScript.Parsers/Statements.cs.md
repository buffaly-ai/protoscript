
## 2026-10-02 — Approved per-input quiet memory parsing
Parse(Tokenizer) rethrows before Logs.LogError when per-input LogParsingErrors is false. Quiet validation remains fail-fast even if global best-effort setting is enabled.
Validation: parser 4/4, critic 11/11, NLMemory runtime suite 69/69; fresh Release worker/NLMemory publish succeeded. Packaged parser hashes match; packaged probe asserts malformed memory rejection with no application log and ordinary parsing with error file. No live deployment or worker restart. Full parser suite not run.

## 2026-10-14 — Commit reviewed fail-fast quiet parsing
Matt directed committing the reviewed existing correction. Statements.Parse retains rethrow-before-logging for quiet inputs, including when global best-effort parsing is enabled; ordinary behavior unchanged. Current parser regression tests4/4 and NLMemory non-integration75/75 passed; staging acceptance pending canonical committed build.
