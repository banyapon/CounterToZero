# Count to Zero

เกม Unity 6 แนว arithmetic bullet hell ตามเอกสาร Lucky Number: GLITCH-0 ต่อสู้ OMEGA-99 สามเฟสในสนาม 3D โดยตัวละคร ลูกแก้ว และกระสุนใช้ SpriteRenderer แบบ Billboard Facing Camera

## เปิดเล่น

1. เปิดด้วย Unity **6000.6.7f1**
2. เปิด `Assets/Scenes/CountToZero.unity` แล้วกด Play → Enter
3. หากยังไม่มีฉาก เลือกเมนู **Count To Zero → Create Game Scene**

| ปุ่ม | การทำงาน |
| --- | --- |
| WASD / ลูกศร | เดินบนระนาบ XZ |
| เมาส์ + คลิกซ้ายค้าง | เล็งและยิง; ยิงลูกแก้ว + / - เพื่อกลับเครื่องหมาย |
| J ค้าง | ยิงไปหาบอสโดยไม่ใช้เมาส์ |
| Space | Dash พร้อมกลับเครื่องหมายค่าตัวเอง, cooldown 3 วินาที |
| E | Bullet Time 3 วินาที เมื่อเกจจาก Grazing เต็ม |
| Esc | หยุด / เล่นต่อ |
| Enter / R | เล่นใหม่หลังจบเกม |

## กติกา

- ค่าเลขของผู้เล่นและบอสแยกจากพลังชีวิต: ผู้เล่น 5 HP; บอส 99 HP แบ่งสามเฟสที่ 66/33/0
- กฎเปลี่ยนทุก 15 วินาที: exact, even, odd, multiple, prime, greater/less than
- Exact Match ปล่อยเลเซอร์ ล้างกระสุน และเปลี่ยนกฎทันที; conditional match ให้กระสุนติดตาม 5 วินาที
- บอสทำ exact match จะหัก HP โดยตรง; conditional match เปิดเกราะสะท้อนและเพิ่มความเร็วกระสุนใหม่ 50% เป็นเวลา 5 วินาที
- Zero ทำให้ผู้เล่นหยุด 1.5 วินาที หรือบอส crash 5 วินาทีและรับดาเมจสองเท่า; มีลูกแก้วฟื้นฟู/คะแนน เว้นแต่กฎเป็น exact zero
- เกินช่วง -99..99 เกิด overflow: ผู้เล่นเสีย 1 HP แล้วกลับเป็น 1; บอสเสีย 5% HP สูงสุดแล้วกลับเป็น 50; ทิ้งเขตอันตราย 4 วินาที
- หารจำนวนเต็มตัดเศษเข้าหาศูนย์; modulo เก็บเครื่องหมายเดิม เช่น -17 % 10 = -7
- ประเมิน match หลังการคำนวณเท่านั้น การ reset และการเปลี่ยนกฎไม่ trigger ซ้ำเอง
- ทุกลูกที่ห้าของการเกิดแบบต่อเนื่องจะช่วยเข้าใกล้กฎ ณ เวลาที่เกิด พร้อมวางในระยะเข้าถึงได้; ค่าตัวเองหรือกฎที่เปลี่ยนหลังจากนั้นอาจเปลี่ยนประโยชน์ของลูกนั้น
- ยิงลูกแก้วที่ถูกบอสดูดหรือ Jinx เพื่อทำลาย; แย่งลูกแก้วที่ทำให้บอส match ได้ Boss Deny
- สะสมคะแนนจาก match, deny, graze, speed match และ no-hit phase; เก็บ best score ใน PlayerPrefs

## โครงสร้าง

`Assets/Scripts/Core` กฎ pure C#, คะแนน, game state; `Entities` ตัวละคร; `Combat` pool กระสุน/ลูกแก้ว; `Visuals` procedural sprites และ billboard; `UI` HUD และ feedback

