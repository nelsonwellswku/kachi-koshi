namespace KachiKoshi;

static class OpponentCheck
{
    public static int Run()
    {
        using var scene = new PracticeScene();
        using var idle = new PracticeScene(); // Same active red; only blue's counter differs.
        var red = scene.Opponent!;
        float? startZ = null, blueAtShove = null;
        float advance = 0, push = 0, recoil = 0;
        bool leftLift = false, rightLift = false, plantedShove = false, hit = false;
        bool recovered = false, counterHit = false;
        int recoveryTicks = 0;
        for (int i = 0; i < 1200 && !scene.Result.HasValue && !idle.Result.HasValue; i++)
        {
            // Allow a human reaction time after seeing the recovery cue.
            bool counter = recoveryTicks is >= 30 and < 85;
            scene.Step(new(RightPalm: counter));
            idle.Step();
            if (scene.RedPhase == OpponentPhase.Shuffle)
                startZ ??= scene.Pose(red.Pelvis).Position.Z;
            if (startZ.HasValue && !blueAtShove.HasValue)
            {
                leftLift |= !red.Planted[0] && red.Planted[1];
                rightLift |= red.Planted[0] && !red.Planted[1];
            }
            if (scene.RedPhase == OpponentPhase.Shove)
            {
                if (!blueAtShove.HasValue)
                {
                    blueAtShove = scene.Pose(scene.Player.Pelvis).Position.Z;
                    plantedShove = red.Planted[0] && red.Planted[1];
                    advance = scene.Pose(red.Pelvis).Position.Z - startZ.GetValueOrDefault();
                }
                hit |= red.PalmHit.Any(p => p);
                if (hit) push = Math.Max(push, scene.Pose(scene.Player.Pelvis).Position.Z - blueAtShove.Value);
            }
            if (scene.RedPhase == OpponentPhase.Recover)
            {
                recoveryTicks++;
                recovered |= red.Planted[0] && red.Planted[1];
                counterHit |= counter && scene.Player.PalmHit[1] && scene.Player.Planted.All(p => p);
                if (counterHit) recoil = Math.Max(recoil,
                    idle.Pose(idle.Opponent!.Pelvis).Position.Z - scene.Pose(red.Pelvis).Position.Z);
            }
            else if (recoveryTicks > 0) break;
        }

        bool approach = leftLift && rightLift && advance > .02f;
        bool shove = plantedShove && hit && push > .015f;
        bool punish = recovered && counterHit && recoil > .015f && scene.Result == null && idle.Result == null;
        bool passed = approach && shove && punish;
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} opponent: alternating lifts {leftLift && rightLift}, " +
            $"advanced {100 * advance:F1} cm, planted shove {plantedShove && hit} moved blue {100 * push:F1} cm; " +
            $"replanted {recovered}, blue recovery counter {counterHit} moved red back {100 * recoil:F1} cm vs idle");
        return passed ? 0 : 1;
    }
}
