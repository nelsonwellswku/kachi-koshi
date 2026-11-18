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

**Current state:** M1a complete. One articulated placeholder wrestler drops 8 cm and settles into a wide stance on the practice floor. The ring outline, static padded post, and both feet are visible from the fixed rear camera.

**Launch:** From the repository root with a .NET 8-compatible SDK and a desktop display, run `dotnet run`. Press `Esc` to exit.

**Verified:** Launched the Raylib scene on Linux and inspected the settled pose. `dotnet run -- --check-stance` runs the same 120 Hz simulation without a window. Across seconds 5–10, pelvis height held at 0.861 m, all eight sole corners stayed within 1.3 mm of the floor, and peak body speed was under 0.019 m/s.

**Open issues:** No known blocker for the idle stance. Balance under player input is untested.

**Next suggested task (M1b, pending assignment):** Add the Q/P leg and W/O arm targets, then try the step, plant, and shove test against the post.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
