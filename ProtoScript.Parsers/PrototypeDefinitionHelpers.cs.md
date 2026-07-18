# PrototypeDefinitionHelpers.cs Change History

## Ontology Converter Migration (2026-07-18)
- Migrated the established ontology-prototype-to-partial-ProtoScript conversion and `SimpleGenerator` materialization behavior into `ProtoScript.Parsers`.
- Mechanically adapted inheritance lookup to `Prototype.GetTypeOfs()` plus `Prototypes.GetPrototype(...)`, preserved `RightOfLast` field-name emission required by the interpreter scope, and added current boolean/double and `NativeValuePrototype` support.
- Kept only graph conversion concerns: inheritance, exact property names, children, associations, native values, collections, and prototype identifiers.