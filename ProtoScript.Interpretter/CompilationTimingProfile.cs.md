# Compilation timing diagnostics

## Opt-in load timing (2026-09-07)
- BUFFALY_PROTOSCRIPT_LOAD_TIMING=1 enables per-compiler correlated project/include-parse/stage/file/prototype/lazy-append timing. Disabled by default; no changes to module discovery or execution order.
- Detail rows record only operations >=10ms, capped at100 per compiler. Stage totals include nested detail timings and must not be added to them. No source contents or credentials logged.
