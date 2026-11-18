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

struct PracticeContacts : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }
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
        return true;
    }
}
