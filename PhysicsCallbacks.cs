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
    public StaticHandle Floor;
    public StaticHandle? Post;
    readonly Dictionary<BodyHandle, Wrestler> owners = new();

    public void Register(Wrestler wrestler)
    {
        foreach (var part in wrestler.Parts) owners.Add(part.Body, wrestler);
    }

    public Wrestler Owner(BodyHandle body) => owners[body];
}

struct PracticeContacts(ContactState contacts) : INarrowPhaseCallbacks
{
    Simulation sim = null!; // Set by Bepu before the first collision pass.
    public void Initialize(Simulation simulation) => sim = simulation;
    public void Dispose() { }

    // A rig's adjacent shapes overlap by design. Other wrestlers and scenery are solid.
    public bool AllowContactGeneration(int worker, CollidableReference a, CollidableReference b, ref float margin)
        => a.Mobility == CollidableMobility.Static || b.Mobility == CollidableMobility.Static
            || contacts.Owner(a.BodyHandle) != contacts.Owner(b.BodyHandle);

    public bool AllowContactGeneration(int worker, CollidablePair pair, int childA, int childB) => true;
    public bool ConfigureContactManifold(int worker, CollidablePair pair, int childA, int childB,
        ref ConvexContactManifold manifold) => true;

    public bool ConfigureContactManifold<T>(int worker, CollidablePair pair, ref T manifold,
        out PairMaterialProperties material) where T : unmanaged, IContactManifold<T>
    {
        material = new() { FrictionCoefficient = 0.95f, MaximumRecoveryVelocity = 2, SpringSettings = new(80, 1) };
        // Bepu orders dynamic/static pairs with the body first.
        var body = pair.A.BodyHandle;
        var owner = contacts.Owner(body);
        bool scenery = pair.B.Mobility == CollidableMobility.Static;
        for (int j = 0; j < manifold.Count; j++)
        {
            manifold.GetContact(j, out var offset, out var normal, out var depth, out _);
            if (depth < -.002f) continue;
            if (!scenery)
            {
                var other = pair.B.BodyHandle;
                RecordPalm(owner, body, offset);
                // Bepu reports offsets relative to A even when the palm belongs to B.
                RecordPalm(contacts.Owner(other), other,
                    offset + (sim.Bodies[body].Pose.Position - sim.Bodies[other].Pose.Position));
            }
            else if (pair.B.StaticHandle == contacts.Floor)
            {
                if (owner.Feet[0].Body == body) owner.Planted[0] |= normal.Y > .7f;
                else if (owner.Feet[1].Body == body) owner.Planted[1] |= normal.Y > .7f;
                else owner.BodyDown = true;
            }
            else if (pair.B.StaticHandle == contacts.Post && normal.Z > .5f)
                RecordPalm(owner, body, offset);
        }
        return true;
    }

    void RecordPalm(Wrestler owner, BodyHandle body, Vector3 offset)
    {
        for (int i = 0; i < 2; i++)
        {
            var arm = owner.Forearms[i];
            if (arm.Body != body) continue;
            var local = Vector3.Transform(offset, Quaternion.Conjugate(sim.Bodies[body].Pose.Orientation));
            if (local.Y >= arm.Size.Y / 2 - .015f) owner.PalmHit[i] = true;
        }
    }
}
