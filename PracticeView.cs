using System.Numerics;
using Raylib_cs;

namespace KachiKoshi;

static class PracticeView
{
    static readonly Color Clay = new(179, 136, 99, 255), Straw = new(232, 201, 144, 255),
        Ink = new(39, 46, 48, 255), Belt = new(45, 75, 94, 255), Skin = new(220, 163, 125, 255),
        Pad = new(136, 66, 51, 255), Binding = new(96, 51, 43, 255);

    public static void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1000, 650, "Kachi-koshi | Practice");
        try
        {
            Raylib.SetTargetFPS(60);
            var camera = new Camera3D(new(1.05f, 2.65f, 4.6f), new(0, .65f, 0),
                Vector3.UnitY, 40, CameraProjection.Perspective);
            while (RunAttempt(camera)) { }
        }
        finally { Raylib.CloseWindow(); }
    }

    // A reset creates the same initial rig, including its solver and contact history.
    static bool RunAttempt(Camera3D camera)
    {
        using var scene = new PracticeScene();
        double accumulator = 0;
        while (!Raylib.WindowShouldClose())
        {
            bool reset = Raylib.IsKeyPressed(KeyboardKey.R);
            var input = new LimbInput(Raylib.IsKeyDown(KeyboardKey.Q), Raylib.IsKeyDown(KeyboardKey.P),
                Raylib.IsKeyDown(KeyboardKey.W), Raylib.IsKeyDown(KeyboardKey.O));
            accumulator += Math.Min(Raylib.GetFrameTime(), .1f);
            while (accumulator >= PracticeScene.StepTime)
            {
                scene.Step(input);
                accumulator -= PracticeScene.StepTime;
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(92, 105, 106, 255));
            Raylib.BeginMode3D(camera);
            DrawRing(scene.PadFlash > 0);
            foreach (var part in scene.Parts) DrawSegment(scene, part);
            Raylib.EndMode3D();
            DrawHud(scene, input);
            Raylib.EndDrawing();
            if (reset) return true;
        }
        return false;
    }

    static void DrawHud(PracticeScene scene, LimbInput input)
    {
        Raylib.DrawRectangle(20, 20, 465, 66, Ink);
        Raylib.DrawRectangle(20, 20, 4, 66, Straw);
        Raylib.DrawText("KACHI-KOSHI", 36, 30, 24, Color.RayWhite);
        Raylib.DrawText("Tap Q, release to plant, then hold O to shove.", 37, 59, 16, Straw);
        Raylib.DrawRectangle(0, 584, 1000, 66, Ink);
        DrawSide(24, "LEFT", "Q", "W", input.LeftKnee, input.LeftPalm, scene.Contacts.Planted[0]);
        DrawSide(348, "RIGHT", "P", "O", input.RightKnee, input.RightPalm, scene.Contacts.Planted[1]);
        Raylib.DrawText(scene.Contacts.BodyDown ? "BODY ON CLAY" : scene.PadFlash > 0 ? "PAD CONTACT" : "PRACTICE POST",
            765, 596, 18, Straw);
        Raylib.DrawText("R  Reset     Esc  Exit", 765, 623, 14, Color.RayWhite);
    }

    static void DrawSide(int x, string side, string legKey, string handKey, bool lift, bool reach, bool ground)
    {
        Raylib.DrawText($"{side} FOOT  /  {(ground ? "ON CLAY" : "IN AIR")}", x, 596, 18, ground ? Straw : Color.Orange);
        Raylib.DrawText($"{legKey}  Lift knee", x, 623, 16, lift ? Color.Orange : Color.RayWhite);
        Raylib.DrawText($"{handKey}  Drive palm", x + 135, 623, 16, reach ? Color.Orange : Color.RayWhite);
    }

    static void DrawRing(bool hit)
    {
        Raylib.DrawCubeV(PracticeScene.FloorCenter, PracticeScene.FloorSize, Clay);
        const int count = 80;
        Vector3 Rim(int i) => new(MathF.Cos(i * MathF.Tau / count) * PracticeScene.RingRadius, .005f,
            MathF.Sin(i * MathF.Tau / count) * PracticeScene.RingRadius);
        for (int i = 0; i < count; i++)
            Raylib.DrawCylinderEx(Rim(i), Rim(i + 1), .026f, .026f, 6, Straw);

        var post = PracticeScene.PostCenter;
        var size = PracticeScene.PostSize;
        Raylib.DrawCubeV(post, size, hit ? Straw : Pad);
        foreach (float y in new[] { .12f, size.Y - .12f })
            Raylib.DrawCubeV(new(post.X, y, post.Z), new(size.X + .006f, .055f, size.Z + .006f), Binding);
    }

    static void DrawSegment(PracticeScene scene, Segment part)
    {
        var pose = scene.Pose(part);
        var q = pose.Orientation;
        Rlgl.PushMatrix();
        Rlgl.Translatef(pose.Position.X, pose.Position.Y, pose.Position.Z);
        var axis = new Vector3(q.X, q.Y, q.Z);
        if (axis.LengthSquared() > 1e-10f)
            Rlgl.Rotatef(2 * MathF.Atan2(axis.Length(), q.W) * 180 / MathF.PI, axis.X, axis.Y, axis.Z);
        if (part.IsCapsule)
        {
            var end = Vector3.UnitY * part.Size.Y / 2;
            Raylib.DrawCapsule(-end, end, part.Size.X, 16, 6, Skin);
            if (part == scene.Forearms[0] || part == scene.Forearms[1])
                Raylib.DrawSphere(end, part.Size.X + .002f, Belt);
            if (part.Surface == Surface.Head)
            {
                Raylib.DrawSphere(end + Vector3.UnitY * .04f, .15f, Ink);
                Raylib.DrawSphere(end + new Vector3(0, .16f, .045f), .07f, Ink);
            }
        }
        else
        {
            Raylib.DrawCubeV(Vector3.Zero, part.Size, part.Surface == Surface.Belt ? Belt : Skin);
            if (part.Surface == Surface.Belt)
                Raylib.DrawCubeV(new(0, .025f, part.Size.Z / 2), new(.18f, .12f, .08f), Ink);
        }
        Rlgl.PopMatrix();
    }
}
