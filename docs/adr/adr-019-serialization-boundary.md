# ADR-019: Serialization Boundary and System.Text.Json Integration

## Context
Process state must be serialized to byte arrays, strings, or JSON documents for persistence in databases.

## Problem
How should serialization be structured to prevent coupling the core library to a specific JSON serializer while remaining fully Native AOT compliant?

## Options
1. Force a hard dependency on `System.Text.Json` or `Newtonsoft.Json` directly in `EricksonLopez.Processes.Abstractions`.
2. Define a clean serializer abstraction `IProcessStateSerializer<TState>` in Abstractions, and provide an optional `EricksonLopez.Processes.SystemTextJson` package utilizing `JsonSerializerContext` source generation.

## Decision
We adopt **Option 2: Abstraction with dedicated `SystemTextJson` AOT package**.

- `IProcessStateSerializer<TState>` defines `byte[] Serialize(TState state)` and `TState Deserialize(ReadOnlySpan<byte> bytes)`.
- `EricksonLopez.Processes.SystemTextJson` provides ready-to-use helpers using `JsonTypeInfo<TState>` and `JsonSerializerContext` for zero-reflection, trimming-safe JSON serialization.

## Rationale
- Allows consumers to use Protobuf, MessagePack, MemoryPack, or System.Text.Json.
- Keeps core abstractions dependency-free.

## Consequences
- Consumers choose their serialization format cleanly.

## Rejected Alternatives
- Hard-coupling core to reflection-based `JsonSerializer.Deserialize(typeof(T))`.
