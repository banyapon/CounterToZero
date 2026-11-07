# First-player playtest ? Count to Zero

Status: protocol prepared; no human sessions have been run or results invented.

## Setup

Recruit 3?5 people who have not watched development. Include someone who rarely plays bullet hell, and at least one mobile browser user when a WebGL build is available. Use the same build for every participant and record its date/version. Ask permission before recording video/audio. Do not collect names or contact details in the results sheet; use P01?P05.

Allow 8?10 minutes per person. Start from Title > New Game so the guided training runs. Let the participant choose their input device. Keep music/SFX at default volume initially.

## Observe without teaching

1. Say only: ?Try this game for five minutes. You can stop whenever you want.?
2. Do not explain Lucky rules, controls or the meaning of the numbers. Record time to first movement and first Lucky.
3. Observe whether they intentionally collect +6, understand the reset to 1, and distinguish VALUE from HP.
4. Watch shooting, dash, the dodge practice, and the first live round. Record hesitation, wrong inputs, obscured bullets or unreadable instructions.
5. At the first death, let them decide what to do. Record whether they can explain the cause and find Retry without help.
6. Do not require all 100 levels. Judge the opening loop before testing long-term progression.

If stuck for 45 seconds, ask ?What do you think you need to do now?? Record that help was needed before giving a hint. Stop if camera motion or flashing causes discomfort.

## Ask after play

- What makes a Lucky happen? What is the difference between VALUE and HP?
- Did you plan a Lucky, or did it seem accidental?
- Why did you lose HP or die? Was any hit unfair?
- Which sounds meant danger, reward and damage? Was music too loud?
- Was anything hard to read or hidden by the cut-in?
- Would you play another round? What is the first thing you would change?
- Rate enjoyment 1?5, then explain the rating. Do not prompt for a high score.

## Initial acceptance targets (not measured results)

- At least 4 of 5 players can explain Lucky and distinguish VALUE/HP after the tutorial.
- At least 4 of 5 complete training within roughly one minute without an explanation.
- At least 3 of 5 voluntarily retry; record their actual choice, not just their answer.
- No damage while the Lucky cut-in obscures play; no accidental menu action from normal gamepad movement/dash.
- Retry immediately restores the live round without another camera tour or loading screen.
- On real mobile WebGL: joystick, simultaneous FIRE/DASH, audio unlock, pause/resume and text visibility work on the itch page.

For only 3 participants, treat observations as directional feedback, not a statistical conclusion. Fix blockers before tuning scores or adding content. Repeat only the affected parts with fresh players when possible.

## Test record

Use `playtest-results.csv`. Empty fields mean ?not tested?, not a pass. Record build, device, input method, observed timings and concrete quotes. Keep the original observations when a fix is made.
