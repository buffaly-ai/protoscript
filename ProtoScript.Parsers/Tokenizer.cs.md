
## 2026-10-02 — Approved per-input quiet memory parsing
Added per-input LogParsingErrors property defaulting true; no shared mutable logging flag.
Validation: parser 4/4, critic 11/11, NLMemory runtime suite 69/69; fresh Release worker/NLMemory publish succeeded. Packaged parser hashes match; packaged probe asserts malformed memory rejection with no application log and ordinary parsing with error file. No live deployment or worker restart. Full parser suite not run.

## 2026-10-14 — Commit reviewed per-input policy
Matt directed committing the reviewed existing correction. LogParsingErrors remains tokenizer-local and defaults true; quiet policy does not leak to unrelated parses. Current parser regression tests4/4 and NLMemory non-integration75/75 passed; staging acceptance pending canonical committed build.
