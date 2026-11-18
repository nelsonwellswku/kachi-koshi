using KachiKoshi;

using var scene = new PracticeScene();
if (args is ["--check-stance"])
    return StanceCheck.Run(scene);

PracticeView.Run(scene);
return 0;
