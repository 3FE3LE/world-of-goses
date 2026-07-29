---
name: godot-dotnet-project
description: Enforce C#/.NET-first implementation rules for this Godot 4 project. Use whenever creating, changing, reviewing, debugging, testing, or documenting gameplay code, nodes, scenes, resources, signals, editor tooling, builds, or exports.
---

# Godot 4 C#/.NET project policy

## Language

- Produce runtime and editor scripts in C# unless the user explicitly requests GDScript.
- Treat GDScript examples from documentation or other skills as conceptual references and translate them into idiomatic Godot C#.
- Never add a `.gd` implementation merely because an upstream example uses GDScript.
- Use the Godot editor build with .NET support.

## C# conventions

- Node scripts must be `partial` classes inheriting the appropriate Godot type.
- Match the C# file name and class name.
- Use PascalCase lifecycle overrides such as `_Ready`, `_Process`, and `_PhysicsProcess`.
- Prefer typed node references, `[Export]` properties or fields, C# events/signals, and `StringName` where repeated engine lookups matter.
- Avoid `GetNode` calls every frame; cache dependencies during initialization.
- Use nullable reference types deliberately and validate required scene dependencies early.
- Prefer composition, small nodes, resources for data, and explicit signals over giant inheritance trees or global singleton dumping grounds.

## Version policy

- Do not target `net7.0` for a modern Godot project.
- Prefer `net8.0` for current desktop Godot 4 .NET projects unless the installed Godot version requires something newer.
- Verify `Godot.NET.Sdk`, TargetFramework, and export platform requirements before changing the project file.
- Do not silently upgrade Godot, the SDK, NuGet packages, or the target framework.

## Verification

After meaningful code changes:

1. Run `dotnet build`.
2. Fix compiler warnings introduced by the change.
3. Run available automated tests.
4. When Godot is available on PATH, run an appropriate headless/import/project check.
5. Report anything that could not be executed instead of claiming success.

## Scene and resource safety

- Preserve node paths, owner relationships, signal connections, exported names, and resource UIDs.
- Avoid broad textual rewrites of `.tscn`, `.tres`, or `.res` files.
- Prefer focused changes and validate the project after modifying serialized Godot files.
