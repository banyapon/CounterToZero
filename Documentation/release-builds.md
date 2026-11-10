# PC and itch.io builds

In Unity, leave Play mode and choose **Count To Zero > Build > PC and itch.zip**.
Individual PC and WebGL commands are available in the same menu.

- Windows download ZIP: `Builds/CountToZero-PC.zip` (all required files included).
- Windows executable: `Builds/PC/CountToZero.exe`. Distribute the entire `Builds/PC` folder, including its data folder and DLLs.
- Browser upload: `Builds/itch.zip`. Upload this ZIP to itch.io as an HTML game and enable **This file will be played in the browser**.
- Progress and result: `Logs/release-build.txt`; full diagnostics: `Logs/Editor.log`.

The command uses the enabled shared scene list, switches to the standard platform profile, and performs an incremental release build. Platform switches can take several minutes. It leaves the last built platform active. PC starts in native-resolution borderless fullscreen. WebGL uses the project PWA template, a viewport-filling canvas, a FULLSCREEN button, a fullscreen manifest and a service worker. Build filenames are hashed to keep the PWA cache current. WebGL uses gzip with decompression fallback and no threads; the ZIP has `index.html` at its root. Serve the WebGL folder over HTTP to test it locally.

## Fixed build blockers

`GUI/Text Shader` was explicitly listed in Always Included Shaders despite being an editor resource marked `HideFlags.DontSave`. This caused `m_LockCount == 0` and failure to write `Library/PlayerDataCache/Win642/Data/Resources/unity_builtin_extra`. The invalid entry was removed; the release command also prevents it from being included again.

The default cursor texture must be readable, RGBA32, without mipmaps, and have alpha transparency enabled. Its Standalone/WebGL import overrides now meet those requirements while preserving its sprite use.

Unity reference: https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/webgl/building-distribution/deploying

## Previous build validation (2026-11-09)

- Unity 6000.6.7f1: Windows x64 and WebGL builds both succeeded with zero errors.
- PC headless startup stayed running without startup exceptions; all six scenes were included.
- `itch.zip`: 18 entries, 23,078,447 bytes; archive CRC check passed and `index.html` is at the root.
- Local HTTP requests for HTML, loader, data, framework, wasm, favicon and CSS returned 200. All three gzip payloads decompressed successfully; the wasm header is valid.
- Interactive browser gameplay and upload to itch.io have not been tested; the browser automation host was unavailable.

## Font replacement and screenshots (2026-11-10)

144 body text components now use Pixel UI Sci-Fi TMP. The body font-size multiplier is (31/90)/(7/8) = 0.3936508, matching the previous cap height. Runtime-generated body labels use the same conversion. 17 title text components also use Pixel UI Sci-Fi TMP at a 1.142857 multiplier to preserve title cap height. Retired Iannnnn source and generated font files are preserved under `Backup/RetiredIannnnnFont`, outside Assets; scene dependencies contain neither.

The user also approved replacing title text. The current Title SDF source GUID resolved to GravityBold8 (the older credits still listed ZF #2ndPixelus). Both the generated Title SDF and the unused ZF source are archived under `Backup/RetiredZFFont`. No Body SDF, Title SDF, Iannnnn or ZF font remains in scene dependencies.

14 distinct live Play mode screenshots: `Snapshot screen/20261110-093147`, each 3158 x 1776, two per level (1, 5, 10, 15, 20, 25, 30). Capture used scripted movement, shooting and healing; score submission was disabled and the user's campaign save restored. All captures report Playing and no runtime errors. Contact sheet: `Documentation/release-snapshots-preview.jpg`.

## Final validation (2026-11-10, after title replacement)

- Unity Windows x64 and WebGL: Succeeded, 0 errors each.
- PC ZIP: 217 entries, 46,583,434 bytes, all 6 scenes, debug backup folder excluded; CRC check passed. Final headless startup logged no exception.
- itch ZIP: 19 entries, 23,064,836 bytes; CRC check passed and all referenced web files match the final build.
- PWA manifest display is fullscreen; fullscreen button uses Unity SetFullscreen; service worker cache key includes both code and data hashes. Service worker JavaScript syntax check passed.
- 10 web resources returned HTTP 200 with matching content. All compressed payloads decompressed, and wasm magic bytes were valid.
- The final 14 screenshots are unique, 3158 x 1776, captured in Playing state with no runtime error. Original campaign save restored.
- No live itch.io upload or interactive browser gameplay test was performed.

SHA256:
- itch.zip: `338173f2e25f51c61d7c47e4ab0ca3681a7b7e9206373796b86882aeb71bd0ba`
- CountToZero-PC.zip: `471709816b0d25bd5036b073a45701845b626742447377b2a67a03502599ec77`
