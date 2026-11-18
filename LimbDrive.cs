using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;

namespace KachiKoshi;

readonly record struct LimbInput(bool LeftKnee = false, bool RightKnee = false,
    bool LeftPalm = false, bool RightPalm = false);

// Pitch about the wrestler's bind-pose lateral axis, expressed in the joint's parent frame.
record struct PoseMotor(ConstraintHandle Handle, Quaternion ParentInverse, Quaternion ChildBind, AngularServo Servo)
{
    public readonly void Pitch(Solver solver, float angle)
    {
        var target = Servo;
        target.TargetRelativeRotationLocalA = ParentInverse *
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle) * ChildBind;
        solver.ApplyDescription(Handle, target);
    }
}

sealed class LimbDrive
{
    public PoseMotor Hip, Knee, Ankle, Shoulder, Elbow;
    float lift, stride, reach;

    public void Update(Solver solver, bool knee, bool palm)
    {
        static float Approach(float value, bool held, float rate) => held
            ? Math.Min(1, value + rate * PracticeScene.StepTime)
            : Math.Max(0, value - rate * PracticeScene.StepTime);

        // Lower the sole faster than drawing the leg back, so it takes weight ahead of the hip.
        float nextLift = Approach(lift, knee, knee ? 4 : 6);
        float nextStride = Approach(stride, knee, knee ? 4 : 1.2f);
        if (nextLift != lift || nextStride != stride)
        {
            lift = nextLift;
            stride = nextStride;
            float thigh = .43f * stride + .48f * lift;
            float shin = .43f * stride - .48f * lift;
            Hip.Pitch(solver, thigh);
            Knee.Pitch(solver, shin - thigh);
            Ankle.Pitch(solver, -shin);
        }

        float nextReach = Approach(reach, palm, palm ? 5 : 3);
        if (nextReach != reach)
        {
            reach = nextReach;
            Shoulder.Pitch(solver, 1.35f * reach);
            Elbow.Pitch(solver, -1.42f * reach);
        }
    }
}
