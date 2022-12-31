# **Subnautica Autosave**

### **Description:**

An automated save system for Subnautica. Autosaves are separate from normal saves by default. Includes mod options for everything.

### **Installation:**

1. Install [BepInEx for Subnautica](https://www.nexusmods.com/subnautica/mods/1108)
2. Download the latest zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/237/?tab=files)
3. Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)

### **(Optional) Configuration:**

#### Recommended - Using Configuration Manager

1. Install the [Configuration Manager](https://www.nexusmods.com/subnautica/mods/1112) mod
2. Launch Subnautica and open the Configuration Manager (default key: F5)
3. Configure desired settings in this mod's section

#### Manual Configuration

1. Launch the game at least once after installing the mod
2. Open *...\Subnautica\BepInEx\config\Dingo.SN.SubnauticaAutosave.cfg* with a text editor
3. Replace the default values with your preferences
	- "Autosaves Profile" -- Separate autosaves by profile, for different playthroughs (Value: 1-5)
    - "Autosave Using Time Intervals" -- Autosave every X seconds as defined in the mod settings
	- "Autosave On Sleep" -- Autosave when the player goes to sleep
	- "Seconds Between Autosaves" -- Time to wait between autosave attempts (Value: at least 120)
    - "Maximum Autosave Slots"  -- Total autosave slots (Value: at least 1)
    - "Minimum Player Health Percent"  -- Autosaves will not occur if player health is below this percent (Value: 0-1.0, disabled if 0)
    - "Hardcore Mode"  -- If true, autosaves will override the normal save slot instead of using separate slots
	- "Quicksave Hotkey" -- Keybinding used to quickly save the game manually	
    > For hotkeys, use KeyCode names found on [this page](https://docs.unity3d.com/ScriptReference/KeyCode.html)

### **(Optional) Translation:**

1. Navigate to *...\Subnautica\BepInEx\plugins\SubnauticaAutosave\Languages*
2. Copy *English.json* and change the file name to match your language
    > Valid language names are found in *...\Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles*
3. Translate the file. Do not touch the keys ("AutosaveStarting"), only the values ("Autosave sequence...")
4. Share the file with me on GitHub or in a Nexus private message

### **FAQ:**

- **Q. Does this mod support the latest Subnautica update?**
- A. Tested on Subnautica version Dec-2022 71137 (Living Large update)
- **Q. Is this mod safe to add or remove from an existing save?**
- A. Should be safe, please report any issues
- **Q. Does this mod have any known conflicts?**
- A. I would not use this mod with [Safe Autosave](https://www.nexusmods.com/subnautica/mods/94)
- **Q. Does this mod impact performance?**
- A. Autosaves are created with vanilla code. Gameplay may "freeze" momentarily when saving

[Source code can be found here.](https://github.com/DingoDjango/snAutosave)﻿

### **Credits:**

- Powered by [Harmony](https://github.com/pardeike/Harmony)
- Code & updates by [MrPurple6411](https://github.com/MrPurple6411), [Bisa](https://github.com/Bisa)
- Translations by [DJDosKiller](https://www.nexusmods.com/users/3737367), [ZiiMiller](https://www.nexusmods.com/users/30791070), [NelttjeN](https://www.nexusmods.com/users/53071371), [obgr](https://github.com/obgr), [Yanuut](https://github.com/Yanuut), [vsx06](https://www.nexusmods.com/users/10667357), [Nodzukav](https://www.nexusmods.com/users/54008122), [Anthuulos](https://www.nexusmods.com/users/116777063), [Amph3](https://www.nexusmods.com/users/140890058), [realmister](https://www.nexusmods.com/users/11833263), [love309099225](https://github.com/love309099225), [2315506431](https://github.com/2315506431)
