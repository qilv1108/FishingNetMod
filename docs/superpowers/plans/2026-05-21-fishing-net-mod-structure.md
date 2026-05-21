# FishingNetMod Structure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a minimal, buildable SMAPI mod project skeleton for Fishing Net Mod.

**Architecture:** The mod is an independent SDK-style .NET class library in `FishingNetMod/`. SMAPI metadata lives in `manifest.json`, build/game dependency resolution is delegated to `Pathoschild.Stardew.ModBuildConfig`, and `ModEntry.cs` provides the initial load entry point.

**Tech Stack:** C# 10, .NET 6, SMAPI 4.x, Pathoschild.Stardew.ModBuildConfig.

---

## File Structure

- Create: `FishingNetMod/FishingNetMod.csproj` — defines the .NET class library, version, target framework, nullable/implicit usings, and SMAPI mod build package dependency.
- Create: `FishingNetMod/manifest.json` — defines SMAPI mod metadata and entry DLL.
- Create: `FishingNetMod/ModEntry.cs` — defines the SMAPI entry class and logs a load message.

### Task 1: Create SMAPI project file

**Files:**
- Create: `FishingNetMod/FishingNetMod.csproj`

- [ ] **Step 1: Create the project directory**

Run:

```bash
mkdir -p "FishingNetMod"
```

Expected: command exits successfully and `FishingNetMod/` exists.

- [ ] **Step 2: Write the project file**

Create `FishingNetMod/FishingNetMod.csproj` with exactly this content:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Version>0.1.0</Version>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.3.2" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Verify NuGet restore starts**

Run:

```bash
dotnet restore "FishingNetMod/FishingNetMod.csproj"
```

Expected: restore completes successfully. If NuGet reports a newer available compatible package, keep `4.3.2` for this initial skeleton unless the restore fails.

### Task 2: Create SMAPI manifest

**Files:**
- Create: `FishingNetMod/manifest.json`

- [ ] **Step 1: Write the manifest file**

Create `FishingNetMod/manifest.json` with exactly this content:

```json
{
  "Name": "Fishing Net Mod",
  "Author": "ChenJianCan",
  "Version": "%ProjectVersion%",
  "Description": "Adds fishing net tools to Stardew Valley.",
  "UniqueID": "ChenJianCan.FishingNetMod",
  "EntryDll": "FishingNetMod.dll",
  "MinimumApiVersion": "4.0.0"
}
```

- [ ] **Step 2: Validate JSON syntax**

Run:

```bash
python -m json.tool "FishingNetMod/manifest.json"
```

Expected: command prints formatted JSON and exits successfully.

### Task 3: Create SMAPI entry class

**Files:**
- Create: `FishingNetMod/ModEntry.cs`

- [ ] **Step 1: Write the entry class**

Create `FishingNetMod/ModEntry.cs` with exactly this content:

```csharp
using StardewModdingAPI;

namespace FishingNetMod;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        this.Monitor.Log("Fishing Net Mod loaded.", LogLevel.Info);
    }
}
```

- [ ] **Step 2: Build the project**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds and produces `FishingNetMod/bin/Debug/net6.0/FishingNetMod.dll`.

### Task 4: Verify deployment-relevant output

**Files:**
- Verify: `FishingNetMod/bin/Debug/net6.0/FishingNetMod.dll`
- Verify: `FishingNetMod/bin/Debug/net6.0/manifest.json`

- [ ] **Step 1: Check build output files exist**

Run:

```bash
ls "FishingNetMod/bin/Debug/net6.0/FishingNetMod.dll" "FishingNetMod/bin/Debug/net6.0/manifest.json"
```

Expected: both file paths are printed.

- [ ] **Step 2: Confirm manifest version token was replaced in output**

Run:

```bash
python - <<'PY'
import json
from pathlib import Path
manifest = json.loads(Path('FishingNetMod/bin/Debug/net6.0/manifest.json').read_text(encoding='utf-8'))
assert manifest['Version'] == '0.1.0', manifest['Version']
assert manifest['EntryDll'] == 'FishingNetMod.dll', manifest['EntryDll']
print('manifest output OK')
PY
```

Expected: prints `manifest output OK`.

### Task 5: Final build verification

**Files:**
- Verify: `FishingNetMod/FishingNetMod.csproj`
- Verify: `FishingNetMod/manifest.json`
- Verify: `FishingNetMod/ModEntry.cs`

- [ ] **Step 1: Run a clean build**

Run:

```bash
dotnet clean "FishingNetMod/FishingNetMod.csproj" && dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: clean and build both succeed with `0 Error(s)`.

- [ ] **Step 2: Review created source files**

Run:

```bash
ls "FishingNetMod/FishingNetMod.csproj" "FishingNetMod/manifest.json" "FishingNetMod/ModEntry.cs"
```

Expected: all three source file paths are printed.

---

## Self-Review

- Spec coverage: project location, `.csproj`, `manifest.json`, `ModEntry.cs`, SMAPI/.NET compatibility, and build verification are covered by Tasks 1-5.
- Placeholder scan: no placeholder steps or deferred implementation remain.
- Type consistency: `FishingNetMod.dll`, namespace `FishingNetMod`, and `ModEntry : Mod` match the manifest and project output names.
