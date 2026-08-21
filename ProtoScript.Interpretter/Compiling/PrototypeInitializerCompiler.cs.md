# PrototypeInitializerCompiler.cs Change History

## Initializer RHS Diagnostics (2026-08-21)
- Guard prototype initializer assignment lowering when the right-hand expression fails to compile, and emit a property-specific diagnostic instead of constructing an `AssignmentOperator` with a null `Right` expression.
- Design Decision: unresolved RHS references and invalid RHS expressions are compiler diagnostics; runtime interpretation should never reach `NativeInterpretter.Evaluate(Expression)` with a null initializer RHS.