เกมสร้างสนามและภาพ pixel sprite ภายในเครื่องตอนเริ่ม ไม่ต้องดาวน์โหลด asset เพิ่ม กล้อง orthographic มุมเฉียง เคลื่อนที่/ชนกันบน XZ ด้วย swept segment checks สำหรับกระสุน และหมุนเฉพาะวัตถุภาพเข้าหากล้องใน LateUpdate

เมนู **Count To Zero → Validate Arithmetic** ตรวจเลขติดลบ ขอบเขต zero/overflow คะแนน และ fairness ทุกค่าระหว่าง -99 ถึง 99 ทุกกฎ ผลอยู่ใน `Documentation/arithmetic-validation.txt`

เมนู **Count To Zero → Run Play Checks** ทดสอบ match, crash, overflow, เฟส, ชนะ/แพ้, pause, pool และ billboard ใน Play Mode แล้วหยุดเกมไว้ที่หน้า Pause ผลอยู่ใน `Documentation/play-validation.txt` การทดสอบนี้จะเริ่มรอบใหม่

ขอบเขตเวอร์ชันนี้: ต้นแบบเล่นได้สำหรับ desktop, HUD ภาษาอังกฤษ, ภาพ/เสียง procedural และค่าปรับสมดุลอยู่ในสคริปต์ ยังไม่มีงานภาพสำเร็จรูป จอย หรือระบบ localization


## Gameplay expansion

- **Controls:** WASD + ?????, Mouse LMB ????/???, J ????????, Space Dash, E Bullet Time, Escape Pause
- **Options:** ???? action ???????????????? (10 ??????), ????????????????, Defaults ?????????????; ?????????? PlayerPrefs ?????????/????????????? ??????????????????????
- **Gamepad:** Left stick / D-pad ????, Right stick ????, RT/RB ???, South/A Dash, West/X Bullet Time, Start Pause; ??????? D-pad + South/A ?????? ???? Gamepad ???? mapping ?????
- **Mobile WebGL:** ???? mobile browser ???? `Assets/Plugins/WebGL/MobileBrowser.jslib` ??????? Fixed Joystick ??? Joystick Pack ????? FIRE ?????????, DASH, FOCUS; ?? touch ??????????????????? 6 ????????? ?????????????????????????? ???? `Show Touch In Editor` ?? Player Input ???????? UI
- **Pause:** ??????????/????/Gamepad ????????????????????? focus; Options ???????????????????? Pause ???????????
- **Camera:** FollowCamera ???? Orthographic ?????????????????????? ?? offset ??????????????????; ???? offset, followAmount, size ?? Inspector
- **Dialogue:** `Assets/GameAssets/Dialogues/en.json` ???? speaker, text, duration ??? id; ?????????????????????????????
- **Lucky action:** Canvas `Lucky Action Cut-in` ???????????? ??? 30% ???????????? ???? 0.8 ?????? ????? raycast ??????? ??? sprite ???????????????????????? energy strike ?????????????
- **Boss:** 6 ScriptableObject patterns ?? `Assets/GameAssets/Patterns` ? ring, fan, spiral, cross, wave, broken ring; ?????????? sway, pursue, orbit, sweep, figure-eight ?????????? 7 ?????? ?????????????????????
- **Encounters:** ???????????????? 9 ?????? ???????????? Cube ??? Cube ?????????; ????????? 1.5 ?????????? cube ????/??????, ????????????/Dash/??????, ?????????? 6?8 ??????; ????????? startup 1 ?????? ????????????? ????????????????
- **Health:** Repair Item +HP ???????? 18 ?????? (????????????? 16) ????????????????????????? ???? 1 HP ?????? 5; ????????????????????????? ?????? 24 ??????

???????? UI, encounter pools, spawn points ??? reference ??????????? Hierarchy; `Count To Zero > Upgrade Gameplay` ?????????????? Editor ??????????????????? base scene ???????? ????????????????????

`Run Advanced Gameplay Checks` ???? key listening, virtual gamepad, camera, cut-in, obstacle collision, minions ??? healing ???????? `Documentation/advanced-gameplay-validation.txt` ??????????????????????


## Campaign and camera

