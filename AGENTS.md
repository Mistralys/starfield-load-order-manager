# AGENTS.md — Starfield Load Order Keeper

> Operating manual for AI agents entering this codebase.
> Read this file first. Follow its directives. Do not deviate.

---

## 1. Project Manifest — Start Here

The Project Manifest is the **canonical source of truth** for this codebase. Read it before touching any source file.

**Location:** `Docs/Agents/project-manifest/`

| Document | Purpose |
|----------|---------|
| [README.md](Docs/Agents/project-manifest/README.md) | Domain context, navigation index, quick reference |
| [tech-stack.md](Docs/Agents/project-manifest/tech-stack.md) | Runtime, libraries, frameworks, architectural patterns |
| [file-tree.md](Docs/Agents/project-manifest/file-tree.md) | Complete directory and file structure |
| [data-flows.md](Docs/Agents/project-manifest/data-flows.md) | Application workflows and coordinator interactions |
| [constraints.md](Docs/Agents/project-manifest/constraints.md) | Rules, guarantees, and system invariants |
| [localization.md](Docs/Agents/project-manifest/localization.md) | Zero-hardcoding localization architecture and translation rules |
| [file-formats.md](Docs/Agents/project-manifest/file-formats.md) | Encoding rules, file format specs, I/O conventions |
| [api-surface.md](Docs/Agents/project-manifest/api-surface.md) | Public signatures for all coordinators, models, services, ViewModels, views, and converters |
| [ui-design.md](Docs/Agents/project-manifest/ui-design.md) | Visual design conventions, component taxonomy, interaction patterns |

### Quick Start Workflow

1. **Read** `README.md` — understand the domain and navigation index.
2. **Read** `tech-stack.md` — internalize the architecture and patterns.
3. **Read** `constraints.md` — learn the rules that must never be violated.
4. **Read** `file-tree.md` — orient yourself in the codebase.
5. **Reference** `api-surface.md`, `data-flows.md`, `localization.md`, `file-formats.md`, and `ui-design.md` as needed for the task at hand.

### Supplementary Reference

| Resource | Location | Purpose |
|----------|----------|---------|
| Sorting Scenarios | `Docs/Agents/Sorting Scenarios/` | Test scenarios for mod list diff and sorting logic |
| Example Files | `Docs/Agents/example-plugins.txt`, `Docs/Agents/example-steam-library.vdf` | Sample data files for reference |

---

## 2. Manifest Maintenance Rules

When you change the codebase, update the corresponding manifest documents **before** completing your task.

| Change Made | Documents to Update |
|-------------|---------------------|
| New service or coordinator added | `api-surface.md`, `file-tree.md`, `tech-stack.md` |
| New ViewModel or View added | `api-surface.md`, `file-tree.md`, `tech-stack.md` |
| New model class added | `api-surface.md`, `file-tree.md` |
| Dependency added or removed | `tech-stack.md` |
| Directory restructured or files moved | `file-tree.md` |
| Public method signature changed | `api-surface.md` |
| New coordinator event or data flow | `data-flows.md`, `api-surface.md` |
| New constraint or invariant introduced | `constraints.md` |
| New locale or localization key added | `localization.md` |
| File format or encoding rule changed | `file-formats.md` |
| UI pattern, style, or component added | `ui-design.md` |
| New converter added | `api-surface.md`, `file-tree.md` |
| New text ViewModel added | `api-surface.md`, `file-tree.md`, `tech-stack.md` |

---

## 3. Efficiency Rules — Search Smart

Do not scan source files when the answer is already in the manifest.

- **Finding files?** Check `file-tree.md` FIRST.
- **Understanding method signatures?** Check `api-surface.md` FIRST.
- **Implementation patterns or architecture?** Check `tech-stack.md` FIRST.
- **Application workflows?** Check `data-flows.md` FIRST.
- **Encoding or file I/O?** Check `file-formats.md` FIRST.
- **Localization conventions?** Check `localization.md` FIRST.
- **UI patterns or styling?** Check `ui-design.md` FIRST.
- **System invariants or constraints?** Check `constraints.md` FIRST.
- **Only then** read source files for implementation details.

---

## 4. Failure Protocol & Decision Matrix

| Scenario | Action | Priority |
|----------|--------|----------|
| Ambiguous requirement | Use the most restrictive interpretation; ask for clarification if impact is high | MUST |
| Manifest/code conflict | Trust the manifest; flag the code for fix | MUST |
| Missing documentation | Flag the gap explicitly; do not invent facts | MUST |
| Untested code path | Proceed with caution; add test recommendation | SHOULD |
| Plugins.txt encoding uncertainty | Always use UTF-8 without BOM (`new UTF8Encoding(false)`) | MUST |
| Hardcoded UI string detected | Extract to locale JSON and text ViewModel; never hardcode strings in XAML or code-behind | MUST |
| New public API without manifest entry | Add signature to `api-surface.md` before completing the task | MUST |
| Coordinator state mutation from outside coordinator | Refactor to use events or coordinator methods; do not bypass the coordinator pattern | MUST |
| Static service needs instance state | Do not convert to instance service without reviewing `tech-stack.md` patterns first | SHOULD |

---

## 5. Project Stats

| Property | Value |
|----------|-------|
| **Language / Runtime** | C# / .NET 9 |
| **UI Framework** | WPF with MaterialDesignThemes v5 |
| **Architecture** | MVVM + Coordinator Pattern + Instance Services |
| **Package Manager** | NuGet (via `dotnet restore`) |
| **Test Framework** | xUnit 2.6 + Moq 4.20 + coverlet |
| **Build Tool** | `dotnet build` / MSBuild |
| **Installer** | WiX Toolset |
| **CI/CD** | GitHub Actions (`release.yml` — tag-triggered) |
| **Localization** | JSON-based, zero-hardcoding (8 locales) |
| **Version** | Tracked in `.csproj` `<Version>` element |
| **Solution File** | `Starfield Load Order Keeper.sln` |
| **Root Namespace** | `LoadOrderKeeper` |

### Build & Test Commands

```shell
# Restore dependencies
dotnet restore

# Build
dotnet build "Starfield Load Order Keeper.sln" --configuration Debug

# Run tests
dotnet test "Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj" --configuration Debug

# Run application
dotnet run --project "Starfield Load Order Keeper.csproj"
```
