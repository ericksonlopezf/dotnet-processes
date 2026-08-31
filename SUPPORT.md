# Support Policy

Thank you for using **`EricksonLopez.Processes`**! This document outlines our support channels, documentation resources, and guidance for getting assistance.

---

## 1. Documentation Resources

Before opening an issue or asking a question, please consult the extensive technical documentation available in this repository:

- 📖 [**Architecture & Diagrams**](docs/architecture-and-diagrams.md): System architecture, execution sequence diagrams, and finite state machine models.
- 📚 [**API Reference**](docs/api-reference.md): Comprehensive documentation of all public types, interfaces, identifiers, and methods.
- 🍳 [**Cookbook**](docs/cookbook.md): 12 ready-to-use, verified code recipes for common process manager and saga patterns.
- 🌟 [**Showcase Guide**](docs/showcase-guide.md): 11 progressive levels (Level 00 to Level 10) in `samples/EricksonLopez.Processes.Showcase`.
- 🔧 [**Troubleshooting Guide**](docs/troubleshooting.md): Diagnosis and solutions for common concurrency, persistence, and state transition issues.
- ⚡ [**Performance Guide**](docs/performance-guide.md): Zero-allocation guidelines, CAS tuning, and Native AOT optimization.
- 🔄 [**Migration Guide**](docs/migration-guide.md): Multi-version schema migrations using `ProcessStateMigrationPipeline`.
- ❓ [**Frequently Asked Questions (FAQ)**](docs/faq.md): Conceptual differences, design choices, and common inquiries.

---

## 2. Community Support Channels

| Channel | Purpose | Response Time |
| :--- | :--- | :--- |
| [**GitHub Issues**](https://github.com/ericksonlopez/dotnet-processes/issues) | Bug reports, unexpected behaviors, and confirmed regressions. | Best effort / community driven |
| [**GitHub Discussions**](https://github.com/ericksonlopez/dotnet-processes/discussions) | Architectural guidance, Q&A, design feedback, and best practices. | Best effort / community driven |
| [**Security Reports**](SECURITY.md) | Confidential reporting of potential vulnerabilities. | Within 48 hours |

---

## 3. Creating Effective Bug Reports

When opening a bug report via [GitHub Issues](https://github.com/ericksonlopez/dotnet-processes/issues/new/choose):

1. **Check Existing Issues**: Verify whether a similar issue has already been reported or solved.
2. **Provide a Minimal Reproducible Example (MRE)**: Include a concise C# code snippet or test case reproducing the behavior.
3. **Specify Environment Details**:
   - .NET SDK version (`dotnet --version`)
   - Target framework (`net10.0`)
   - Storage provider (`PostgreSQL`, `SQL Server`, `SQLite`, `MySQL`, `MariaDB`, `Oracle`, or `InMemory`)
   - Operating system and runtime mode (JIT vs. Native AOT).
