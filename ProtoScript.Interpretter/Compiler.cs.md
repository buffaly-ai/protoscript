# Compiler.cs Change History

## Initializer RHS Diagnostics (2026-08-21)
- Added an explicit diagnostic for capitalized boolean literal tokens `True` and `False`; ProtoScript boolean literals are lowercase `true` and `false`, so these tokens should not fall through as unresolved identifiers.
- Design Decision: report literal-casing mistakes at identifier compilation time so initializer assignment failures surface an actionable language diagnostic instead of later producing a null compiled expression.

## Cold Lazy Function Body Scope (2026-08-07)
- Record the exact `FunctionRuntimeInfo` created for each parsed `FunctionDefinition` and use that declaration when compiling its body.
- Design Decision: body compilation must not rebind by name because common prototype method names such as `Execute` are ambiguous, and an incidental caller scope during strict lazy activation must not select or hide a different declaration.

## Missing Function Annotation Collection Contract (2026-08-07)
- Changed function-annotation compilation to return an empty statement list after emitting a missing-function diagnostic instead of returning `null`.
- Resolve file-level function annotations from the global symbol scope where those functions are declared, rather than from incidental active compiler scope.
- Design Decision: compilation stages aggregate statement collections with `AddRange`; a known diagnostic path must preserve that collection contract so strict lazy compilation reports the real missing-symbol diagnostic rather than throwing `ArgumentNullException(collection)`.

## Include Missing-File Diagnostics (2026-03-12)
- Updated include-file parsing flow to carry include-site context into missing-file failures.
- Design Decision: wrap missing include targets as `ProtoScriptCompilerException` with `IncludeStatement.Info` so callers receive file/offset for the failing include line instead of a contextless runtime error.

## StringRef Built-in Type Registration (2026-03-13)
- Added built-in type aliases `StringRef` and `stringref` during compiler initialization.
- Design Decision: expose string-reference return/parameter contracts without requiring project-level imports.

## Method Evaluation Null-Guard Diagnostics (2026-03-13)
- Hardened `Compile(MethodEvaluation)` to report diagnostics when method name is missing, `nameof` has no parameters, or a non-function symbol is invoked.
- Design Decision: convert prior `NullReferenceException` crash paths into actionable compiler diagnostics with statement context so best-effort mode can skip offending files cleanly.

## Dotnet Collection Initializer Entry Lowering (2026-04-15)
- Updated dotnet `new` compilation to bucket initializer entries by kind during lowering: named member assignments map into `DotNetNewInstance.MemberInitializers`, and collection-style entries map into `DotNetNewInstance.CollectionInitializers`.
- Design Decision: keep initializer lowering explicit and typed in `DotNetNewInstance` rather than rejecting collection entries at compile time, so runtime can apply deterministic initializer semantics.
- Note: this trimmed feature pass is scoped to member + collection entry lowering for dotnet `new` object creation.

## Preserve Host Type Identity During Shadow Loading (2026-07-15)
- Reuse an already loaded assembly when a file reference has the same assembly identity and exact file hash.
- Design Decision: unchanged host contracts must retain the default load-context type identity, while changed same-version DLLs continue through shadow loading for hot reload.
