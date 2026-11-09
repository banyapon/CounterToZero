# Asset cleanup audit — 2026-11-09

Removed the unused Unity template `Assets/TutorialInfo/` plus its folder meta and `Assets/Readme.asset`/meta after checking GUID references in all remaining Assets and ProjectSettings text assets. No external references were found. A complete copy is stored in `Backup/2026-11-09-before-micro-circuit/UnusedTemplate/`.

The old character folder was copied before edits to `Backup/2026-11-09-before-micro-circuit/man/`. At backup time it contained the four current source PNGs (idle, walk, punch, enemy walk) and their metas. The current folder contains only these required sources and the two new robot variants.

Kept Unity packages, fonts and licenses, shared UI assets, editor build/validation tools, and generated materials referenced by scenes or code. The robot variants are loaded via constructed paths in MicroCircuitUpgrade, so a naive filename/GUID-only orphan scan must not delete them. Native Aseprite sources live outside Assets in ArtSource.
