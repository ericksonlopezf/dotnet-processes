# ADR-038: Source Generator DI Extension (`AddGeneratedProcesses`)

## Status
**Accepted**

---

## Context
The Roslyn Incremental Generator (`ProcessSourceGenerator`, ADR-018) originally generated `GeneratedProcessRegistry.g.cs` containing `RegisterDiscoveredProcesses(ProcessRegistry registry)` and `CreateRegistry()`.

To maximize Developer Experience (DX) and eliminate startup boilerplate, the source generator was extended to emit a companion extension method `AddGeneratedProcesses(this IServiceCollection services)` that registers the pre-populated `ProcessRegistry` directly into the DI container at compile time.

---

## Problem
How to generate an `IServiceCollection` extension method from a Roslyn Source Generator project (targeting `netstandard2.0`) without:
1. Adding runtime DI dependencies to the Roslyn analyzer/generator assembly itself?
2. Compromising Native AOT compliance in generated code?
3. Creating brittle couplings between compiler analyzers and runtime DI packages?

---

## Decision
Emit `GeneratedProcessRegistryExtensions.g.cs` referencing Microsoft Dependency Injection types via `global::Microsoft.Extensions.DependencyInjection.IServiceCollection` fully qualified type names.

The emitted extension method lives in the compilation context of the consuming project:

```csharp
public static class GeneratedProcessRegistryExtensions
{
    public static IServiceCollection AddGeneratedProcesses(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ProcessRegistry>(static _ =>
        {
            var registry = new ProcessRegistry();
            GeneratedProcessRegistry.RegisterDiscoveredProcesses(registry);
            return registry;
        });

        services.AddSingleton<IProcessRegistry>(
            static sp => sp.GetRequiredService<ProcessRegistry>());

        return services;
    }
}
```

---

## Rationale
- **100% Native AOT-Safe**: Uses `AddSingleton<T>` with a static factory delegate, avoiding runtime reflection or `Activator.CreateInstance`.
- **Zero Boilerplate**: Consuming applications configure process discovery with a single line: `services.AddGeneratedProcesses();`.
- **Decoupled Generator Build**: The Roslyn generator assembly remains a pure `netstandard2.0` analyzer with zero runtime package dependencies.

---

## Consequences
- **Positive**:
  - Full completion of ADR-018 compile-time DI promises.
  - Ergonomic single-line application startup.
- **Negative**:
  - The emitted extension compiles successfully only if the consumer project references `Microsoft.Extensions.DependencyInjection.Abstractions`. Consuming projects without DI can use `GeneratedProcessRegistry.CreateRegistry()` directly.
