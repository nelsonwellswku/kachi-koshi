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

**Current state:** M2 complete. Either wrestler loses when a non-foot part touches the clay or a foot touches the clay beyond the circular boundary. Overhanging the rim is legal. The bout freezes on the deciding tick, a yellow dot marks the contact, and the HUD names the winner and the cause. If both lose on the same 120 Hz tick, the call is a draw.

**Launch / try it:** Run `dotnet run` with a .NET 8-compatible SDK and a desktop display. Let both wrestlers settle. Tap `Q`, release to plant the left foot, then hold `O` for a right-palm shove. To try a fall instead, keep `Q` held as you drive with `O` and hold both for about two seconds. Press `R` for an instant rematch. `P` lifts the right knee, `W` drives the left palm, and `Esc` exits. The padded-post drill remains available with `dotnet run -- --practice-post`.

**Verified:** `dotnet run -- --check-bout` exercises real clay contacts: a blue hand 1 mm above the clay stays legal, a touching hand loses, a red foot lands outside with its center still inside, and a lean with the head over the rim and both feet planted stays legal. It also checks same-tick draws, a frozen result, and a fresh bout played to a loss using limb inputs. In the Linux window, holding `Q` through a shove produced a persistent result banner and `R` restored the opening HUD. `--check-stance`, `--check-shove`, and `--check-faceoff` also pass.

**Open issues:** No known M2 blocker. Red holds its stance and responds to pushes but has no active strategy yet.

**Next suggested task (M3a, pending assignment):** Give red a delayed controller using the same limb inputs as blue. Start with a shuffle into range and a planted shove; check that blue can exploit the recovery.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
