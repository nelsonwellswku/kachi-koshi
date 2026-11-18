using KachiKoshi;

if (args is ["--check-stance"] or ["--check-shove"])
{
    using var scene = new PracticeScene();
    return args[0] == "--check-stance" ? StanceCheck.Run(scene) : ShoveCheck.Run(scene);
}

PracticeView.Run();
return 0;
