using System.Numerics;

namespace KachiKoshi;

static class BoutCheck
{
    public static int Run()
    {
        bool ok = Check("blue hand touches clay; decision holds", HandDown());
        ok &= Check("red foot lands across rim with its center inside", FootOutside());
        ok &= Check("lean over rim with both feet planted stays live", LegalLean());
        ok &= Check("same-tick losses give a draw", Draw());
        ok &= Check("fresh face-off settles, then overcommitted shove loses", Rematch());
        return ok ? 0 : 1;
    }

    // Stage poses before the first tick; the normal solver, manifolds and bout logic make all calls.
    static void PlacePalm(PracticeScene scene, Wrestler rig, float bottom)
    {
        var arm = rig.Forearms[1];
        var body = scene.Body(arm);
        body.Pose.Orientation = new Quaternion(1, 0, 0, 0); // Distal cap down.
        body.Pose.Position.Y = bottom + arm.Size.X + arm.Size.Y / 2;
    }

    static bool HandDown()
    {
        using var near = new PracticeScene();
        PlacePalm(near, near.Player, .001f);
        near.Step();
        if (near.Result.HasValue) return false; // Speculative contact just above the surface.

        using var scene = new PracticeScene();
        PlacePalm(scene, scene.Player, -.001f);
        scene.Step();
        var result = scene.Result;
        var pos = scene.Pose(scene.Player.Pelvis).Position;
        for (int i = 0; i < 60; i++) scene.Step(new(RightKnee: true));
        return result is { Player.Reason: LossReason.HandDown, Opponent: null }
            && scene.Result == result && scene.Pose(scene.Player.Pelvis).Position == pos;
    }

    static bool FootOutside()
    {
        using var scene = new PracticeScene();
        var rig = scene.Opponent!;
        foreach (var part in rig.Parts)
            scene.Body(part).Pose.Position += new Vector3(1, 0, 1.93f);
        scene.Step();
        if (scene.Result.HasValue) return false; // Outside in the air is legal.
        for (int i = 0; i < 60 && !scene.Result.HasValue; i++) scene.Step();
        return scene.Result is { Player: null, Opponent: { Reason: LossReason.FootOutside } foul }
            && Outside(foul.Point) && rig.Feet.All(foot => !Outside(scene.Pose(foot).Position));
    }

    static bool LegalLean()
    {
        using var scene = new PracticeScene();
        var rig = scene.Player;
        var tilt = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -.27f);
        foreach (var part in rig.Parts)
        {
            var body = scene.Body(part);
            var relative = body.Pose.Position - new Vector3(-.12f, .08f, .37f);
            body.Pose.Position = new Vector3(0, .08f, -1.92f) + Vector3.Transform(relative, tilt);
            body.Pose.Orientation = tilt * body.Pose.Orientation;
        }
        var head = rig.Parts.Single(p => p.Surface == Surface.Head);
        for (int i = 0; i < 60; i++)
        {
            scene.Step();
            if (scene.Result.HasValue) return false;
            if (rig.Planted.All(p => p) && Outside(scene.Pose(head).Position)) return true;
        }
        return false;
    }

    static bool Draw()
    {
        using var scene = new PracticeScene();
        PlacePalm(scene, scene.Player, -.001f);
        PlacePalm(scene, scene.Opponent!, -.001f);
        scene.Step();
        return scene.Result is { Player.Reason: LossReason.HandDown, Opponent.Reason: LossReason.HandDown };
    }

    static bool Rematch()
    {
        using var scene = new PracticeScene(); // Same fresh scene used by R.
        for (int i = 0; i < 90; i++) scene.Step();
        var player = scene.Player;
        if (!player.Planted.All(p => p) || !scene.Opponent!.Planted.All(p => p)) return false;
        var initial = scene.Pose(player.Feet[0]).Position.Y;
        for (int i = 0; i < 20; i++) scene.Step(new(LeftKnee: true));
        if (scene.Result.HasValue || player.Planted[0] || scene.Pose(player.Feet[0]).Position.Y <= initial + .02f)
            return false;
        for (int i = 20; i < 600 && scene.Result == null; i++)
            scene.Step(new(LeftKnee: i < 100, RightPalm: i >= 34 && i < 82));
        return scene.Result is { Player: not null, Opponent: null };
    }

    static bool Outside(Vector3 p) => new Vector2(p.X, p.Z).Length() > PracticeScene.RingRadius;

    static bool Check(string label, bool pass)
    {
        Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {label}");
        return pass;
    }
}
