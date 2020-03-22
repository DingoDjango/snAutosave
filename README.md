## **Subnautica Autosave by Dingo**

#### **Description:**  
An automated save system which saves in time intervals. The autosave slots are separate from the normal save by default, but can be configured otherwise.  
You can define several custom parameters in the settings file (see the Configuration section).

#### **Installation:**  
1) Install [QMods](https://www.nexusmods.com/subnautica/mods/201)﻿ if you haven't already  
2) Download the zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/237/?tab=files)  
3) Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)  

#### **(Optional) Configuration:**  
1) Navigate to the mod's directory (*Subnautica\QMods\SubnauticaAutosave*).  
2) Edit settings.json with Notepad or your favourite text editor.  
3) Define custom values according to your preference. The settings available are -  
   *  "SecondsBetweenAutosaves": 900 -- The time (in seconds) between autosave attempts. Must be at least 120.  
   *  "MaxSaveFiles": 3  -- The maximum amount of autosave slots. Must be at least 1.  
   *  "MinimumPlayerHealthPercent": 25 -- If player health is below this percent, no save will occur. Change to 0 to disable this option.  
   *  "HardcoreMode": false -- If true, autosaves will override the normal save slot instead of using separate slots.  

#### **(Optional) Translation:**  
If you want to contribute a translation for this mod, please follow these steps:  
1) Look at the file *"English.json"* in *QMods\SubnauticaAutosave\Languages*  
2) Copy that file and change the file name to your language. It needs to match the file name in *Subnautica\SNUnmanagedData\LanguageFiles*  
3) Translate the file. Do not touch the keys ("AutosaveStarting"), only the translated values ("Autosave sequence...")  
4) Share the file with me, preferrably over GitHub, a Nexus private message or (if you must) a comment  

#### **FAQ:**  
* **Q. Is this mod safe to add or remove from an existing save file?**
* A. Perfectly safe.
* **Q. Does this mod have any known conflicts?**
* A. I don't think so, but I would not use this mod with [Safe Autosave](https://www.nexusmods.com/subnautica/mods/94) due to redundancy.
* **Q. Does this mod impact performance?**
* A. Autosaves are stored using the same code as regular saves. Gameplay will "freeze" for a few seconds when saving and then continue normally.

[Source code can be found here.](https://github.com/DingoDjango/snAutosave)﻿  

#### **Credits:**  
Powered by [Harmony](https://github.com/pardeike/Harmony)  
Made for the [QMods Subnautica Mod System](https://www.nexusmods.com/subnautica/mods/201)﻿  
Unity 2019 update by [MrPurple6411](https://github.com/MrPurple6411)  
German translation + feedback - [DJDosKiller](https://www.nexusmods.com/users/3737367)  
Russian translation - [ZiiMiller](https://www.nexusmods.com/users/30791070) & [NelttjeN](https://www.nexusmods.com/users/53071371)  
Swedish translation - [obgr](https://github.com/obgr)  
French translation - [Yanuut](https://github.com/Yanuut)  
Polish translation - [vsx06](https://www.nexusmods.com/users/10667357)  
Turkish translation - [Nodzukav](https://www.nexusmods.com/users/54008122)  

2020-03-22  
* Turkish translation by Nodzukav  

2020-03-01 - v1.3.5  
* New version for QMods 3.0 & latest Subnautica patch by MrPurple6411  
* French translation by Yanuut  
* Polish translation by vsx06  
* Updated Russian translation by NelttjeN  

2019-07-12  
* Uploaded Swedish translation by obgr  

2019-06-06  
* Uploaded Russian translation by ZiiMiller  

2019-04-27  
* Uploaded German translation by DJDosKiller  

2019-04-16 - v1.2.0  
* Improved save slot rotation method  
* Added screenshots copying routine  
* Fixed compatibility with the Map mod and possibly other mods  

2019-04-12 - v1.1.0  
* Added a HardcoreMode setting. If enabled, the main save file is used instead of separate saves  

2019-04-12 - v1.0.2  
* Small bug fix  

2019-04-12 - v1.0.1  
* Initial release
