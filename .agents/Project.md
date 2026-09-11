# General
- NEVER modify core `.md` in `.agents` folder unless user explicitly asks
- User instructions always override `.agents`

# Build
- Build command: `dotnet build` with `--no-restore` (avoid NuGet)
- NEVER add your own postbuild arguments to build command
- Never modify output directory (ex. `BepInEx`) contents - overwritten on build
- Build project `.csproj` not `.sln`

# Game
- Subnautica source: `SN_Source\Subnautica_Assembly\Assembly-CSharp`
- Subnautica game: `C:\Program Files (x86)\Steam\steamapps\common\Subnautica`
- BZ Source: `SN_Source\SubnauticaBZ_Assembly\Assembly-CSharp`
- BZ game: `C:\Games\Below Zero`
- NEVER make edits to game source

# Libraries
- Nautilus source: `Nautilus\Nautilus`
- Use Nautilus methods in priority if exist
- NEVER make edits to library source

# Localization
- Nautilus has no lookup API, handled by mod
- `Translation.cs` caches, wraps calls to vanilla/Nautilus
- Missing key in non-English json fallback to `English.json`
- User-facing strings: `Shared\Localization\*.json` (filenames match `C:\Program Files (x86)\Steam\steamapps\common\Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles`)
- `LanguageHandler.RegisterLocalizationFolder()` in `ModPlugin.Awake()` loads `Shared\Localization\`

# Shared files
- `Shared` subfolder used in multiple projects in directory
- Modify original files in `Shared`, NOT output directory
- Example `csproj` link:
  ```xml
  <ItemGroup>
    <None Include="..\Shared\Localization\*.*">
      <Link>Localization\%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  ```
