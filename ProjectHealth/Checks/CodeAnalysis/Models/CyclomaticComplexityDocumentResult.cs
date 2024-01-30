using Microsoft.CodeAnalysis;

namespace ProjectHealth.Checks.CodeAnalysis.Models;

public record CyclomaticComplexityDocumentResult(Document Document, int ComplexityScore);