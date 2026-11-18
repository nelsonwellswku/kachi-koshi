using System.Numerics;

namespace KachiKoshi;

static class FaceoffCheck
{
    public static int Run()
    {
        using var scene = new PracticeScene(computerOpponent: false);
        using var baseline = new PracticeScene(computerOpponent: false);
        var opponent = scene.Opponent!;
        bool settled = true, idleHit = false;
        foreach (var rig in new[] { scene.Player, opponent })
        {
            var toOther = scene.Pose(rig == opponent ? scene.Player.Pelvis : opponent.Pelvis).Position
                - scene.Pose(rig.Pelvis).Position;
            var forward = Vector3.Transform(-Vector3.UnitZ, scene.Pose(rig.Pelvis).Orientation);
            settled &= Vector3.Dot(forward, Vector3.Normalize(toOther)) > .9f;
        }
        for (int tick = 0; tick < 720; tick++)
        {
            scene.Step();
            baseline.Step();
            idleHit |= scene.Player.PalmHit.Any(hit => hit) || opponent.PalmHit.Any(hit => hit);
            if (tick >= 480) settled &= Standing(scene, scene.Player) && Standing(scene, opponent);
        }

        bool hit = false;
        float recoil = 0;
        for (int tick = 0; tick < 144; tick++)
        {
            bool knee = tick < 20, drive = tick >= 34 && tick < 82;
            scene.Step(new(LeftKnee: knee, RightPalm: drive));
            baseline.Step(new(LeftKnee: knee));
            hit |= drive && scene.Player.PalmHit[1];
            if (hit)
                recoil = Math.Max(recoil, baseline.Pose(baseline.Opponent!.Pelvis).Position.Z
                    - scene.Pose(opponent.Pelvis).Position.Z);
        }

        bool passed = settled && !idleHit && hit && recoil > .015f;
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} face-off: both settled facing each other {settled}, " +
            $"idle palm contact {idleHit}, right palm hit {hit}, opponent recoil vs step alone {100 * recoil:F1} cm");
        return passed ? 0 : 1;
    }

    static bool Standing(PracticeScene scene, Wrestler rig)
    {
        var pelvis = scene.Pose(rig.Pelvis);
        return rig.Planted[0] && rig.Planted[1] && !rig.BodyDown && pelvis.Position.Y > .77f
            && Vector3.Transform(Vector3.UnitY, pelvis.Orientation).Y > .97f
            && rig.Parts.All(part => scene.Velocity(part).Linear.Length() < .035f
                && scene.Velocity(part).Angular.Length() < .12f);
    }
}
