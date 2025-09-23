# **Subnautica Autosave**

### **Description:**

An automated save system for Subnautica. Autosaves are separate from normal saves by default. Includes mod options.

**For Below Zero instructions please view the [Nexus mod page](https://www.nexusmods.com/subnauticabelowzero/mods/359).**

### **Installation:**

1. Install [BepInEx for Subnautica](https://www.nexusmods.com/subnautica/mods/1108)
2. Install [Nautilus](https://www.nexusmods.com/subnautica/mods/1262)
3. Download the latest zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/237/?tab=files)
4. Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)

### **(Optional) Configuration:**

#### Recommended - Using Configuration Manager

1. Install the [Configuration Manager](https://www.nexusmods.com/subnautica/mods/1112) mod (also works for BZ)
2. Launch Subnautica and open the Configuration Manager (default key: F5)
3. Configure desired settings in this mod's section

#### Manual Configuration

1. Launch the game at least once after installing the mod
2. Open *...\Subnautica\BepInEx\config\Dingo.SN.SubnauticaAutosave.cfg* with a text editor
3. Replace the default values with your preferences. Read setting descriptions before changing values.
    > For hotkeys, use KeyCode names found on [this page](https://docs.unity3d.com/ScriptReference/KeyCode.html)

### **(Optional) Translation:**

1. Navigate to *...\Subnautica\BepInEx\plugins\SubnauticaAutosave\Localization*
2. Copy *English.json* and change the file name to match your language
    > Valid language names are found in *...\Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles*
3. Translate the file. Do not touch the keys ("AutosaveStarting"), only the values ("Autosave sequence...")
4. Share the file with me on GitHub or in a Nexus private message

### **FAQ:**

- **Q. Does this mod support the latest Subnautica update?**
- A. Latest version tested on September 2025
- **Q. Is this mod safe to add or remove from an existing save?**
- A. Should be safe. Please report any issues
- **Q. Does this mod have any known conflicts?**
- A. Do not use this mod with other autosave/quicksave mods. Other known incompatibilities: Cyclops Docking, Map, Deathrun
- **Q. Does this mod impact performance?**
- A. Autosaves are created in a similar way to vanilla saves. I implemented one "dirty hack" for copying screenshots, but that setting can be toggled off if your PC is too slow

[Source code can be found here.](https://github.com/DingoDjango/snAutosave)

### **Credits:**

- Powered by [Harmony](https://github.com/pardeike/Harmony)
- Code & updates by [MrPurple6411](https://github.com/MrPurple6411), [Bisa](https://github.com/Bisa)
- Translations by [DJDosKiller](https://www.nexusmods.com/users/3737367), [ZiiMiller](https://www.nexusmods.com/users/30791070), [NelttjeN](https://www.nexusmods.com/users/53071371), [obgr](https://github.com/obgr), [Yanuut](https://github.com/Yanuut), [vsx06](https://www.nexusmods.com/users/10667357), [Nodzukav](https://www.nexusmods.com/users/54008122), [Anthuulos](https://www.nexusmods.com/users/116777063), [Amph3](https://www.nexusmods.com/users/140890058), [realmister](https://www.nexusmods.com/users/11833263), [love309099225](https://github.com/love309099225), [2315506431](https://github.com/2315506431)
