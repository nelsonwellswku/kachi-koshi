using System.Numerics;

namespace KachiKoshi;

enum LossReason { FootOutside, BodyDown, HandDown }

readonly record struct Foul(LossReason Reason, Vector3 Point);
readonly record struct BoutResult(Foul? Player, Foul? Opponent);
