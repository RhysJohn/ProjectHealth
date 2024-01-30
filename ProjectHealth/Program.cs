// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using ProjectHealth.Checks;
using ProjectHealth.Checks.CodeAnalysis;
using ProjectHealth.Checks.CodeAnalysis.Models;

var solutionPath = @"C:\Git\Audacia.Azure\Audacia.Azure.sln";

if (!MSBuildLocator.IsRegistered)
{
    var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
    MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
}

var workspace = MSBuildWorkspace.Create();

var solution = await workspace.OpenSolutionAsync(solutionPath);

if (solution is null)
{
    throw new ArgumentException("Unable to find solution to analyze");
}

var codeAnalysisCheckInfo = new CodeAnalysisCheckInfo(solution);

var staticCodeAnalysisCheck = new StaticCodeAnalysisCheck();

await staticCodeAnalysisCheck.RunAsync(codeAnalysisCheckInfo);

var cyclomaticComplexityCheck = new CyclomaticComplexityCheck();

await cyclomaticComplexityCheck.RunAsync(codeAnalysisCheckInfo);

var maintainabilityIndexCheck = new MaintainabilityIndexCheck();

await maintainabilityIndexCheck.RunAsync(codeAnalysisCheckInfo);