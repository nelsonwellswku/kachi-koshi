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

**Current state:** M1b complete. All four limb controls drive strength-limited joint motors. From rest, the wrestler can step forward onto the left foot, shove the fixed post with the right palm, and stay standing. The HUD reports each foot's contact with the clay; the pad flashes when a palm hits its front face.

**Launch / try it:** From the repository root with a .NET 8-compatible SDK and a desktop display, run `dotnet run`. Let the wrestler settle. Tap `Q` for about 0.17 s, release, then hold `O` for about 0.4 s as the left foot lands. Release `O` to retract. Press `R` to reset and try again, this time holding `Q` through the shove. `P` lifts the right knee, `W` drives the left palm, and `Esc` exits.

**Verified:** `dotnet run -- --check-stance` passes. `dotnet run -- --check-shove` replays both attempts at 120 Hz. The early release planted the left foot 12.5 cm forward; the right palm contacted the pad and the wrestler stayed up. With only the `Q` release delayed, the left foot was airborne at the start of the drive, the palm still hit, and the body fell onto the clay. The mirrored `P`/`W` step, reach, and retraction also passed. In the Linux window, keyboard input reproduced the pad flash, recovery, fall, and reset.

**Open issues:** No known blocker for the practice drill. Contact filtering currently assumes one wrestler: it permits collisions with the scenery but suppresses all body-to-body contacts.

**Next suggested task (M2a, pending assignment):** Spawn a second wrestler facing the player. Filter contacts by wrestler so a palm can push the other rig without its own overlapping joints colliding. Verify both settle and the opponent responds to a shove before adding loss detection.

The owner assigns work one slice at a time. When handing a slice back, update the current state and next available task above with what actually works, how to run or verify it, and any unresolved issue. Treat the next task as a suggestion until assigned.
