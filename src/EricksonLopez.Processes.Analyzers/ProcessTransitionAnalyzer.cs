// Copyright © Erickson Lopez. MIT License.
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.Processes.Analyzers;

/// <summary>
/// Provides a Roslyn diagnostic analyzer that validates process manager definitions and state machine consistency.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProcessTransitionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the diagnostic identifier for missing initial state configuration.
    /// </summary>
    public const string DiagnosticIdMissingInitialState = "PROC001";

    /// <summary>
    /// Gets the diagnostic identifier for unhandled process transition compensation.
    /// </summary>
    public const string DiagnosticIdMissingCompensation = "PROC002";

    private static readonly DiagnosticDescriptor MissingInitialStateRule = new(
        DiagnosticIdMissingInitialState,
        "Process definition missing initial state handler",
        "Process '{0}' does not configure an initial state transition handler",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every process manager definition must define an initial state handler to initiate the saga lifecycle.");

    private static readonly DiagnosticDescriptor MissingCompensationRule = new(
        DiagnosticIdMissingCompensation,
        "Step transition missing compensation action",
        "Saga step '{0}' defines an outbound effect without a registered compensation action",
        "Reliability",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Compensating actions are recommended for all saga steps performing outbound side effects.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingInitialStateRule, MissingCompensationRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        if (namedType.TypeKind != TypeKind.Class || namedType.IsAbstract)
        {
            return;
        }

        // Check if the class is decorated with [ProcessDefinition] or [SagaDefinition]
        var isProcess = namedType.GetAttributes().Any(static a =>
            a.AttributeClass is
            {
                Name: "ProcessDefinitionAttribute" or "SagaDefinitionAttribute"
                or "ProcessDefinition" or "SagaDefinition"
            });

        if (!isProcess)
        {
            return;
        }

        // Validate that the type contains at least one handler method
        var hasHandler = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(static method => method.GetAttributes().Any(static attr =>
                attr.AttributeClass is { Name: "ProcessHandlerAttribute" or "ProcessHandler" }));

        if (!hasHandler)
        {
            var diagnostic = Diagnostic.Create(
                MissingInitialStateRule,
                namedType.Locations[0],
                namedType.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
