# Pause / Options fix and gameplay GIF

Reproduced the overlap in Play mode: the runtime CREDITS button overlapped Close Options, and the Pause menu's Options/Title buttons remained visible behind the modal. Baseline: `Documentation/PauseOptions-Before/options.png` and `validation.txt`.

OptionsController now arranges Defaults, Cancel Listen, Credits, Save & Title and Back in one row with space for hover scaling. CampaignExpansionUI registers its Credits button with that layout. While Options is open, external Options, Title and Pause controls are hidden; the modal is raised and uses a solid backing. Closing restores the Options button before restoring keyboard/gamepad focus.

Play-mode validation passed: no overlapping interactable controls; modal focus; Back returns to Pause; Options focus restored; Resume returns to gameplay. Evidence: `Snapshot screen/Gameplay-GIF/options.png` and `validation.txt`.

GIF: `Snapshot screen/Gameplay-GIF/CountToZero-gameplay.gif`, 960 x 540, 120 frames, 10 fps, 12 seconds, looping, 13,385,065 bytes. Captured from live gameplay with scripted movement/shooting/healing; scores disabled and campaign save restored.

## Unity Play

Unity Play accepts zipped Unity WebGL build folders. `Builds/UnityPlay.zip` is a copy of the same PWA build packaged for itch.io, with index.html at its root. This is an upload-ready artifact, not a published game.

Sign in at https://play.unity.com/en/upload, select UnityPlay.zip, set title/description/thumbnail and visibility, then publish. The current build is well below Unity's documented 1 GB upload limit. Browser fullscreen still requires a user gesture (use the FULLSCREEN button); the PWA canvas fills the available viewport.

Official sources:
- https://play.unity.com/en/faq
- https://learn.unity.com/tutorial/getting-started-with-unity-web?publish=true
- https://docs.unity.com/en-us/engine/6000.7/manual/platform-specific/webgl/building-distribution/web-build-settings
