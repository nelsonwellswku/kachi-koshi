using System.Numerics;

namespace KachiKoshi;

static class ShoveCheck
{
    // Use exactly the same held-key states and physics ticks as the interactive scene.
    // The two post trials differ only in when Q is released.
    public static int Run(PracticeScene scene)
    {
        bool good = Attempt(scene, latePlant: false, mirror: false);
        using var unplanted = new PracticeScene();
        bool poor = Attempt(unplanted, latePlant: true, mirror: false);
        using var otherSide = new PracticeScene();
        bool mirror = Attempt(otherSide, latePlant: false, mirror: true);
        return good && poor && mirror ? 0 : 1;
    }

    static bool Attempt(PracticeScene scene, bool latePlant, bool mirror)
    {
        for (int i = 0; i < 720; i++) scene.Step();
        int leg = mirror ? 1 : 0, hand = 1 - leg;
        var foot = scene.Feet[leg];
        var arm = scene.Forearms[hand];
        float Reach()
        {
            var pose = scene.Pose(arm);
            var pelvis = scene.Pose(scene.Pelvis);
            var palm = pose.Position + Vector3.Transform(Vector3.UnitY * arm.Size.Y / 2, pose.Orientation);
            return -Vector3.Transform(palm - pelvis.Position, Quaternion.Conjugate(pelvis.Orientation)).Z;
        }

        var start = scene.Pose(foot).Position;
        float initialReach = Reach(), maxReach = initialReach, advance = 0, lift = 0, minUp = 1;
        bool groundedAtDrive = false, bodyDown = false, hit = false, hitWhilePlanted = false;
        const int drive = 34, retract = 82;
        for (int i = 0; i < 600; i++)
        {
            if (i == drive)
            {
                groundedAtDrive = scene.Contacts.Planted[leg];
                advance = start.Z - scene.Pose(foot).Position.Z;
            }
            bool knee = i < (latePlant ? 100 : 20), palm = i >= drive && i < retract;
            scene.Step(mirror ? new(RightKnee: knee, LeftPalm: palm) : new(LeftKnee: knee, RightPalm: palm));
            if (i < drive && !scene.Contacts.Planted[leg])
                lift = Math.Max(lift, scene.Pose(foot).Position.Y - start.Y);
            if (palm)
            {
                maxReach = Math.Max(maxReach, Reach());
                hit |= scene.Contacts.OnPad[hand];
                hitWhilePlanted |= scene.Contacts.OnPad[hand] && scene.Contacts.Planted[leg];
            }
            minUp = Math.Min(minUp, Vector3.Transform(Vector3.UnitY, scene.Pose(scene.Pelvis).Orientation).Y);
            bodyDown |= scene.Contacts.BodyDown;
        }

        bool standing = !bodyDown && minUp > .90f && scene.Pose(scene.Pelvis).Position.Y > .78f
            && scene.Contacts.Planted.All(contact => contact);
        bool passed = lift > .02f && advance > .09f && (latePlant
            ? !groundedAtDrive && hit && bodyDown
            : groundedAtDrive && (mirror || hitWhilePlanted) && standing
                && maxReach - initialReach > .13f && Math.Abs(Reach() - initialReach) < .06f);
        string label = mirror ? "P/W mirrored reach" : latePlant ? "Q held through shove" : "Q plant, O shove";
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {label}: lift {lift:F3} m, advance {advance:F3} m, " +
            $"foot down at drive {groundedAtDrive}, reach gain {maxReach - initialReach:F3} m, " +
            $"pad contact {hit}, body touched clay {bodyDown}");
        return passed;
    }
}
