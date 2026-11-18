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

**Current state:** M2a complete. The blue-belt player faces a passive red-belt opponent. Both use the same jointed rig and settle on their feet. Each wrestler collides with the clay and the other wrestler; overlapping parts of the same rig ignore each other. A right-palm shove rocks the opponent back and flashes `PALM CONTACT` on the HUD.

**Launch / try it:** Run `dotnet run` with a .NET 8-compatible SDK and a desktop display. Let both wrestlers settle. Tap `Q` for about 0.17 s, release, then hold `O` for about 0.4 s as the left foot lands. Watch the red belt move back. `P` lifts the right knee, `W` drives the left palm, `R` resets both rigs, and `Esc` exits. For the original padded-post drill, run `dotnet run -- --practice-post`.

**Verified:** `dotnet run -- --check-faceoff` checks both stances over the last two seconds of a six-second settle at 120 Hz. Neither palm touched the other rig at rest. The right palm hit during the drive, and the opponent's pelvis moved 3.0 cm farther back than in an otherwise identical replay with the step alone. The original `--check-stance` and `--check-shove` checks also pass.

**Open issues:** No known M2a blocker. Falls and ring-outs do not end the face-off yet.

**Next suggested task (M2b, pending assignment):** Use clay contact points to decide losses for either wrestler. Check a hand touching down, a foot landing outside, and a lean across the boundary with both feet still inside. Show the result and let `R` start a fresh bout.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
