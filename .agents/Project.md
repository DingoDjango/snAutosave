# Mod project
- Subnautica project: `snAutosave/Subnautica/StorageInfo.csproj`
- Below Zero ("BZ") project: `snAutosave/Below Zero/StorageInfo_BZ.csproj`
**IGNORE BZ FOLDER - only working on Subnautica**

# Build
- Build command: `dotnet build`
- Use argument `--no-restore` (avoid NuGet fetching)
- Don't add postbuild arguments to build command (automatic from csproj)
- BepInEx is build output directory (don't modify contents, overwritten on build)
