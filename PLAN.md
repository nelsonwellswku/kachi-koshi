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

**Current state:** M3a complete. Red uses the same knee and palm controls as blue. It judges range and palm side from 1/6-second-old observations, alternates short steps to close the gap, and starts a shove only with both feet planted. After committing to the shove, red releases all four controls for 0.75 seconds before it can attack again. The HUD shows when red is stepping, planting, shoving, or recovering. M2 contact calls, same-tick draws, and instant rematches remain in place.

**Launch / try it:** Run `dotnet run` with a .NET 8-compatible SDK and a desktop display. For the first exchange, leave both feet down and let red step in and shove. When the HUD changes to `RED: RECOVERING`, hold `O` for about half a second to push red back with your right palm. Press `R` to replay the opening. `Q` / `P` lift the left / right knee; release to plant. `W` drives the left palm and `Esc` exits. The padded-post drill is available with `dotnet run -- --practice-post`.

**Verified:** `dotnet run -- --check-opponent` plays two headless bouts from the normal opening. Red lifted each foot in turn, advanced 5.7 cm, and landed a planted shove that moved blue 4.1 cm. In one bout blue waited a quarter-second into recovery, then countered with `O` on planted feet. Red recoiled 3.7 cm farther than against idle blue, and both stayed in the bout. `--check-stance`, `--check-shove`, `--check-faceoff` (passive sparring partner), and `--check-bout` also pass.

**Open issues:** The recovery counter is verified as a positional gain from the opening. Recovery timing and readability later in a human-played bout still need playtesting.

**Next suggested task (M3b, pending assignment):** Playtest full bouts. Check whether blue can read and punish a missed shove after the opening exchange, and tune red's reach and recovery timing if needed.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
