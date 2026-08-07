# PrototypeCompilerDiagnostics_Tests.cs Change History

## Cold Lazy Function Annotation Regressions (2026-08-07)
- Added coverage proving missing function symbols preserve an explicit diagnostic and return an empty statement collection.
- Added coverage proving file-level function annotations resolve their function runtime information from global scope even when another compiler scope is active.
