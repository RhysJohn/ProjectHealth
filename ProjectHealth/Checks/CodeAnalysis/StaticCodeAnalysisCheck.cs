using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using ProjectHealth.Checks.CodeAnalysis.Models;

namespace ProjectHealth.Checks.CodeAnalysis;

public class StaticCodeAnalysisCheck
{
    public async Task RunAsync(CodeAnalysisCheckInfo analysisCheckInfo)
    {
        Console.WriteLine("***** Starting Static Code Analysis Check *****");
        var diagnosticResults = new List<Diagnostic>();

        foreach (var project in analysisCheckInfo.Solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                continue;
            }

            foreach (var diagnostic in compilation.GetDiagnostics())
            {
                diagnosticResults.Add(diagnostic);
            }
        }

        var groupedDiagnosticResult = diagnosticResults.GroupBy(result => result.Id);

        Console.WriteLine($"Number of violations from analysis: {diagnosticResults.Count}");

        foreach (var grouping in groupedDiagnosticResult)
        {
            var firstErrorInGrouping = grouping.First();
            Console.WriteLine(
                $"Code ID: {grouping.Key}; Info: {firstErrorInGrouping.GetMessage()}; IsWarningAsError: {firstErrorInGrouping.IsWarningAsError} - Count: {grouping.Count()}");
        }
    }
}