# Wrapping
- Wrap debug code `#if DEBUG` + `#endif`
- Error checking never wrapped (null checks, try/catch exceptions)

# In-progress debug
- Temporary debug code wrapped in comment `[DEBUG-TEMP]`
- NEVER fetch DLL bytecode. Use source code. Ask user for source folder if missing
