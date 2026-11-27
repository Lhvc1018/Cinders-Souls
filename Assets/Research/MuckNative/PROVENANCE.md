# Native Muck boss visuals

Source: local Muck installation, E:/SteamLibrary/steamapps/common/Muck/Muck_Data.
Extraction: AssetRipper 2.0.0; original export retained at D:/67/MuckAnimationResearch/Export.
Original Muck meshes, skeletons, materials and generic transform animations retain their source authorship.
No recovered Muck scripts are loaded into this project.

Runtime copies:
- Gronk -> Duende Mutante
- Chief -> Lanza del Infierno
- Guardian -> Gurdi del Abismo

Native rigs are preserved. Runtime wrappers normalize height and use URP material equivalents.
Controllers map 16 original clips to the existing Cinders Souls presentation API.
Gronk Attack2_hogoblin is empty in the original data and is excluded.
Death uses the project's procedural CorpseCollapse; no native death clip was recovered.
Hit and stun hold the native idle pose. These are not extracted hit/death animations.
Encounter AI, telegraphs, damage and runtime VFX are Cinders Souls code, not copied Muck combat logic.
