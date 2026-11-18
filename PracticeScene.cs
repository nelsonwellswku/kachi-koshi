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
    readonly OpponentController? controller;
    public OpponentPhase? RedPhase => controller?.Phase;
    public bool RedMissed => controller?.RecoveringFromMiss == true;
    public Wrestler Player { get; }
    public Wrestler? Opponent { get; }
    public float HitFlash { get; private set; }
    public BoutResult? Result { get; private set; }

    public PracticeScene(bool practicePost = false, bool computerOpponent = true)
    {
        var contacts = new ContactState();
        sim = Simulation.Create(pool, new PracticeContacts(contacts), new Gravity(), new SolveDescription(16, 4));
        contacts.Floor = StaticBox(FloorCenter, FloorSize);
        if (practicePost) contacts.Post = StaticBox(PostCenter, PostSize);
        // Stagger the face-off just enough to put the chest in the right palm's path.
        float spacing = practicePost ? .65f : .37f;
        float stagger = practicePost ? 0 : .12f;
        Player = new Wrestler(sim, new(-stagger, .08f, spacing), Quaternion.Identity);
        contacts.Register(Player);
        if (!practicePost)
        {
            Opponent = new Wrestler(sim, new(stagger, .08f, -spacing), new Quaternion(0, 1, 0, 0));
            contacts.Register(Opponent);
            if (computerOpponent) controller = new();
        }
    }

    public BodyReference Body(Segment part) => sim.Bodies[part.Body];
    public RigidPose Pose(Segment part) => Body(part).Pose;
    public BodyVelocity Velocity(Segment part) => Body(part).Velocity;
    public void Step(LimbInput input = default)
    {
        if (Result.HasValue) return;
        Player.PrepareStep(input);
        if (Opponent is { } red)
        {
            var pose = Pose(red.Pelvis);
            var toBlue = Pose(Player.Pelvis).Position - pose.Position;
            var local = Vector3.Transform(toBlue, Quaternion.Conjugate(pose.Orientation));
            red.PrepareStep(controller?.Step(new Vector2(toBlue.X, toBlue.Z).Length(), local.X,
                red.Planted[0] && red.Planted[1]) ?? default);
        }
        sim.Timestep(StepTime);
        if (Opponent is { } rival) controller?.RecordPalmContact(rival.PalmHit[0], rival.PalmHit[1]);
        HitFlash = Player.PalmHit[0] || Player.PalmHit[1] ? .20f : Math.Max(0, HitFlash - StepTime);
        if (Opponent != null && (Player.Foul.HasValue || Opponent.Foul.HasValue))
            Result = new BoutResult(Player.Foul, Opponent.Foul);
    }

    StaticHandle StaticBox(Vector3 position, Vector3 size) => sim.Statics.Add(new StaticDescription(position,
        sim.Shapes.Add(new Box(size.X, size.Y, size.Z))));

    public void Dispose()
    {
        sim.Dispose();
        pool.Clear();
    }
}

sealed class Wrestler
{
    readonly Simulation sim;
    readonly Quaternion facing;
    readonly List<Segment> parts = new();
    readonly LimbDrive[] drives = [new(), new()];
    public IReadOnlyList<Segment> Parts => parts;
    public Segment Pelvis { get; }
    public Segment[] Feet { get; } = new Segment[2];
    public Segment[] Forearms { get; } = new Segment[2];
    public bool[] Planted { get; } = new bool[2];
    public bool[] PalmHit { get; } = new bool[2];
    public bool BodyDown { get; set; }
    public Foul? Foul { get; private set; }

    public void RecordFoul(LossReason reason, Vector3 point)
    {
        // Multiple contacts can arrive in one tick. Prefer the more specific touchdown.
        if (Foul == null || reason > Foul.Value.Reason) Foul = new(reason, point);
    }

    public Wrestler(Simulation sim, Vector3 origin, Quaternion facing)
    {
        this.sim = sim;
        this.facing = facing;
        Vector3 At(float x, float y, float z) => origin + Vector3.Transform(new Vector3(x, y, z), facing);
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
            TargetOrientation = facing, SpringSettings = new(2, 2), ServoSettings = new(3, 0, 18)
        });
    }

    public void PrepareStep(LimbInput input)
    {
        drives[0].Update(sim.Solver, input.LeftKnee, input.LeftPalm);
        drives[1].Update(sim.Solver, input.RightKnee, input.RightPalm);
        Array.Clear(Planted);
        Array.Clear(PalmHit);
        BodyDown = false;
        Foul = null;
    }

    Segment BoxPart(Vector3 position, Vector3 size, float mass, Surface surface) =>
        Add(new Box(size.X, size.Y, size.Z), new(position, facing), mass, size, false, surface);

    Segment Limb(Vector3 a, Vector3 b, float radius, float mass, Surface surface = Surface.Skin)
    {
        // Point the capsule axis up the segment (either cap can be the proximal end).
        Vector3 axis = Vector3.Normalize(b - a);
        if (axis.Y < 0) axis = -axis;
        var rotation = Quaternion.Normalize(new Quaternion(Vector3.Cross(Vector3.UnitY, axis), 1 + axis.Y));
        float length = Vector3.Distance(a, b);
        return Add(new Capsule(radius, length), new((a + b) / 2, rotation * facing), mass,
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
        var a = sim.Bodies[parent.Body].Pose;
        var b = sim.Bodies[child.Body].Pose;
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
        // Express both bind frames relative to this wrestler so pitch follows its lateral axis.
        return new(sim.Solver.Add(parent.Body, child.Body, servo),
            invA * facing, Quaternion.Conjugate(facing) * b.Orientation, servo);
    }
}
