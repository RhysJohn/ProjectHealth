using System.Runtime.CompilerServices;
using ProjectHealth.Checks.CodeAnalysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectHealth.Extensions;

namespace ProjectHealth.Checks.CodeAnalysis;

public class MaintainabilityIndexCheck
{
    private const int RedMaintainabilityIndexUpperBound = 9;

    private const int YellowMaintainabilityIndexUpperBound = 19;

    private const int GreenMaintainabilityIndexUpperBound = 100;

    public async Task RunAsync(CodeAnalysisCheckInfo analysisCheckInfo)
    {
        Console.WriteLine("***** Starting Maintainability Index Check *****");

        var highLevelResults = new List<MaintainabilityIndexProjectResult>();
        foreach (var project in analysisCheckInfo.Solution.Projects)
        {
            var projectDocumentsMaintainabilityIndexes = new List<MaintainabilityIndexDocumentResult>();

            foreach (var document in project.Documents)
            {
                var syntaxTree = document.GetSyntaxTreeAsync().Result;
                var root = await syntaxTree.GetRootAsync();

                var halsteadVolume = CalculateHalsteadVolume(root);
                var cyclomaticComplexity = root.CalculateCyclomaticComplexity();

                var linesOfCodeCounter = new StatementWalker(CancellationToken.None);
                linesOfCodeCounter.Visit(root);
                var linesOfCode = linesOfCodeCounter.StatementCount;

                if (linesOfCode > 0)
                {
                    var maintainabilityIndex = 171 - 5.2 * Math.Log(halsteadVolume) - 0.23 * cyclomaticComplexity -
                                               16.2 * Math.Log(linesOfCode);

                    projectDocumentsMaintainabilityIndexes.Add(
                        new MaintainabilityIndexDocumentResult(document, maintainabilityIndex));
                }
            }

            highLevelResults.Add(
                new MaintainabilityIndexProjectResult(project, projectDocumentsMaintainabilityIndexes));
        }

        LogResults(highLevelResults);
    }

    private void LogResults(List<MaintainabilityIndexProjectResult> highLevelResults)
    {
        foreach (var highLevelResult in highLevelResults)
        {
            var averageDocumentMaintainabilityIndex =
                highLevelResult.MaintainabilityIndexResults.Average(r => r.MaintainabilityIndex);

            Console.WriteLine(
                $"Project: {highLevelResult.Project.Name}; Documents Count: {highLevelResult.MaintainabilityIndexResults.Count()}");

            Console.WriteLine(
                $"Average Document Maintainability Index: {MakeMaintainabilityIndexReadable(averageDocumentMaintainabilityIndex)}");

            var yellowMaintainableDocuments = highLevelResult.MaintainabilityIndexResults.Where(
                    maintainabilityIndexDocument =>
                        maintainabilityIndexDocument.MaintainabilityIndex is > RedMaintainabilityIndexUpperBound
                            and <= YellowMaintainabilityIndexUpperBound)
                .ToList();

            if (yellowMaintainableDocuments.Any())
            {
                var averageYellowMaintainableDocuments =
                    yellowMaintainableDocuments.Average(document => document.MaintainabilityIndex);
                Console.WriteLine(
                    $"Yellow Maintainability Index Documents Count: {yellowMaintainableDocuments.Count()}; Average Yellow Document Maintainability Index: {MakeMaintainabilityIndexReadable(averageYellowMaintainableDocuments)}");

                foreach (var yellowMaintainableDocument in yellowMaintainableDocuments)
                {
                    Console.WriteLine(
                        $"Yellow Maintainability Index Document: {yellowMaintainableDocument.Document.Name}; Maintainability Index: {MakeMaintainabilityIndexReadable(yellowMaintainableDocument.MaintainabilityIndex)}");
                }
            }

            var redMaintainableDocuments = highLevelResult.MaintainabilityIndexResults.Where(
                documentMaintainabilityResult =>
                    documentMaintainabilityResult.MaintainabilityIndex <= RedMaintainabilityIndexUpperBound).ToList();

            if (redMaintainableDocuments.Any())
            {
                var averageRedMaintainableDocuments =
                    redMaintainableDocuments.Average(document => document.MaintainabilityIndex);
                Console.WriteLine(
                    $"Red Maintainability Index Documents Count: {redMaintainableDocuments.Count()}; Average Red Document Maintainability Index: {MakeMaintainabilityIndexReadable(averageRedMaintainableDocuments)}");

                foreach (var redMaintainableDocument in redMaintainableDocuments)
                {
                    Console.WriteLine(
                        $"Red Maintainability Index Document: {redMaintainableDocument.Document.Name}; Maintainability Index: {MakeMaintainabilityIndexReadable(redMaintainableDocument.MaintainabilityIndex)}");
                }
            }
        }
    }

    private string MakeMaintainabilityIndexReadable(double maintainabilityIndex)
    {
        if (maintainabilityIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maintainabilityIndex),
                "Maintainability Index cannot be negative");
        }

        switch (maintainabilityIndex)
        {
            case <= RedMaintainabilityIndexUpperBound:
                return $"Red ({maintainabilityIndex:0.00})";
            case <= YellowMaintainabilityIndexUpperBound:
                return $"Yellow ({maintainabilityIndex:0.00})";
            case > YellowMaintainabilityIndexUpperBound:
                return $"Green ({maintainabilityIndex:0.00})";
            default:
                throw new ArgumentOutOfRangeException(nameof(maintainabilityIndex),
                    "Maintainability Index out of bounds of accepted values");
        }
    }

    private double CalculateHalsteadVolume(SyntaxNode root)
    {
        // Calculate Halstead Volume
        var operators = root.DescendantTokens()
            .Where(t => t.IsKind(SyntaxKind.PlusToken) || t.IsKind(SyntaxKind.MinusToken));
        var operands = root.DescendantNodes().OfType<IdentifierNameSyntax>();

        int totalOperators = operators.Count();
        int totalOperands = operands.Count();
        int vocabularySize = totalOperators + totalOperands;
        int programLength = totalOperators + totalOperands;

        // Calculate Halstead Volume
        double halsteadVolume = programLength * Math.Log2(vocabularySize);
        return halsteadVolume;
    }
}

class StatementWalker : CSharpSyntaxWalker
{
    private CancellationToken _cancellationToken;
    private bool _nullChecksFinished;

    public int StatementCount { get; private set; }

    public StatementWalker(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    public override void Visit(SyntaxNode node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsStatement(node))
        {
            StatementCount++;
        }

        base.Visit(node);
    }

    private bool IsStatement(SyntaxNode node)
    {
        return !node.IsMissing && node is StatementSyntax statement && !IsExcludedStatement(statement);
    }

    private bool IsExcludedStatement(StatementSyntax node)
    {
        var isExcludedNodeType = node is BlockSyntax || node is LabeledStatementSyntax ||
                                 node is LocalFunctionStatementSyntax;
        if (isExcludedNodeType)
        {
            return true;
        }

        if (!_nullChecksFinished)
        {
            // Argument null checks will be at the top of a method, so once we're past them we can stop checking
            var isArgumentNullCheck = node.IsArgumentNullCheck();
            if (!isArgumentNullCheck)
            {
                _nullChecksFinished = true;
            }

            return isArgumentNullCheck;
        }

        return false;
    }
}