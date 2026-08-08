# Subnautica Autosave + Cyclops Docking Compatibility Patch

Ensures that CyclopsDocking-Continued routes and base parts are correctly saved and persisted when using the Subnautica Autosave mod.

## Overview
By default, CyclopsDocking-Continued hooks into the standard manual save process. Because Subnautica Autosave utilizes a different internal save path (`SaveGameAsync`), CyclopsDocking data was bypassed during autosaves, leading to loss of autopilot routes and base part configurations upon loading an autosave.

This patch redirects the CyclopsDocking save logic to the universal save path, ensuring data persistence across all save types (manual, autosave, and permadeath quits).
