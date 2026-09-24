# Support Policy

Thank you for using **`EricksonLopez.Processes`**! This document outlines our support channels, documentation resources, and guidance for getting assistance.

---

## 1. Documentation Resources

Before opening an issue or asking a question, please consult the extensive technical documentation available in this repository:

- 📖 [**Architecture & Diagrams**](docs/architecture/architecture-and-diagrams.md): System architecture, execution sequence diagrams, and finite state machine models.
- 📚 [**API Reference**](docs/guides/api-reference.md): Comprehensive documentation of all public types, interfaces, identifiers, and methods.
- 🍳 [**Cookbook**](docs/guides/cookbook.md): 12 ready-to-use, verified code recipes for common process manager and saga patterns.
- 🌟 [**Showcase Guide**](docs/showcase/index.md): 11 progressive levels (Level 00 to Level 10) in `samples/EricksonLopez.Processes.Showcase`.
- 🔧 [**Troubleshooting Guide**](docs/guides/troubleshooting.md): Diagnosis and solutions for common concurrency, persistence, and state transition issues.
- ⚡ [**Performance Guide**](docs/guides/performance-guide.md): Zero-allocation guidelines, CAS tuning, and Native AOT optimization.
- 🔄 [**Migration Guide**](docs/guides/migration-guide.md): Multi-version schema migrations using `ProcessStateMigrationPipeline`.
- ❓ [**Frequently Asked Questions (FAQ)**](docs/guides/faq.md): Conceptual differences, design choices, and common inquiries.

---

## 2. Community Support Channels

| Channel | Purpose | Response Time |
| :--- | :--- | :--- |
| [**GitHub Issues**](https://github.com/ericksonlopezf/dotnet-processes/issues) | Bug reports, unexpected behaviors, and confirmed regressions. | Best effort / community driven |
| [**GitHub Discussions**](https://github.com/ericksonlopezf/dotnet-processes/discussions) | Architectural guidance, Q&A, design feedback, and best practices. | Best effort / community driven |
| [**Maintainer Direct Contact**](mailto:ericksonlopezf@gmail.com) | Direct inquiries, project sponsorship, or ecosystem questions. | Best effort |
| [**Security Reports**](SECURITY.md) | Confidential reporting of potential vulnerabilities. | Within 48 hours |

---

## 3. Creating Effective Bug Reports

When opening a bug report via [GitHub Issues](https://github.com/ericksonlopezf/dotnet-processes/issues/new/choose):

1. **Check Existing Issues**: Verify whether a similar issue has already been reported or solved.
2. **Provide a Minimal Reproducible Example (MRE)**: Include a concise C# code snippet or test case reproducing the behavior.
3. **Specify Environment Details**:
   - .NET SDK version (`dotnet --version`, e.g. `10.0.100`)
   - Target framework (`net10.0`)
   - Storage provider (`PostgreSQL`, `SQL Server`, `SQLite`, `MySQL`, `MariaDB`, `Oracle`, or `InMemory`)
   - Operating system and runtime mode (JIT vs. Native AOT).

---

## 4. Enterprise & Commercial Support

`EricksonLopez.Processes` is distributed as open-source software under the MIT License and is maintained on a community-driven, best-effort basis. Formal commercial Service Level Agreements (SLAs), 24/7 incident response, or custom enterprise support contracts are not currently provided. Community members and enterprise users are encouraged to submit pull requests, open discussions, and participate in code reviews.
