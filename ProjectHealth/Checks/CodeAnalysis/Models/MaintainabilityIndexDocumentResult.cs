using Microsoft.CodeAnalysis;

namespace ProjectHealth.Checks.CodeAnalysis.Models;

public record MaintainabilityIndexDocumentResult(Document Document, double MaintainabilityIndex);