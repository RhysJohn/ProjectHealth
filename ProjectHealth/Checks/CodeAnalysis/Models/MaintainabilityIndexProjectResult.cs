using Microsoft.CodeAnalysis;

namespace ProjectHealth.Checks.CodeAnalysis.Models;

public record MaintainabilityIndexProjectResult(Project Project,
    IEnumerable<MaintainabilityIndexDocumentResult> MaintainabilityIndexResults);