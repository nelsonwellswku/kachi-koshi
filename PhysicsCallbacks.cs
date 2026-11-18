using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace KachiKoshi;

struct Gravity : IPoseIntegratorCallbacks
{
    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;
    public void Initialize(Simulation simulation) { }
    public void PrepareForIntegration(float dt) { }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide inertia, Vector<int> mask, int worker, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        velocity.Linear.Y -= 9.81f * dt;
    }
}

// Narrow phase runs on the scene's single simulation thread. Count only touching manifolds,
// not speculative pairs; for a shove the contacting point must be on the palm end of the arm.
sealed class ContactState
{
    public StaticHandle Floor, Post;
    public BodyHandle[] Feet { get; } = new BodyHandle[2];
    public Segment[] Forearms { get; } = new Segment[2];
    public bool[] Planted { get; } = new bool[2];
    public bool[] OnPad { get; } = new bool[2];
    public bool BodyDown;

    public void Clear()
    {
        Array.Clear(Planted);
        Array.Clear(OnPad);
        BodyDown = false;
    }
}

struct PracticeContacts(ContactState contacts) : INarrowPhaseCallbacks
{
    Simulation sim = null!; // Set by Bepu before the first collision pass.
    public void Initialize(Simulation simulation) => sim = simulation;
    public void Dispose() { }

    // The single wrestler's jointed segments overlap at their pivots. Collide only with scenery.
    public bool AllowContactGeneration(int worker, CollidableReference a, CollidableReference b, ref float margin)
        => a.Mobility == CollidableMobility.Static || b.Mobility == CollidableMobility.Static;

    public bool AllowContactGeneration(int worker, CollidablePair pair, int childA, int childB) => true;
    public bool ConfigureContactManifold(int worker, CollidablePair pair, int childA, int childB,
        ref ConvexContactManifold manifold) => true;

    public bool ConfigureContactManifold<T>(int worker, CollidablePair pair, ref T manifold,
        out PairMaterialProperties material) where T : unmanaged, IContactManifold<T>
    {
        material = new() { FrictionCoefficient = 0.95f, MaximumRecoveryVelocity = 2, SpringSettings = new(80, 1) };
        // Bepu orders dynamic/static pairs with the body first.
        var body = pair.A.BodyHandle;
        for (int j = 0; j < manifold.Count; j++)
        {
            manifold.GetContact(j, out var offset, out var normal, out var depth, out _);
            if (depth < -.002f) continue;
            if (pair.B.StaticHandle == contacts.Floor)
            {
                int foot = Array.IndexOf(contacts.Feet, body);
                if (foot < 0) contacts.BodyDown = true;
                else if (normal.Y > .7f) contacts.Planted[foot] = true;
            }
            else if (pair.B.StaticHandle == contacts.Post && normal.Z > .5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    var arm = contacts.Forearms[i];
                    if (arm.Body != body) continue;
                    var local = Vector3.Transform(offset, Quaternion.Conjugate(sim.Bodies[body].Pose.Orientation));
                    if (local.Y >= arm.Size.Y / 2 - .015f) contacts.OnPad[i] = true;
                }
            }
        }
        return true;
    }
}
