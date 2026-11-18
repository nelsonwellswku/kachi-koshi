# Kachi-koshi

A QWOP-style 3D sumo game built in C# with Raylib-cs. The player controls individual limbs against a computer opponent on a dohyō modeled after the one at Ryōgoku Kokugikan in Tokyo.

**Design test:** Plant your left foot, shove with your right palm, and try not to follow your opponent out of the ring. Success should feel intentional, and you should be able to tell why a failed attempt went wrong.

## Prototype decisions

- Use a fixed camera behind the player so left and right stay readable and both feet are visible.
- `Q` / `P`: hold to lift the left / right knee and bring the foot forward; release to lower it and take weight.
- `W` / `O`: hold to drive the left / right palm forward; release to draw the elbow back.
- Use Raylib-cs for input, rendering, and sound. Start with BepuPhysics v2 for fixed-timestep physics. Build wrestlers from boxes and capsules, with strength-limited joint motors driving target poses at the hips, knees, shoulders, and elbows.
- Give the pelvis weak upright assistance. Tune foot grip, motor strength, and balance assistance against the design test before adding controls or character art.

## Milestones

| ID | Deliverable | Playable acceptance check |
| --- | --- | --- |
| M1 | One wrestler, flat practice ring, padded post | From rest, step up, plant a foot, shove the post, and stay standing. Repeat with poor footing and check that the cause of the stumble is visible. |
| M2 | Two wrestlers, opening face-off, loss detection, instant rematch | A hand touching the clay loses; a foot landing outside loses; leaning over the boundary without touching outside does not. Restart the bout and play again. |
| M3 | Computer opponent using the same limb inputs | It shuffles into range, shoves, and plants its feet to recover. Its reaction delay gives the player a chance to exploit an overcommitted shove. |
| M4 | Wrestler meshes and Tokyo tournament setting | Play a bout on a raised square clay platform with a circular straw-bale boundary and two white starting lines. Both feet and the boundary remain readable with the suspended roof, ringside referee, and crowd audio in place. |

## Handoff

**Current state:** M3b complete. After landing a shove, red tries the other palm if blue still looks centered between its shoulders. From the staggered face-off that follow-up can miss wide. A yellow `RED MISSED - COUNTER!` cue lasts through recovery only if the driving palm made no contact during the shove. The existing reach threshold and 0.75-second recovery worked in full-bout replays.

**Launch / try it:** Run `dotnet run` with a .NET 8-compatible SDK and a desktop display. Leave both feet down through red's opening hit and wait for its second shove. On the yellow miss cue, hold `O` for about half a second; look for `PALM CONTACT` with both feet `ON CLAY`. After recovery, try following with short `P` taps and right-palm pressure. Press `R` to replay. `Q` / `P` lift the left / right knee; release to plant. `W` / `O` drive the left / right palm. `Esc` exits. For the padded post, run `dotnet run -- --practice-post`.

**Verified:** `dotnet run -- --check-full-bout` plays two bouts to a contact call. In both, red landed the opener and missed with its next shove. Blue waited a quarter-second after the miss cue, then countered on planted feet in one bout: red was 3.1 cm farther back at release than in the waiting bout. With identical follow-up steps and palm inputs, blue won the counter bout and lost the waiting bout, both by body contact. A scripted keyboard playtest in the desktop window also confirmed the yellow cue, planted palm contact, and a subsequent body-down decision. `--check-opponent`, `--check-stance`, `--check-shove`, `--check-faceoff`, and `--check-bout` pass.

**Open issues:** The desktop inputs were scripted; whether a first-time player notices the cue without coaching is still untested. A fall toward the camera can put the losing contact behind the bottom HUD, though the result text remains visible.

**Next suggested task (M4a, pending assignment):** Build the raised square dohyō, straw-bale boundary, and two white starting lines. Check feet and near-rim contact visibility from the fixed camera before adding wrestler meshes and arena detail.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
