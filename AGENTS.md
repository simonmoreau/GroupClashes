# Group Clashes — Agent Guide

## Project Overview

Group Clashes is an Autodesk Navisworks Manage plugin for rule-based clash result grouping. It groups the individual clash results of a given clash test into groups based on criteria such as the nearest level, the nearest grid intersection, the model or selection an element belongs to, or the clash status/approval/assignment.

The plugin supports Navisworks 2020 through 2027. Current work happens on the `feature/update-2027` branch.

> Note: `README.md` is outdated and describes an older version of the project. This file is the authoritative reference for the current state.

## Solution Architecture

A single project (`GroupClashes/`) with a code-behind architecture. There is no MVVM, no service layer, and no abstraction around the Navisworks API — the plugin calls `Autodesk.Navisworks.Api.Application.MainDocument` directly.

```
RibbonHandler (CommandHandlerPlugin)   — ribbon tab/button, toggles the dock pane
    -> GroupClashesPane (DockPanePlugin)     — hosts the UI in a dockable pane
        -> GroupClashesHostingControl        — WinForms UserControl, embeds WPF via ElementHost
            -> GroupClashesInterface         — WPF UserControl (code-behind, no MVVM)
                -> GroupingFunctions         — static class, all core grouping logic
```

- **`RibbonHandler`** (`GroupClashes.cs`) — `CommandHandlerPlugin`. Defines the ribbon tab and button via attributes. Toggles the visibility of the dock pane. Throws when running under Automation. `TryShowCommandHelp` opens the online help.
- **`GroupClashesPane`** (`GroupClashesPane.cs`) — `DockPanePlugin`. `CreateControlPane()` returns a `GroupClashesHostingControl`.
- **`GroupClashesHostingControl`** (`GroupClashesHostingControl.cs`) — WinForms host control. Uses an `ElementHost` to embed the WPF `GroupClashesInterface` and resizes it to fill the pane.
- **`GroupClashesInterface`** (`GroupClashesInterface.xaml(.cs)`) — the WPF user interface. Code-behind only (no MVVM). Binds to `CustomClashTest` items and `GroupingMode` enums, wires document-change events, and orchestrates grouping/ungrouping. Also defines the `CustomClashTest` helper class (wraps a `ClashTest` and exposes display/selection names).
- **`GroupingFunctions`** (`GroupingFunctions.cs`) — static class containing all grouping logic, plus the `GroupingMode` enum.

## Key Concepts

The core domain logic lives in `GroupingFunctions.cs`:

- **Grouping modes** (`GroupingMode` enum): `None`, `Level`, `GridIntersection`, `SelectionA`, `SelectionB`, `ModelA`, `ModelB`, `AssignedTo`, `ApprovedBy`, `Status`.
- **Group then subgroup**: the UI exposes "Group by" and an optional "Then by". When both are set, the second mode creates subgroups within each first-level group; subgroup names are prefixed with the parent name and `_` (e.g. `LevelName_GridName`).
- **Single-clash cleanup**: groups containing exactly one clash are dissolved; that clash is left ungrouped.
- **Keep existing groups**: when checked, existing groups are backed up (`CreateCopy()`) and preserved alongside the newly created ones; otherwise existing groups are flattened back into individual clashes first.
- **Ungroup**: flattens all groups back into individual clash results.
- **Fallback groups**: clashes that don't resolve to a level, grid intersection, element, or property value are placed in fallback groups such as "No Level", "No Grid intersection", "Unnamed Level", "Empty clash", "Unspecified".
- **Navisworks constraint**: original `ClashResult`s cannot be reused in a new clash test — the code always copies results (`CreateCopy()`) before adding them. Grouping runs inside a `Transaction`, using a backed-up copy of the test for cancellation safety.

## Build System

The csproj uses a Navisworks-version selector. The configuration name becomes the `VersionSoftware` property:

