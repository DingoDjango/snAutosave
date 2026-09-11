# Class members order
0. Subclasses (`enum`, nested, helper classes)
1. Private `const` fields
2. Private `readonly` fields
3. Other private fields
4. Protected fields
5. Public `const` fields
6. Public `readonly` fields
7. Public fields
8. Private methods
9. Internal methods
10. Public methods

# Variables
- Variable names descriptive, explicit
   Bad: `var index = 1`
   Good: `int index = 1`
   Bad: `lastSaveError = false`
   Good: `bool lastSaveError = false`
   Bad: `List<GameObject> obs`
   Good: `List<GameObject> objectsInScene`

# Spacing
- Prefer spaces over tabs

# Comments
- Never add comments in `csproj`
- NEVER add tracking comments (`same line — just moved`, `was private`)
- Comment to explain complex logic, or reference external classes/methods
- Short comments (1-2 lines), telegraphic, no filler words or parentheticals
- No comments for self-explaining, well-named methods/variables

# Harmony
- All patches in single `HarmonyPatches.cs`
- Patching always manual (`harmony.Patch()`)
- Explicit parameter names when used (`harmony.Patch(original: X, prefix: Y)`)
- Wrap all patches together in single try/catch, defer errors to `ModPlugin.LogError`

# Localization
- Never hardcode user-facing text. Create key in `English.json`, ask for APPROVAL, translate to other localizations
- If changed name of method/variable which has translation, update `English.json` key, ask for APPROVAL, apply to other localizations
- Prefer real world language-specific style and conventions. Ask user if several options
