# User Guides

Comprehensive documentation for authoring MSBuild packages using JD.MSBuild.Fluent's strongly-typed fluent API.

## Quick Links

- [Quick Start](getting-started/quick-start.md) - Get up and running in 5 minutes
- [Architecture](core-concepts/architecture.md) - Understand the framework architecture
- [Fluent Builders](core-concepts/builders.md) - Master the fluent builder API
- [Working with Targets](targets-tasks/targets.md) - Define build targets and orchestration
- [Custom Tasks](targets-tasks/custom-tasks.md) - Declare and invoke custom MSBuild tasks
- [Migration Guide](migration/from-xml.md) - Migrate from manual XML to fluent API
- [Best Practices](best-practices/index.md) - Patterns for authoring robust MSBuild packages

## Overview

JD.MSBuild.Fluent is a strongly-typed, fluent DSL for authoring MSBuild `.props`, `.targets`, and SDK assets. It eliminates XML boilerplate by providing a C# fluent API that generates 100% standard MSBuild XML. All MSBuild patterns are fully supported: properties, items, targets, UsingTask declarations, Choose/When/Otherwise constructs, and more.

The framework is built around three core concepts:

1. **Intermediate Representation (IR)** - An in-memory model of MSBuild projects that separates authoring from rendering
2. **Fluent Builders** - Type-safe, chainable API for constructing MSBuild assets
3. **Deterministic Rendering** - Canonical XML output that eliminates spurious diffs

### Why Use JD.MSBuild.Fluent?

**Type Safety**: Strongly-typed property names, item types, target names, and task parameters catch errors at compile time.

**DRY Principle**: Share common logic across props and targets, extract helper methods, and leverage C# language features.

**Refactorability**: Rename symbols with IDE support, extract methods, and reorganize code without breaking XML structure.

**Testability**: Validate generated MSBuild projects in unit tests with full access to the IR.

**Determinism**: Canonical rendering ensures consistent output order, making diffs meaningful and reducing merge conflicts.

### Package Structure

When you define a package, you can emit assets to multiple locations:

| Location | Purpose | When to Use |
|----------|---------|-------------|
| `build/` | Direct consumers only | Properties and targets for projects that directly reference your package |
| `buildTransitive/` | Direct and transitive consumers | Propagate settings through the dependency graph |
| `Sdk/` | SDK-style projects | Enable `<Project Sdk="YourPackageId">` syntax |

The fluent API provides dedicated methods for each location: `Props()`, `Targets()`, `BuildProps()`, `BuildTargets()`, `BuildTransitiveProps()`, `BuildTransitiveTargets()`, `SdkProps()`, and `SdkTargets()`.

## Getting Started

Start with the [Quick Start](getting-started/quick-start.md) guide to create your first MSBuild package definition. Then explore the core concepts to understand the architecture and builder patterns.

## Core Concepts

### Architecture

The [Architecture](core-concepts/architecture.md) guide explains the three-layer design:
- IR layer for representing MSBuild projects
- Builder layer for constructing projects
- Renderer layer for emitting canonical XML
- Packaging layer for NuGet folder structure

### Builders

The [Fluent Builders](core-concepts/builders.md) guide covers:
- Package definition with `Package.Define()`
- Property and item definitions
- Choose/When/Otherwise conditionals
- Strongly-typed names and helpers
- Builder composition patterns

## Targets and Tasks

### Targets

The [Targets](targets-tasks/targets.md) guide demonstrates:
- Target orchestration (BeforeTargets, AfterTargets, DependsOnTargets)
- Inputs and Outputs for incremental builds
- Task invocations within targets
- PropertyGroup and ItemGroup inside targets

### Custom Tasks

The [Custom Tasks](targets-tasks/custom-tasks.md) guide shows:
- UsingTask declarations with multi-TFM assembly paths
- Task parameter bindings
- Output mappings to properties and items
- Strongly-typed task references

## Migration and Best Practices

### Migration from XML

The [Migration Guide](migration/from-xml.md) provides:
- Side-by-side XML and fluent API comparisons
- Migration strategies for existing packages
- Common patterns and their fluent equivalents

### Best Practices

The [Best Practices](best-practices/index.md) guide covers:
- Package organization patterns
- Separation of props and targets
- Conditional logic strategies
- Multi-targeting considerations
- Testing and validation approaches

## Navigation

Use the sidebar to navigate through topics organized by complexity and use case. Each guide includes runnable code examples demonstrating real-world usage patterns.