- `<Configurations>Debug;2020;2021;2022;2023;2024;2025;2026;2027</Configurations>`
- When the configuration is not `Debug`, `VersionSoftware` = the configuration name.
- `VersionSoftware` drives the `Speckle.Navisworks.API` NuGet package version (`$(VersionSoftware).*`), the `VERSION$(VersionSoftware)` define constant, and the output path (`bin\$(VersionSoftware)\`).

**Debug builds target exactly one Navisworks version.** The Debug block in the csproj pins `<VersionSoftware>2027</VersionSoftware>` and sets `StartProgram` to that version's `Roamer.exe`. To debug against another version (e.g. 2026), change that single value.

The Navisworks 2027 SDK changed the clash API: `DocumentClashTests.Tests` became `DocumentClashTests.Value.TestsRoot.Children`, and `TestsReplaceWithCopy` now requires a parent `GroupItem` argument. Code handling these members is branched with `#if VERSION2027` (the 2020–2026 API still exposes the old members).

Supported versions: **2020–2027**.

### Build commands

```powershell
# Build for a specific Navisworks version (Release)
dotnet build --configuration 2024

# Build for a specific version with the solution configuration
dotnet build GroupClashes.sln --configuration 2024
```

### Post-build

`GroupClashes/PostBuild.ps1` runs after every build (`AfterTargets="PostBuildEvent"`) and:

1. Signs all unsigned `.exe`/`.dll` assemblies with `azuresigntool` against the EAI Azure Key Vault signing infrastructure.
2. In the **Debug** configuration only, copies the build output and `PackageContents.xml` into `%APPDATA%\Autodesk\ApplicationPlugins\GroupClashes.BM42.bundle\`.

Release builds do not auto-deploy. Release packaging is not yet automated — a dedicated PowerShell script is planned for the future.

## Key Technologies

- **.NET Framework 4.8** (`net48`), SDK-style csproj
- **WPF + WinForms interop**: `ElementHost` hosts the WPF interface inside a WinForms dock pane
- **`Speckle.Navisworks.API`** NuGet package for the Navisworks SDK assemblies (version selected per Navisworks version)
- **PowerShell** for post-build signing/deployment
- Target `LangVersion` for new code: `latest` (requires adding `<LangVersion>latest</LangVersion>` to the csproj)

## Testing

There are no automated tests. Manual testing inside Navisworks is the permanent approach for this project.

## Code Conventions

These are the **target conventions for new code**. Existing code may not fully conform yet.

- File-scoped namespaces, single-line usings
- `_camelCase` for private fields
- No `var` (always explicit types)
- No `#region`
- No local functions
- `is null` / `is not null` (never `== null` / `!= null`)
- XML doc comments on all public APIs
- No comments on non-public code

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes/Methods/Properties | PascalCase | `GroupingFunctions` |
| Private fields | `_camelCase` | `_clashTest` |
| Interfaces | `I` prefix | `IGroupingService` |
| Local variables | camelCase | `myVariable` |

## Deployment

- **Versioning**: manually bumped in `CommonAssemblyInfo.props` (edit via Ctrl+H). Current version: 1.1.4.
- **Distribution**: published to the Autodesk App Store via `PackageContents.xml` (which also lists the supported Navisworks versions).
- **Installation for debugging**: the PostBuild script deploys the Debug build to the Autodesk ApplicationPlugins bundle folder.

## Key Directories

| Path | Contents |
|---|---|
| `GroupClashes/` | All source code (entry points, hosting, WPF UI, grouping logic) |
| `GroupClashes/GroupClashes.cs` | `RibbonHandler` plugin entry point |
| `GroupClashes/GroupClashesPane.cs` | `DockPanePlugin` implementation |
| `GroupClashes/GroupClashesHostingControl.cs` | WinForms/ElementHost interop host |
| `GroupClashes/GroupClashesInterface.xaml(.cs)` | WPF UI and code-behind |
| `GroupClashes/GroupingFunctions.cs` | Core grouping logic and `GroupingMode` enum |
| `GroupClashes/en-US/` | Navisworks ribbon layout/strings resources |
| `GroupClashes/Images/` | Ribbon icons |
| `GroupClashes/PackageContents.xml` | Autodesk App Store / ApplicationPlugin manifest |
| `GroupClashes/PostBuild.ps1` | Post-build signing and deployment |
| `CommonAssemblyInfo.props` | Assembly version and branding |
| `GroupClashes/bin/` | Build outputs, one folder per Navisworks version |

## CI/CD

There is no CI/CD. Builds, signing, packaging, and App Store uploads are all done manually via local PowerShell commands and scripts.
