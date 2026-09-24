# Showcase — Level 08: Customization & Extensibility

**Level 08** covers replacing and customizing core components: custom persistence stores, alternative binary serializers, saga snapshot repositories, and bidirectional mapping with `ProcessStateRecord`.

---

## Covered Components

1. **Custom Store (`IProcessStore<TState>`)**:
   - Custom persistence implementations over any storage engine or document store.
   - Managing `ProcessSaveResult` and evaluating CAS revisions.

2. **Custom Serializer (`IProcessStateSerializer<TState>`)**:
   - Implementing alternative binary serialization formats (e.g. MessagePack, Protocol Buffers).
   - Methods: `Serialize(TState)` -> `byte[]`, `Deserialize(ReadOnlySpan<byte>)` -> `TState`.

3. **Snapshot Repository (`ISagaSnapshotRepository<TState>`)**:
   - Methods: `SaveSnapshotAsync`, `GetLatestSnapshotAsync`.
   - Optimizes long-running sagas using periodic snapshots to prevent costly replay recomputations.

4. **Mapping with `ProcessStateRecord`**:
   - Flat relational record with all persistence columns (`ProcessId`, `ProcessType`, `Version`, `Status`, `Revision`, `CorrelationId`, `StatePayload`, `CreatedAt`, `UpdatedAt`, `CompletedAt`).
   - Bidirectional mapping: `ProcessInstance<TState>` ↔ `ProcessStateRecord`.

---

## Bidirectional Mapping

```mermaid
flowchart LR
    INST["ProcessInstance&lt;TState&gt; (Domain)"] -->|Serializes State to byte[]| ADAPT["Storage Adapter"]
    ADAPT -->|Creates| REC["ProcessStateRecord (Relational Row)"]
    REC -->|Deserializes State| INST
```

---

## Showcase Execution

Executable code:
- `samples/EricksonLopez.Processes.Showcase/Level08_Customization/Level08CustomStoreDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level08_Customization/Level08CustomSerializerDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level08_Customization/Level08SnapshotRepositoryDemo.cs`
- `samples/EricksonLopez.Processes.Showcase/Level08_Customization/Level08ProcessStateRecordDemo.cs`

```bash
dotnet run --project samples/EricksonLopez.Processes.Showcase -- --level=8
```
