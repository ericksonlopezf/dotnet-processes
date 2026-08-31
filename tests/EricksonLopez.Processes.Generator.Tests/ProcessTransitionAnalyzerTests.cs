// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

#pragma warning disable IL3000
namespace EricksonLopez.Processes.Generator.Tests;

[Trait("Category", "Analyzers")]
public class ProcessTransitionAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var baseRefs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(ProcessDefinitionAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ProcessTransitionAnalyzer).Assembly.Location)
            })
            .Distinct()
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            baseRefs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new ProcessTransitionAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public void SupportedDiagnostics_ShouldContainRequiredRules()
    {
        var analyzer = new ProcessTransitionAnalyzer();
        var diagnostics = analyzer.SupportedDiagnostics;

        diagnostics.Should().HaveCount(2);

        var missingInitial = diagnostics.Single(d => d.Id == ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
        missingInitial.Title.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Process definition missing initial state handler");
        missingInitial.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Process '{0}' does not configure an initial state transition handler");
        missingInitial.Category.Should().Be("Design");
        missingInitial.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        missingInitial.IsEnabledByDefault.Should().BeTrue();
        missingInitial.Description.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Every process manager definition must define an initial state handler to initiate the saga lifecycle.");

        var missingComp = diagnostics.Single(d => d.Id == ProcessTransitionAnalyzer.DiagnosticIdMissingCompensation);
        missingComp.Title.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Step transition missing compensation action");
        missingComp.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Saga step '{0}' defines an outbound effect without a registered compensation action");
        missingComp.Category.Should().Be("Reliability");
        missingComp.DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
        missingComp.IsEnabledByDefault.Should().BeTrue();
        missingComp.Description.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Compensating actions are recommended for all saga steps performing outbound side effects.");
    }

    [Fact]
    public async Task ProcessDefinition_WithoutHandlerMethod_ShouldReportPROC001()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""order.flow"", 1)]
public class OrderProcess
{
    public int StateField = 0;
    public string Name { get; set; } = string.Empty;
    public void UnannotatedMethod() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Process 'OrderProcess' does not configure an initial state transition handler");
    }

    [Fact]
    public async Task ProcessDefinitionAttribute_WithoutHandlerMethod_ShouldReportPROC001()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinitionAttribute(""order.flow"", 1)]
public class FullNameAttributeProcess
{
    public void UnannotatedMethod() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task SagaDefinition_WithoutHandlerMethod_ShouldReportPROC001()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[SagaDefinition(""order.saga"", 1)]
public class OrderSaga
{
    public void PlainMethod() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Should().Be("Process 'OrderSaga' does not configure an initial state transition handler");
    }

    [Fact]
    public async Task SagaDefinitionAttribute_WithoutHandlerMethod_ShouldReportPROC001()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[SagaDefinitionAttribute(""order.saga"", 1)]
public class FullNameAttributeSaga
{
    public void PlainMethod() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task ProcessDefinition_WithHandlerMethod_ShouldNotReportDiagnostics()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""order.flow"", 1)]
public class OrderProcess
{
    [ProcessHandler]
    public void HandleOrder() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDefinition_WithHandlerMethodFullAttributeName_ShouldNotReportDiagnostics()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""order.flow"", 1)]
public class OrderProcess
{
    [ProcessHandlerAttribute]
    public void HandleOrder() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDefinition_WithMultipleMethods_WhenSecondHasHandler_ShouldNotReportDiagnostics()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""order.flow"", 1)]
public class OrderProcess
{
    [Obsolete]
    public void Helper() { }

    [ProcessHandler]
    public void HandleOrder() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonClassOrAbstractTypes_ShouldNotReportDiagnostics()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""abstract.flow"", 1)]
public abstract class AbstractProcess
{
}

[ProcessDefinition(""struct.flow"", 1)]
public struct StructProcess
{
}

[ProcessDefinition(""interface.flow"", 1)]
public interface IInterfaceProcess
{
}

[ProcessDefinition(""enum.flow"", 1)]
public enum EnumProcess
{
    None
}

public class NonProcessClass
{
    public void DoSomething() { }
}

[Obsolete]
public class ObsoleteOnlyClass
{
    public void DoSomething() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDefinition_WithMultipleClassAttributes_ShouldReportPROC001()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[Obsolete]
[ProcessDefinition(""multi.attr.flow"", 1)]
public class MultiAttrProcess
{
    public void DoSomething() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
    }

    [Fact]
    public async Task ProcessDefinition_ExactShortAttributeName_WithoutHandler_ShouldReportPROC001()
    {
        const string source = @"
using System;

namespace MyTest;

[AttributeUsage(AttributeTargets.Class)]
public class ProcessDefinition : Attribute { }

[ProcessDefinition]
public class ExactShortNameProcess
{
    public void DoSomething() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
    }

    [Fact]
    public async Task SagaDefinition_ExactShortAttributeName_WithoutHandler_ShouldReportPROC001()
    {
        const string source = @"
using System;

namespace MyTest;

[AttributeUsage(AttributeTargets.Class)]
public class SagaDefinition : Attribute { }

[SagaDefinition]
public class ExactShortNameSaga
{
    public void DoSomething() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ProcessTransitionAnalyzer.DiagnosticIdMissingInitialState);
    }

    [Fact]
    public async Task ProcessHandler_ExactShortAttributeName_WithHandler_ShouldNotReportDiagnostics()
    {
        const string source = @"
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[AttributeUsage(AttributeTargets.Method)]
public class ProcessHandler : Attribute { }

[ProcessDefinition(""test.flow"", 1)]
public class ExactShortHandlerProcess
{
    [ProcessHandler]
    public void Handle() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task GeneratedCode_WithoutHandler_ShouldBeIgnored()
    {
        const string source = @"// <auto-generated/>
using System;
using EricksonLopez.Processes.Abstractions;

namespace MyTest;

[ProcessDefinition(""generated.flow"", 1)]
public class GeneratedProcess
{
    public void UnannotatedMethod() { }
}
";
        var diagnostics = await RunAnalyzerAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Initialize_ShouldEnableConcurrentExecution_AndRegisterSymbolAction()
    {
        var analyzer = new ProcessTransitionAnalyzer();
        var context = new RecordingAnalysisContext();

        analyzer.Initialize(context);

        context.ConcurrentExecutionEnabled.Should().BeTrue();
        context.GeneratedCodeFlags.Should().Be(GeneratedCodeAnalysisFlags.None);
        context.RegisteredSymbolKinds.Should().Contain(SymbolKind.NamedType);
        context.RegisteredAction.Should().NotBeNull();
    }

    private sealed class RecordingAnalysisContext : AnalysisContext
    {
        public bool ConcurrentExecutionEnabled { get; private set; }
        public GeneratedCodeAnalysisFlags GeneratedCodeFlags { get; private set; }
        public Action<SymbolAnalysisContext>? RegisteredAction { get; private set; }
        public ImmutableArray<SymbolKind> RegisteredSymbolKinds { get; private set; }

        public override void EnableConcurrentExecution() => ConcurrentExecutionEnabled = true;

        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags flags) =>
            GeneratedCodeFlags = flags;

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            RegisteredAction = action;
            RegisteredSymbolKinds = symbolKinds;
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) { }
        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) { }
        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action) { }
        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action) { }
        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) { }
        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) { }
        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) { }
        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) { }
        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) { }
        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) { }
        public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action) { }
    }
}
#pragma warning restore IL3000
