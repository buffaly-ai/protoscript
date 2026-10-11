# IncludeDeduplicationTests history

## 2026-10-10 — Regression guard against parse-before-deduplication
- Added behavioral deleted-input proof for serial/parallel aliases, wildcard diamonds and precompiled JSON; preserve serial ordering, cyclic traversal, strict/best-effort errors and reread-on-next-operation behavior. No installed project files changed.
- Validation:29/29 focused include/CLI/lazy cases passed. No broad suite claim. Staging validation pending. ProtoScript AGENTS manual post-review commit approval remains applicable.

## 2026-10-11 — Explicit commit approval and regression rerun
- Matt approved the scoped four-file commit and staging completion. Test semantics are unchanged: serial/parallel deleted-alias, wildcard ordering, cycle, per-site errors, strict missing-file, precompiled identity and next-operation freshness.
- Current include/CommandLine/lazy filter passed 22/22, zero failures/skips. Prior staging baseline remains four duplicate-read failures (10/14 passed); installed post-delivery verification is not yet claimed. Unrelated quiet-parser tests are not included in this commit.

## 2026-10-11 — Installed regression proof completed
- Registered validated Build 1189 applied to staging. Installed compiler/snapshot harness passed 14/14 for worker and independently 14/14 for web with zero failures/skips. Includes nine include cases and five existing snapshot cases; the four old duplicate-read failures now pass.
- Package/installed compiler hashes match in both layouts; last web and worker harness hashes independently match their installed references. Source tests use isolated temporary files; installed tests do not mutate live project, database, or provider workloads. No full-suite or production performance claim. Matt-local four monitored DLL hashes remain unchanged.
