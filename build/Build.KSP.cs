using Nuke.Common;
using Nuke.Common.IO;
using static Nuke.Common.IO.FileSystemTasks;

partial class Build
{
    [Parameter("Path where local installation of KSP is located.")]
    readonly AbsolutePath KspDir;

    AbsolutePath GameData => KspDir / "GameData";
    AbsolutePath InstallDir => GameData / "PSG";

    Target RestoreKspRefs => _ => _
        .Requires(() => KspDir)
        .Requires(() => DirectoryExists(KspDir))
        .After(Clean)
        .DependentFor(Compile)
        .Executes(() =>
        {
            var engine = KspDir / "KSP_Data" / "Managed";
            var kos = GameData / "kOS" / "Plugins";

            var files = new[]
            {
                engine / "Assembly-CSharp.dll",
                engine / "UnityEngine.CoreModule.dll",
                engine / "UnityEngine.dll",
                kos / "kOS.dll",
                kos / "kOS.Safe.dll",
            };

            foreach (var file in files)
            {
                CopyFileToDirectory(file, RefsDirectory, FileExistsPolicy.Skip);
            }
        });

    Target Install => _ => _
        .Requires(() => KspDir)
        .DependsOn(Rebuild)
        .Executes(() =>
        {
            EnsureCleanDirectory(InstallDir);

            var files = MainProject.Directory
                .GlobFiles(@$"bin/{Configuration}/**/{MainProject.Name}.{{dll,pdb}}");
            foreach (var file in files)
            {
                CopyFileToDirectory(file, InstallDir);
            }
        });

    Target Uninstall => _ => _
        .Requires(() => KspDir)
        .Executes(() =>
        {
            DeleteDirectory(InstallDir);
        });
}
