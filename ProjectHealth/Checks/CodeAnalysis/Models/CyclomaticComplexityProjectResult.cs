using Microsoft.CodeAnalysis;

namespace ProjectHealth.Checks.CodeAnalysis.Models;

public record CyclomaticComplexityProjectResult(Project Project,
    IEnumerable<CyclomaticComplexityDocumentResult> ComplexityDocumentResults);