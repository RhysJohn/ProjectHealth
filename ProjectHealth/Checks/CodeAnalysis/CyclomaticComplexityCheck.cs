using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectHealth.Checks.CodeAnalysis.Models;
using ProjectHealth.Extensions;

namespace ProjectHealth.Checks.CodeAnalysis;

public class CyclomaticComplexityCheck
{
    private const int LowComplexityUpperBound = 10;
    private const int ModerateComplexityUpperBound = 20;

    public async Task RunAsync(CodeAnalysisCheckInfo analysisCheckInfo)
    {
        Console.WriteLine("***** Starting Cyclomatic Complexity Check *****");

        var highLevelResults = new List<CyclomaticComplexityProjectResult>();
        foreach (var project in analysisCheckInfo.Solution.Projects)
        {
            var projectDocumentsComplexityScores = new List<CyclomaticComplexityDocumentResult>();
            foreach (var document in project.Documents)
            {
                var syntaxTree = document.GetSyntaxTreeAsync().Result;

                if (syntaxTree is null)
                {
                    // TODO: add some type of logging out that unable to calculate the complexity for this particular document
                    continue;
                }

                var syntaxNode = await syntaxTree.GetRootAsync();
                var complexity = syntaxNode.CalculateCyclomaticComplexity();

                projectDocumentsComplexityScores.Add(new CyclomaticComplexityDocumentResult(document, complexity));
            }

            highLevelResults.Add(new CyclomaticComplexityProjectResult(project, projectDocumentsComplexityScores));
        }

        LogResults(highLevelResults);
    }

    private void LogResults(List<CyclomaticComplexityProjectResult> highLevelResults)
    {
        foreach (var highLevelResult in highLevelResults)
        {
            var overallProjectComplexityScore = highLevelResult.ComplexityDocumentResults.Sum(r => r.ComplexityScore);
            var averageDocumentComplexity = highLevelResult.ComplexityDocumentResults.Average(r => r.ComplexityScore);

            Console.WriteLine(
                $"Project: {highLevelResult.Project.Name}; Documents Count: {highLevelResult.ComplexityDocumentResults.Count()}");

            Console.WriteLine(
                $"Overall Complexity Rating: {overallProjectComplexityScore}; Average Document Complexity: {averageDocumentComplexity:0.00}");

            var moderateComplexityDocuments = highLevelResult.ComplexityDocumentResults.Where(
                    documentComplexityResult =>
                        documentComplexityResult.ComplexityScore > LowComplexityUpperBound &&
                        documentComplexityResult.ComplexityScore <= ModerateComplexityUpperBound)
                .ToList();

            if (moderateComplexityDocuments.Any())
            {
                var averageModerateComplexityDocuments =
                    moderateComplexityDocuments.Average(document => document.ComplexityScore);
                Console.WriteLine(
                    $"Moderate Complexity Documents Count: {moderateComplexityDocuments.Count()}; Average Moderate Document Complexity: {averageModerateComplexityDocuments:0.00}");

                foreach (var moderateComplexityDocument in moderateComplexityDocuments)
                {
                    Console.WriteLine(
                        $"Moderate Complexity Document: {moderateComplexityDocument.Document.Name}; Complexity Score: {moderateComplexityDocument.ComplexityScore}");
                }
            }

            var highComplexityDocuments = highLevelResult.ComplexityDocumentResults.Where(documentComplexityResult =>
                documentComplexityResult.ComplexityScore > ModerateComplexityUpperBound).ToList();

            if (highComplexityDocuments.Any())
            {
                var averageHighComplexityDocuments =
                    highComplexityDocuments.Average(document => document.ComplexityScore);
                Console.WriteLine(
                    $"High Complexity Documents Count: {highComplexityDocuments.Count()}; Average High Document Complexity: {averageHighComplexityDocuments:0.00}");

                foreach (var highComplexityDocument in highComplexityDocuments)
                {
                    Console.WriteLine(
                        $"High Complexity Document: {highComplexityDocument.Document.Name}; Complexity Score: {highComplexityDocument.ComplexityScore}");
                }
            }
        }
    }
}