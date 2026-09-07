# Compiler-owned nesting

DeclareNestedPrototypes links each successfully declared lexical child to its known parent before local symbol insertion. Generic temporary registration does not infer nesting from names. No global scan or secondary index is used. Tests cover recursion, lazy modules, values, reset/reload and isolated contexts.
