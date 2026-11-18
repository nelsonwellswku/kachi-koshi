using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;

namespace KachiKoshi;

enum Surface { Skin, Belt, Head }
// Boxes store full extents. Capsules store (radius, distance between cap centers, radius).
record struct Segment(BodyHandle Body, bool IsCapsule, Vector3 Size, Surface Surface);

sealed class PracticeScene : IDisposable
{
    public const float StepTime = 1f / 120;
    public const float RingRadius = 2.25f;
    public static readonly Vector3 FloorSize = new(9, .2f, 9);
    public static readonly Vector3 FloorCenter = -Vector3.UnitY * FloorSize.Y / 2;
    public static readonly Vector3 PostSize = new(.44f, 1.55f, .44f);
    public static readonly Vector3 PostCenter = new(.50f, PostSize.Y / 2, -.08f);

    readonly BufferPool pool = new();
    readonly Simulation sim;
    readonly List<Segment> parts = new();
    public IReadOnlyList<Segment> Parts => parts;
    public Segment Pelvis { get; }
    public Segment[] Feet { get; } = new Segment[2];
    public Segment[] Forearms => Contacts.Forearms;
    public ContactState Contacts { get; } = new();
    public float PadFlash { get; private set; }
    readonly LimbDrive[] drives = [new(), new()];

    public PracticeScene()
    {
        sim = Simulation.Create(pool, new PracticeContacts(Contacts), new Gravity(), new SolveDescription(16, 4));
        Contacts.Floor = StaticBox(FloorCenter, FloorSize);
        Contacts.Post = StaticBox(PostCenter, PostSize);

        // Bind pose is a shallow squat, initially 8 cm above the clay, facing -Z.
        Vector3 At(float x, float y, float z) => new(x, y + .08f, z + .65f);
        Pelvis = BoxPart(At(0, .87f, .04f), new(.63f, .26f, .43f), 18, Surface.Belt);
        var chest = Limb(At(0, 1.10f, .03f), At(0, 1.31f, -.03f), .32f, 31);
        var head = Limb(At(0, 1.64f, -.06f), At(0, 1.72f, -.06f), .16f, 6, Surface.Head);
        Join(Pelvis, chest, At(0, .99f, .03f), 160);
        Join(chest, head, At(0, 1.54f, -.06f), 35);

        for (int i = 0; i < 2; i++)
        {
            int s = 2 * i - 1;
            var hip = At(s * .21f, .86f, .05f);
            var knee = At(s * .36f, .49f, -.09f);
            var ankle = At(s * .39f, .14f, .08f);
            var thigh = Limb(hip, knee, .145f, 11);
            var shin = Limb(knee, ankle, .098f, 5);
            var foot = BoxPart(At(s * .39f, .075f, -.01f), new(.25f, .15f, .43f), 3, Surface.Skin);
            Feet[i] = foot;
            Contacts.Feet[i] = foot.Body;
            drives[i].Hip = Join(Pelvis, thigh, hip, 235);
            drives[i].Knee = Join(thigh, shin, knee, 240);
            drives[i].Ankle = Join(shin, foot, ankle, 135);

            var shoulder = At(s * .28f, 1.38f, -.015f);
            var elbow = At(s * .46f, 1.08f, -.04f);
            var palm = At(s * .48f, 1.14f, -.30f);
            var upperArm = Limb(shoulder, elbow, .105f, 4);
            var forearm = Limb(elbow, palm, .09f, 3);
            Forearms[i] = forearm;
            drives[i].Shoulder = Join(chest, upperArm, shoulder, 42);
            drives[i].Elbow = Join(upperArm, forearm, elbow, 28);
        }
        sim.Solver.Add(Pelvis.Body, new OneBodyAngularServo
        {
            TargetOrientation = Quaternion.Identity, SpringSettings = new(2, 2), ServoSettings = new(3, 0, 18)
        });
    }

    public RigidPose Pose(Segment part) => sim.Bodies[part.Body].Pose;
    public BodyVelocity Velocity(Segment part) => sim.Bodies[part.Body].Velocity;
    public void Step(LimbInput input = default)
    {
        drives[0].Update(sim.Solver, input.LeftKnee, input.LeftPalm);
        drives[1].Update(sim.Solver, input.RightKnee, input.RightPalm);
        Contacts.Clear();
        sim.Timestep(StepTime);
        PadFlash = Contacts.OnPad[0] || Contacts.OnPad[1] ? .20f : Math.Max(0, PadFlash - StepTime);
    }

    StaticHandle StaticBox(Vector3 position, Vector3 size) => sim.Statics.Add(new StaticDescription(position,
        sim.Shapes.Add(new Box(size.X, size.Y, size.Z))));

    Segment BoxPart(Vector3 position, Vector3 size, float mass, Surface surface) =>
        Add(new Box(size.X, size.Y, size.Z), new(position), mass, size, false, surface);

    Segment Limb(Vector3 a, Vector3 b, float radius, float mass, Surface surface = Surface.Skin)
    {
        // Point the capsule axis up the segment (either cap can be the proximal end).
        Vector3 axis = Vector3.Normalize(b - a);
        if (axis.Y < 0) axis = -axis;
        var rotation = Quaternion.Normalize(new Quaternion(Vector3.Cross(Vector3.UnitY, axis), 1 + axis.Y));
        float length = Vector3.Distance(a, b);
        return Add(new Capsule(radius, length), new((a + b) / 2, rotation), mass,
            new(radius, length, radius), true, surface);
    }

    Segment Add<T>(T shape, RigidPose pose, float mass, Vector3 size, bool capsule, Surface surface)
        where T : unmanaged, IConvexShape
    {
        var handle = sim.Bodies.Add(BodyDescription.CreateDynamic(pose, shape.ComputeInertia(mass),
            sim.Shapes.Add(shape), new BodyActivityDescription(-1))); // Keep pose motors active.
        var part = new Segment(handle, capsule, size, surface);
        parts.Add(part);
        return part;
    }

    PoseMotor Join(Segment parent, Segment child, Vector3 pivot, float torque)
    {
        var a = Pose(parent);
        var b = Pose(child);
        var invA = Quaternion.Conjugate(a.Orientation);
        sim.Solver.Add(parent.Body, child.Body, new BallSocket
        {
            LocalOffsetA = Vector3.Transform(pivot - a.Position, invA),
            LocalOffsetB = Vector3.Transform(pivot - b.Position, Quaternion.Conjugate(b.Orientation)),
            SpringSettings = new(55, 1)
        });
        var servo = new AngularServo
        {
            TargetRelativeRotationLocalA = invA * b.Orientation,
            SpringSettings = new(36, 3), ServoSettings = new(8, 0, torque)
        };
        return new(sim.Solver.Add(parent.Body, child.Body, servo), invA, b.Orientation, servo);
    }

    public void Dispose()
    {
        sim.Dispose();
        pool.Clear();
    }
}
