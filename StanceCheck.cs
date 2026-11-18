using System.Numerics;

namespace KachiKoshi;

static class StanceCheck
{
    public static int Run(PracticeScene scene)
    {
        var wrestler = scene.Player;
        float initialHeight = scene.Pose(wrestler.Pelvis).Position.Y;
        float minHeight = float.MaxValue, maxHeight = 0, minUp = 1, worstSole = 0, maxSpeed = 0, maxSpin = 0;
        for (int i = 0; i < 1200; i++)
        {
            scene.Step();
            if (i < 600) continue; // Let the rig land; sample every tick of the final five seconds.
            var pelvis = scene.Pose(wrestler.Pelvis);
            minHeight = Math.Min(minHeight, pelvis.Position.Y);
            maxHeight = Math.Max(maxHeight, pelvis.Position.Y);
            minUp = Math.Min(minUp, Vector3.Transform(Vector3.UnitY, pelvis.Orientation).Y);
            foreach (var part in wrestler.Parts)
            {
                var v = scene.Velocity(part);
                maxSpeed = Math.Max(maxSpeed, v.Linear.Length());
                maxSpin = Math.Max(maxSpin, v.Angular.Length());
            }
            foreach (var foot in wrestler.Feet)
            {
                var pose = scene.Pose(foot);
                for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = new Vector3(x, -1, z) * foot.Size / 2;
                    float y = pose.Position.Y + Vector3.Transform(corner, pose.Orientation).Y;
                    worstSole = Math.Max(worstSole, Math.Abs(y));
                }
            }
        }

        bool standing = initialHeight - maxHeight > .04f && minHeight > .77f && minUp > .97f
            && worstSole < .015f && maxSpeed < .035f && maxSpin < .12f;
        Console.WriteLine($"5-10 s: pelvis {minHeight:F3}..{maxHeight:F3} m (spawn {initialHeight:F3}), " +
            $"min up {minUp:F3}, worst sole gap {worstSole:F4} m, " +
            $"peak motion {maxSpeed:F4} m/s, {maxSpin:F4} rad/s");
        Console.WriteLine(standing ? "PASS: settled standing stance" : "FAIL: stance did not settle");
        return standing ? 0 : 1;
    }
}
