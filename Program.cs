using KachiKoshi;

if (args is ["--check-stance"] or ["--check-shove"])
{
    using var scene = new PracticeScene(practicePost: true);
    return args[0] == "--check-stance" ? StanceCheck.Run(scene) : ShoveCheck.Run(scene);
}

if (args is ["--check-faceoff"]) return FaceoffCheck.Run();

PracticeView.Run(practicePost: args is ["--practice-post"]);
return 0;
