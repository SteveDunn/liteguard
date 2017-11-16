using System;
using System.Threading.Tasks;
using static Bullseye.Targets;
using static SimpleExec.Command;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        Add("default", DependsOn("pack", "test"));

        Add(
            "build",
            () => RunAsync(
                "dotnet",
                "build LiteGuard.sln /property:Configuration=Release /maxcpucount " +
                    $"/fl /flp:LogFile=build.log;Verbosity=Detailed;PerformanceSummary " +
                    $"/bl:build.binlog"));

        Add(
            "pack",
            DependsOn("build"),
            async () =>
            {
                foreach (var nuspec in new[] { "LiteGuard.nuspec", "LiteGuard.Source.nuspec", })
                {
                    Environment.SetEnvironmentVariable("NUSPEC_FILE", nuspec, EnvironmentVariableTarget.Process);
                    await RunAsync("dotnet", $"pack src/LiteGuard --configuration Release --no-build");
                }
            });

        Add(
            "test",
            DependsOn("build"),
            () => RunAsync("dotnet", $"xunit -configuration Release -nobuild", "./tests/LiteGuardTests"));

        return await RunAsync(args);
    }
}
