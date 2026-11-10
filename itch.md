# COUNT TO ZERO

**Count to the Lucky Number. Break the machine.**

OMEGA-99, a rogue AI, has taken over the city networks of the world, one lucky number at a time.
GLITCH-0 and the GLITCH SQUAD dive into its circuits. Their only weapon is arithmetic: build your number
up to the Lucky Number, and every hit becomes a laser.

*Made for the Official Unity Game Jam 2026. Theme: **Lucky Number**.*

---

## How to play (30-second version)

1. **Your number starts at 1.** It's shown above your head and in the bottom-left corner.
2. **The gold box (top-left) shows the Lucky Number**, e.g. `EXACTLY 7`.
3. **Walk into number orbs to count.** `+3` adds 3, `-2` subtracts 2.
4. **Hit the Lucky Number exactly** → **LUCKY 7!** You fire a laser that clears enemy bullets and blasts OMEGA-99.
5. Your number resets to 1. A new Lucky Number appears. Count again.

> Example: Lucky Number 7. You are 1. Grab `+2` (now 3), then `+4` (now 7) → **LUCKY 7!**

Level 1 teaches exactly that and nothing else. New mechanics unlock one at a time as you travel:

| Level | What's new |
|---|---|
| 1 | One rule: count to the exact Lucky Number. Only `+` and `-` orbs. |
| 2 | Even / Odd rules, `x` and `/` orbs, Sentinel enemies, helper allies |
| 3 | All rules (multiple of, prime, greater / less than), Jinx orbs, falling blocks |
| 4+ | Tracking lightning, sector hazards, new boss forms |

## World tour

Every stage is set in a real city and opens on that place's lucky number:

| Stop | Lucky Number | Why it's lucky |
|---|---|---|
| Las Vegas, USA | 7 | Lucky 7 pays out on every slot machine |
| Bangkok, Thailand | 9 | "Kao" (nine) sounds like *stepping forward* |
| Shanghai, China | 8 | "Ba" sounds like *fa*: fortune |
| Rome, Italy | 13 | *Fare tredici*: hitting the jackpot |
| Dublin, Ireland | 4 | The four-leaf clover |
| Hong Kong | 6 | Sounds like *flowing smoothly* |
| Tokyo, Japan | 8 | The kanji 八 opens like a fan into the future |
| Berlin, Germany | 3 | *Aller guten Dinge sind drei* |
| Osaka, Japan | 7 | The Seven Lucky Gods |
| Hanoi, Vietnam | 9 | Long life and lasting luck |
| Singapore | 88 | Double fortune |
| Beijing, China | 99 | "Jiu jiu", *forever*: the final core |

## Controls

| Action | Keyboard / Mouse | Gamepad | Touch |
|---|---|---|---|
| Move | WASD / Arrow keys | Left stick / D-pad | Joystick |
| Aim + fire | Mouse + hold Left Click (or hold **J** to auto-aim) | Right stick + RT / RB | FIRE |
| Dash (flips your sign: +3 → -3) | Space | A / South | DASH |
| Bullet Time (when the graze meter is full) | E | X / West | FOCUS |
| Pause | Esc | Start | Pause button |
| Skip intro / training | Enter / Space / Tab | A / Select | SKIP |

All keys can be rebound in **Options**.

## Rules worth knowing

- **Shoot an orb to flip its sign.** `+5` becomes `-5`. Use it when you need to count down.
- **Dash flips YOUR sign.** Being at `-7` when the rule is `EXACTLY 7`? Dash.
- **OMEGA-99 counts too.** Its number is shown next to its HP bar, and it pulls orbs toward itself. If it hits the Lucky Number first, it counter-attacks. Shoot the orbs it is pulling to deny it.
- **Zero = crash.** If you hit 0 you are stunned for a moment. If OMEGA-99 hits 0, it crashes and takes double damage.
- **Overflow.** Going past ±99 hurts you and resets you to 1.
- **The rule changes every 15 seconds.** The bar under the gold box shows how much time is left.
- **Graze** (fly close past bullets) to charge Bullet Time.
- Clear 30 stages for the ending, then keep going in Endless mode with the cloud leaderboard.

## Tips

- You don't need to aim at the boss to win. Counting is your main attack. Shooting is for flipping orbs.
- Every 5th orb that spawns near you is a helpful one that moves you toward the current rule.
- Small numbers are safe. If you drift above 50, look for `%10` or `=1` orbs.

---

## Credits

**Design, programming, pixel art:** DayDev (Banyapon Poolsawas)

### Art

All character, boss, portrait, cutscene and UI artwork was **drawn by hand in Aseprite** from reference images.
The Aseprite source files are included in the project (`ArtSource/`).

| Asset | Source |
|---|---|
| GLITCH-0 hero, robots, assistants (gold / green), twin portraits, Lucky Charge portrait | Original pixel art, Aseprite |
| Comic cutscenes 1–4, title artwork | Original pixel art, Aseprite |
| OMEGA-99 boss forms, serpent segments, chain / flail / fireball / shock sprites | Original pixel art, Aseprite |
| Arena, circuit borders, particles, number orbs | Made in the project (procedural + Aseprite) |
| City skyline backdrop ("City Landscape V2": Sky, Buildings 1–4, Shade, Tiny Cloud) | **[creator + link: please fill in]** |
| Controller button icons (gdb-keyboard / xbox / playstation / switch / gamepad) | **[creator + link: please fill in]** |
| Pixel UI Sci-Fi Free: HUD Kit Sampler 1.0.4 | heyheythere: https://itch.io/profile/heyheythere (Unity Asset Store #412096) |
| Joystick Pack 2.1 | Fenerax Studios: https://assetstore.unity.com/packages/tools/input-management/joystick-pack-107631 |

### Fonts

| Font | Author / License |
|---|---|
| Pixel UI Sci-Fi TMP (main UI font) | HeyHeyThere / Pixel UI Sci-Fi Free |
| GravityBold8 | John Watson |
| Liberation Sans | Steve Matteson / Ascender; Google, Red Hat. SIL OFL 1.1 |

### Audio

| Asset | Source |
|---|---|
| Music: title.mp3, story.mp3, bgm.mp3 | **[composer + link: please fill in]** |
| SFX: Choose, wobble, Explosion, GameOver, Hit, Lucky, Pickup | **[creator + link: please fill in]** |
| Polish SFX + music beds (Dash, Death, Heal, Shoot, Warning, Phase, Countdown, Ready, Music Bed / Pulse / Danger) | Synthesized in code for this game (`Tools/generate_polish_audio.py`) |
| Blocked hit cue | Synthesized in code (`Tools/generate_blocked_audio.py`) |
| **Lucky Jackpot** exact-match cue | Synthesized in code (`Tools/generate_jackpot_audio.py`) |

### Tools & technology

Unity 6 (6000.6), Universal Render Pipeline, Input System, TextMesh Pro, Unity Gaming Services (Authentication + Leaderboards). Aseprite (Igara Studio).

### Generative AI disclosure

- **Art:** no generative AI. All artwork was drawn by hand in Aseprite.
- **Code and text:** **[please confirm and edit]** If an AI coding assistant helped write code, tutorial text or dialogue, describe it here, e.g. *"Parts of the C# code, tutorial text and world-tour dialogue were written with an AI coding assistant (Claude Code) and then reviewed and edited by the developer."* The jam rules require disclosure for **any** part of the submission.
