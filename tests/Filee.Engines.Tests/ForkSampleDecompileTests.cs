// Fork only, not for the PR: decompiles three real Garry's Mod models (tests/fork-samples) and checks the output
// against values a separate prototype decoded from the same files.

using System.Globalization;
using Filee.Engines.Models3D;

namespace Filee.Engines.Tests;

public class ForkSampleDecompileTests(EngineFixture fx, ITestOutputHelper output) : IClassFixture<EngineFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string Sample(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Filee.slnx")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "tests", "fork-samples", name + ".mdl");
    }

    private string Decompile(string name)
    {
        var root = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        var folder = root is null ? fx.NewFolder() : Directory.CreateDirectory(Path.Combine(root, "decompiled", name)).FullName;
        SourceDecompiler.Decompile(Sample(name), folder, Ct);
        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            output.WriteLine($"{Path.GetRelativePath(folder, file)} {new FileInfo(file).Length}");
        output.WriteLine(File.ReadAllText(Path.Combine(folder, name + ".qc")));
        return folder;
    }

    private static float[] Bone(string[] lines, int frame, int bone)
    {
        var at = Array.IndexOf(lines, $"time {frame}");
        return [.. lines[at + 1 + bone].Split(' ').Skip(1).Select(n => float.Parse(n, CultureInfo.InvariantCulture))];
    }

    [Fact]
    public void Gate()
    {
        var folder = Decompile("gate_e2");
        var qc = File.ReadAllText(Path.Combine(folder, "gate_e2.qc"));
        Assert.Contains("$staticprop", qc);
        Assert.Contains("$surfaceprop \"plastic\"", qc);
        Assert.Contains("$collisionmodel \"gate_e2_physics.smd\"", qc);
        Assert.Contains("\t$concave", qc);
        Assert.Equal(12, File.ReadAllLines(Path.Combine(folder, "gate_e2_physics.smd")).Count(l => l == "phy"));
    }

    [Fact]
    public void Chassis()
    {
        var folder = Decompile("chassis");
        var qc = File.ReadAllText(Path.Combine(folder, "chassis.qc"));
        Assert.Contains("$lod 100", qc);
        Assert.Contains("$shadowlod", qc);
        Assert.Contains("\tblank", qc);
        Assert.Contains("prop_data", qc);
        Assert.True(File.Exists(Path.Combine(folder, "chassis_lod3.smd")));
        Assert.Equal(74, File.ReadAllLines(Path.Combine(folder, "chassis_physics.smd")).Count(l => l == "phy"));
    }

    [Fact]
    public void Launcher()
    {
        var folder = Decompile("c_homing_launcher");
        var qc = File.ReadAllText(Path.Combine(folder, "c_homing_launcher.qc"));
        Assert.Contains("activity \"ACT_VM_RELOAD\"", qc);
        Assert.Contains("{ event 5004 5 \"Glide.HomingLauncher.Draw\" }", qc);
        var reload = File.ReadAllLines(Path.Combine(folder, "c_homing_launcher_anims", "reload.smd"));
        float[] arm = [14.0948f, -0.1211f, 39.716f, -1.6484f, -0.0241f, -3.0173f];
        float[] spine = [15.0864f, -21.0357f, -45.2252f, -4.3856f, -0.6653f, -2.4117f];
        var gotArm = Bone(reload, 35, 22);
        var gotSpine = Bone(reload, 35, 0);
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(arm[i], gotArm[i], 2);
            Assert.Equal(spine[i], gotSpine[i], 2);
        }
        var fire = File.ReadAllLines(Path.Combine(folder, "c_homing_launcher_anims", "fire.smd"));
        float[] spineFire = [-8.5081f, 0.0118f, -9.9312f, -1.5708f, -1.5363f, 0];
        var got = Bone(fire, 10, 0);
        for (var i = 0; i < 6; i++)
            Assert.Equal(spineFire[i], got[i], 2);
    }
}
