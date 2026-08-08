# SimpleGenerator.cs Change History

## Unary Expression Round-Trip Safety (2026-08-08)
- Emit both the operator token and operand for prefix unary expressions such as `!value.Contains(...)`, `-1`, and `~flags`.
- Preserve postfix position for parsed `++` and `--` expressions.
- Fail generation when a unary AST is missing its token or operand instead of writing corrupt, reparsable source such as `if (!)` or `return -;`.

## Initializer Emission for Non-Assignment Entries (2026-04-15)
- Updated simple generation for `new` initializers to emit both assignment initializers and non-assignment initializer expressions.
- Design Decision: preserve collection-entry initializer syntax in generated output instead of assuming every initializer is `Name = Value`.
