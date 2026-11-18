namespace KachiKoshi;

static class FullBoutCheck
{
    public static int Run()
    {
        // Two fresh face-offs. Both blues wait out the opening hit. Only one
        // responds to the first visible miss; both pursue with the same inputs afterward.
        using var scene = new PracticeScene();
        using var control = new PracticeScene();
        int shoves = 0;
        bool palmTouched = false, openingLanded = false;
        for (int tick = 0; tick < 120 * 30 && !scene.RedMissed && scene.Result == null; tick++)
        {
            var phase = scene.RedPhase;
            scene.Step();
            control.Step();
            if (scene.RedPhase == OpponentPhase.Shove)
            {
                if (phase != OpponentPhase.Shove) { shoves++; palmTouched = false; }
                palmTouched |= scene.Opponent!.PalmHit.Any(p => p);
            }
            if (shoves == 1 && phase == OpponentPhase.Shove && scene.RedPhase == OpponentPhase.Recover)
                openingLanded = palmTouched && !scene.RedMissed;
        }
        bool cleanMiss = shoves > 1 && scene.RedMissed && !palmTouched;
        bool counterHit = false;
        float gain = 0;
        for (int shown = 1; shown <= 240 && scene.RedMissed && scene.Result == null; shown++)
        {
            // Allow 1/4 s to read the cue before pressing O for just under half a second.
            bool drive = shown is >= 30 and < 85;
            scene.Step(new(RightPalm: drive));
            control.Step();
            counterHit |= drive && scene.RedMissed && scene.Player.PalmHit[1] && scene.Player.Planted.All(p => p);
            if (shown == 84 && scene.RedMissed && control.RedMissed && scene.Result == null && control.Result == null)
                gain = control.Pose(control.Opponent!.Pelvis).Position.Z - scene.Pose(scene.Opponent!.Pelvis).Position.Z;
        }
        for (int tick = 0; tick < 120 * 60 && (scene.Result == null || control.Result == null); tick++)
        {
            // Keep up right-palm pressure and take short steps off the planted left foot.
            var follow = new LimbInput(RightKnee: tick % 90 < 10, RightPalm: true);
            scene.Step(follow);
            control.Step(follow);
        }
        bool pass = openingLanded && cleanMiss && counterHit && gain > .015f
            && scene.Result.HasValue && control.Result.HasValue;
        Console.WriteLine($"{(pass ? "PASS" : "FAIL")} full bout: opening hit {openingLanded}, later clean miss {cleanMiss}, " +
            $"planted counter {counterHit}, {gain * 100:F1} cm farther back at release vs waiting. " +
            $"Counter: {Call(scene.Result)}; waiting: {Call(control.Result)}.");
        return pass ? 0 : 1;
    }

    static string Call(BoutResult? result) => result switch
    {
        null => "no call",
        { Player: not null, Opponent: not null } => "draw",
        { Player: { } blue } => $"red wins ({blue.Reason})",
        { Opponent: { } red } => $"blue wins ({red.Reason})",
        _ => throw new InvalidOperationException()
    };
}
