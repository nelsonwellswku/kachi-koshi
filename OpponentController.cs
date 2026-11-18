namespace KachiKoshi;

enum OpponentPhase { Plant, Shuffle, Shove, Recover }

// One call per physics tick. Reactions use old observations; once a limb action starts,
// red must finish it even if blue moves or countershoves.
sealed class OpponentController
{
    const int ObserveDelay = 20, Opening = 110, Lift = 14, Plant = 70, Drive = 50, Recovery = 90;
    const float Reach = .80f;
    readonly record struct Sight(float Gap, float Side);
    readonly Sight[] history = new Sight[ObserveDelay]; // 1/6 s at 120 Hz.
    int cursor, ticks = Opening;
    bool leftStep, rightPalm, shoveConnected;
    public OpponentPhase Phase { get; private set; }
    public bool RecoveringFromMiss => Phase == OpponentPhase.Recover && !shoveConnected;

    // Sample after the physics tick, including the last tick with the arm driven.
    public void RecordPalmContact(bool left, bool right)
    {
        if (Phase == OpponentPhase.Shove) shoveConnected |= rightPalm ? right : left;
    }

    public LimbInput Step(float gap, float side, bool planted)
    {
        var seen = history[cursor];
        history[cursor] = new(gap, side);
        cursor = (cursor + 1) % history.Length;
        if (ticks == 0)
        {
            switch (Phase)
            {
                case OpponentPhase.Shuffle:
                    Phase = OpponentPhase.Plant;
                    ticks = Plant;
                    break;
                case OpponentPhase.Recover:
                    Phase = OpponentPhase.Plant;
                    break;
                case OpponentPhase.Shove:
                    Phase = OpponentPhase.Recover;
                    ticks = Recovery;
                    break;
                case OpponentPhase.Plant when planted:
                    if (seen.Gap <= Reach)
                    {
                        Phase = OpponentPhase.Shove;
                        // Follow a landed shove with the other hand while blue still
                        // looks square between the shoulders. A stagger can bait that palm wide.
                        rightPalm = shoveConnected && Math.Abs(seen.Side) < .28f ? !rightPalm : seen.Side >= 0;
                        shoveConnected = false;
                        ticks = Drive;
                    }
                    else
                    {
                        Phase = OpponentPhase.Shuffle;
                        leftStep = !leftStep;
                        ticks = Lift;
                    }
                    break;
            }
        }
        if (ticks > 0) ticks--;
        return Phase switch
        {
            OpponentPhase.Shuffle => new(LeftKnee: leftStep, RightKnee: !leftStep),
            OpponentPhase.Shove => new(LeftPalm: !rightPalm, RightPalm: rightPalm),
            _ => default
        };
    }
}