Start from `Assets/Scenes/Title.unity` (first scene in Build Settings).
The authored flow is Title -> Preload -> Story (Next / Skip) -> CountToZero -> TurnEnd.
The Preload slider reports actual `LoadSceneAsync` progress, normalized to activation.
Scene UI and references are serialized; gameplay does not construct these screens.

- 100 story levels, five blocks of 20. Each level has one of 20 distinct shot patterns and three boss health phases.
- Pattern assets: `Assets/GameAssets/Patterns`; tune movement, interval and speed in Inspector.
- After each block, difficulty adapts to clear time, damage and failed attempts. Shot cadence and speed scale within 0.75-2.5x difficulty bounds.
- Progress, score and block performance save locally. Continue resumes the next uncleared level; loss retries the same level.
- Level 100 leads to StoryEnd. Skipping still shows the explicit Endless / Title choice.
- Endless starts at level 101, cycles 20 patterns and continues block adaptation.
- Story text: `Assets/GameAssets/Dialogues/story_en.json`.

Camera returns to the original centered `(0, 21, -18)` offset without sideways yaw or roll.
A six-second intro orbits the arena, moves in around the player, then settles at the gameplay camera.
Combat is suspended during the intro. Mouse wheel and Ctrl+Up/Down zoom during gameplay; Ctrl+arrows do not move the player.
Zoom limits, follow strength, offset and intro duration are editable on Arena Camera / FollowCamera.

Editor: `Count To Zero / Build Campaign Scenes` authors missing scenes and wires 20 patterns.
`Run Campaign Checks` validates progression, saves, UI raycasts, scene loads, intro, zoom, retry, ending and Endless.


## Typography and HP bars

All game scenes and number-orb labels use TextMeshPro. Body text uses `Assets/Fonts/Body SDF.asset` generated from `2005_iannnnnAMD.ttf`; headings use `Title SDF.asset` generated from `ZF#2ndPixelus.ttf`. Both include bold styling and dark outlines. Source fonts remain embedded for dynamic glyph support.

Player, boss and all six pooled minions have authored world-space `HP Bar` children with current/max HP. The player also has an HP bar in the HUD, and the boss HUD bar is thicker with larger numbers. Boss arithmetic value is labeled VALUE separately. Edit dimensions, colors and font sizes directly in Hierarchy/Inspector.

`Count To Zero / Validate TMP and HP Bars` checks the six scenes, text references, fonts, orb prefab and damage/healing updates.


## First minute, pacing and audio

New campaigns begin with a guided safe training sequence: movement, collect +6 to make 1 + 6 = 7, fire three shots, dash/sign inversion, and 12 seconds of slow dodge practice. Training freezes the live rule/combat clock, suppresses normal boss/encounter/orb spawning and prevents HP loss. It ends with a fresh live round. Progress is saved; training can be skipped. English instructions live in `Assets/GameAssets/Dialogues/onboarding_en.json` and display the current keyboard bindings.

The six-second camera tour plays once per campaign, with mouse/touch Skip, Enter/Space or gamepad A. Later levels and retries go directly to gameplay. Defeat stays in the current scene: Enter, R or the Retry button restarts immediately, and the result shows the cause of the last hit. Title is available from pause/defeat and Options. Gameplay disables UI navigation so the movement stick and dash button cannot accidentally operate menus.

Lucky cut-ins protect the player from all damage for their full visible duration plus 0.25 seconds. Pausing preserves that protection. After each 20-level block, adaptive difficulty changes by no more than +/-0.12x and the before/after value is shown.

`Game Audio / Persistent Mix` is authored in every scene; a single instance persists across scene loads. Nine distinct synth cues and three synchronized 96 BPM music stems react to training, combat, phase and pause. Music and SFX have separate saved sliders in Options. Audio assets are generated offline by `Tools/generate_polish_audio.py` (original synthesis, no external samples), not constructed at runtime. Do not run generation during gameplay.

Use `Count To Zero / Validate First Minute and Retry` for automated checks. Human test protocol: `Documentation/first-playtest.md`; blank results sheet: `Documentation/playtest-results.csv`. Automated checks do not establish fun or replace testing with new players or real mobile WebGL devices.
