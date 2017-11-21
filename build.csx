#r "nuget:SimpleExec, 2.0.0"

#load "packages/simple-targets-csx.6.0.0/contentFiles/csx/any/simple-targets.csx"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static SimpleTargets;
using static SimpleExec.Command;

// version
var versionSuffix = Environment.GetEnvironmentVariable("VERSION_SUFFIX") ?? "";
var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "000000";
var buildNumberSuffix = versionSuffix == "" ? "" : "-build" + buildNumber;
var version = File.ReadAllText("src/LiteGuard/LiteGuard.csproj")
    .Split(new[] { "<Version>" }, 2, StringSplitOptions.RemoveEmptyEntries)[1]
    .Split(new[] { "</Version>" }, 2, StringSplitOptions.None).First() + versionSuffix + buildNumberSuffix;

// locations
var logs = "./artifacts/logs";
var output = "./artifacts/output";
var nuget = ".nuget/v4.3.0/NuGet.exe";

// targets
var targets = new TargetDictionary();

targets.Add("default", DependsOn("pack", "test"));

targets.Add("logs", () => Directory.CreateDirectory(logs));

targets.Add(
    "build",
    DependsOn("logs"),
    () => Run(
        "dotnet",
        "build LiteGuard.sln /property:Configuration=Release /nologo /maxcpucount " +
            $"/fl /flp:LogFile={logs}/build.log;Verbosity=Detailed;PerformanceSummary " +
            $"/bl:{logs}/build.binlog"));

targets.Add("output", () => Directory.CreateDirectory(output));

targets.Add(
    "pack",
    DependsOn("build", "output"),
    () =>
    {
        foreach (var nuspec in new[] { "./src/LiteGuard/LiteGuard.nuspec", "./src/LiteGuard/LiteGuard.Source.nuspec", })
        {
            Run(nuget, $"pack {nuspec} -Version {version} -OutputDirectory {output} -NoPackageAnalysis");
        }
    });

targets.Add("test", DependsOn("build"), () => Run("dotnet", $"xunit -configuration Release -nobuild", "./tests/LiteGuardTests"));

Run(Args, targets);
