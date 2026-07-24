Include ..\AGENTS.md

# Shaft Build Order — Mod Overview

## Purpose
Forces power shafts to be built sequentially in the direction they are placed. When holding **Shift** while placing shafts, a yellow directional arrow appears and builders will not work on a segment until the segment behind it is complete (haulers can still deliver materials to all segments).

**Mod ID:** `Calloatti.ShaftBuildOrder` · **Assembly:** `shaftbuildorder` · **Namespace:** `Calloatti.ShaftBuildOrder`

## Key Classes & Responsibilities

| Class | File | Role |
|---|---|---|
| `ModStarter` | `ModStarter.cs` | Entry point — creates Harmony instance `"Calloatti.ShaftBuildOrder"` and calls `PatchAll()` |
| `ShaftBuildOrderConfigurator` | `ShaftBuildOrderConfigurator.cs` | Bindito DI — registers `ShaftBuildOrderBlocker` and `ShaftDirectionVisualizer` as transients, decorates `ModularShaft` with both via `TemplateModule` |
| `ShaftBuildOrderBlocker` | `ShaftBuildOrderBlocker.cs` | Core logic — attached to each `ModularShaft`. Detects Shift-held placement, checks `CoordinatesBehind()` for unfinished predecessor, sets `EnforceBuildOrder` flag. Shows/hides `StatusToggle`. Persists via `IPersistentEntity`. |
| `ShaftDirectionVisualizer` | `ShaftDirectionVisualizer.cs` | Visual — renders yellow triangular arrow mesh above shafts. Visible during preview (Shift held) and during construction (when enforce mode active). Hidden when finished. |
| `ConstructionSite_ReadyToBuild_Patch` | `ModPatches.cs` | Harmony postfix on `ConstructionSite.ReadyToBuild` getter — if a shaft has `ShaftBuildOrderBlocker` with `EnforceBuildOrder && ShouldBlockBuilders()`, forces `__result = false` |

## How It Hooks Into the Game

- **Mod loading:** Native `IModStarter` (no BepInEx)
- **Harmony:** 1 postfix patch on `ConstructionSite.ReadyToBuild` getter
- **DI:** Bindito `[Context("Game")]` configurator, decorates `ModularShaft` with custom components
- **Persistence:** `IPersistentEntity` — saves/loads `EnforceBuildOrder` flag per shaft
- **Input:** `Keyboard.current.shiftKey.isPressed` during `OnPostPlacementChanged`
- **Status UI:** `StatusToggle` with localization key `"DirectionalBlocking"` = "Waiting for previous segment to be built"

## Architecture

- Source in versioned folders: `Version-1.0/`, `Version-1.1/`
- Targets `netstandard2.1`, C# 10, nullable disabled
- Uses `Krafs.Publicizer` v2.3.0 (~33+ assemblies in shared props, plus per-project: `Timberborn.Terraforming`, `Timberborn.ModularShafts`, `Timberborn.BlockingSystem`, `Timberborn.BlockSystem`, `Timberborn.BuilderHubSystem`)
- Harmony DLL referenced from Steam Workshop ID `3284904751`

## Notable Implementation Details

- **Direction detection:** `_blockObject.CoordinatesBehind()` is an extension method from `Timberborn.ModularShafts` (publicized) that returns the coordinate behind the shaft based on its facing direction.
- **Hauler vs Builder split:** The patch only blocks `ConstructionSite.ReadyToBuild` (affects builders). Haulers use a different code path for material delivery, so they keep filling all construction sites regardless of order.
- **Shift detection happens once** at placement time (`OnPostPlacementChanged`). The flag is persisted, so build order survives save/load.
- **No localization file** — status toggle uses a hardcoded English string (`"DirectionalBlocking"` key at `StatusToggle.CreateNormalStatus`). No `.csv` locale files present.
- **No blueprints, assets, or UI files** — the arrow is created programmatically (procedural mesh + `Sprites/Default` shader).
- **v1.0 vs v1.1 difference:** v1.0 targets `MinimumGameVersion 1.0.0.0` and uses `IPreviewStateListener` + `IStartableComponent`. v1.1 targets `MinimumGameVersion 1.1.0.0` and uses `IInitializablePreview` + `IInitializableEntity` (the Timberborn API changed between versions).

## Save/Load

- Component key: `"ShaftBuildOrderBlocker"`, property key: `"EnforceOrder"`
- Only the boolean `EnforceBuildOrder` is persisted per shaft — no other state

## Build

- Pre/post-build scripts in `..\..\tools\`
- Project file: `Version-1.{0,1}/ShaftBuildOrder.csproj`
- Mod v1.0.0 / v1.1.0, requires Harmony 2.4.1
- No test framework present
