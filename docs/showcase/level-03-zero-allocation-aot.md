# Level 03: Zero-Allocation & Native AOT Architecture

## 1. Native AOT & Trimming Support
`EricksonLopez.Processes` relies exclusively on compile-time source generation and `System.Text.Json` source generator contexts (`JsonSerializerContext`) for state serialization:

```csharp
[JsonSerializable(typeof(OrderProcessState))]
public partial class ProcessSerializationContext : JsonSerializerContext { }
```

---

## 2. Allocation Optimization
By leveraging `ValueTask`, pooled execution contexts, and zero-allocation transition delegates, process evaluations execute without allocating heap memory on the hot path.

### Benchmark Comparison

| Operation | Reflection Workflow | EricksonLopez.Processes |
|---|---|---|
| State Transition Execution | 1,850 B | **0 B (ValueTask)** |
| Durable State Deserialization | 2,100 B | **48 B (Direct Buffer)** |
| Saga Compensation Dispatch | 3,400 B | **0 B (Static Delegate)** |
