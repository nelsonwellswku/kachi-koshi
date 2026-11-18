using System.Numerics;
using Raylib_cs;

namespace KachiKoshi;

static class PracticeView
{
    static readonly Color Clay = new(179, 136, 99, 255), Straw = new(232, 201, 144, 255),
        Ink = new(39, 46, 48, 255), Belt = new(45, 75, 94, 255), Skin = new(220, 163, 125, 255),
        Pad = new(136, 66, 51, 255), Binding = new(96, 51, 43, 255);

    public static void Run(PracticeScene scene)
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1000, 650, "Kachi-koshi | Practice");
        try
        {
            Raylib.SetTargetFPS(60);
            var camera = new Camera3D(new(1.05f, 2.65f, 4.6f), new(0, .65f, 0),
                Vector3.UnitY, 40, CameraProjection.Perspective);
            double accumulator = 0;
            while (!Raylib.WindowShouldClose())
            {
                accumulator += Math.Min(Raylib.GetFrameTime(), .1f);
                while (accumulator >= PracticeScene.StepTime)
                {
                    scene.Step();
                    accumulator -= PracticeScene.StepTime;
                }
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(92, 105, 106, 255));
                Raylib.BeginMode3D(camera);
                DrawRing();
                foreach (var part in scene.Parts) DrawSegment(scene, part);
                Raylib.EndMode3D();
                Raylib.DrawRectangle(20, 20, 210, 66, Ink);
                Raylib.DrawRectangle(20, 20, 4, 66, Straw);
                Raylib.DrawText("KACHI-KOSHI", 36, 30, 24, Color.RayWhite);
                Raylib.DrawText("PRACTICE   /   STANCE", 37, 59, 14, Straw);
                Raylib.EndDrawing();
            }
        }
        finally { Raylib.CloseWindow(); }
    }

    static void DrawRing()
    {
        Raylib.DrawCubeV(PracticeScene.FloorCenter, PracticeScene.FloorSize, Clay);
        const int count = 80;
        Vector3 Rim(int i) => new(MathF.Cos(i * MathF.Tau / count) * PracticeScene.RingRadius, .005f,
            MathF.Sin(i * MathF.Tau / count) * PracticeScene.RingRadius);
        for (int i = 0; i < count; i++)
            Raylib.DrawCylinderEx(Rim(i), Rim(i + 1), .026f, .026f, 6, Straw);

        var post = PracticeScene.PostCenter;
        var size = PracticeScene.PostSize;
        Raylib.DrawCubeV(post, size, Pad);
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
