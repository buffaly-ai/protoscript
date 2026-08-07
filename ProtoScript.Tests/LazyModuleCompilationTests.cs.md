# LazyModuleCompilationTests.cs Change History

## Cold Lazy Annotated Module Regression (2026-08-07)
- Added end-to-end `CompileAndAppendModule` coverage for an annotated file-level function compiled while a non-global scope is active.
- The regression protects strict cold-lazy module loading from scope-dependent missing-function diagnostics and annotation aggregation failures.
