using KachiKoshi;

if (args is ["--check-stance"] or ["--check-shove"])
{
    using var scene = new PracticeScene(practicePost: true);
    return args[0] == "--check-stance" ? StanceCheck.Run(scene) : ShoveCheck.Run(scene);
}

if (args is ["--check-faceoff"]) return FaceoffCheck.Run();
if (args is ["--check-bout"]) return BoutCheck.Run();
if (args is ["--check-opponent"]) return OpponentCheck.Run();
if (args is ["--check-full-bout"]) return FullBoutCheck.Run();

PracticeView.Run(practicePost: args is ["--practice-post"]);
return 0;
