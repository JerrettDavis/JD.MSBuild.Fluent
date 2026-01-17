# Tutorials

Learn JD.MSBuild.Fluent through hands-on tutorials that take you from basic property packages to complex build pipelines.

## Learning Path

Follow this structured path to master MSBuild package authoring with JD.MSBuild.Fluent:

### 🟢 Beginner: Fundamentals

Start here if you're new to JD.MSBuild.Fluent or MSBuild package authoring.

1. **[Building a Simple Properties Package](beginner/simple-props.md)** (15 minutes)
   - Create your first MSBuild package
   - Define properties and items
   - Generate and test the output
   - **What you'll learn**: Package structure, props configuration, basic emission

### 🟡 Intermediate: Build Integration

Once you're comfortable with properties, move on to targets and build orchestration.

2. **[Creating a Build Integration Package](intermediate/build-integration.md)** (30 minutes)
   - Define custom build targets
   - Orchestrate target execution order
   - Integrate with the MSBuild lifecycle
   - Handle task invocations and error conditions
   - **What you'll learn**: Targets, task invocation, BeforeTargets/AfterTargets, incremental builds

### 🔴 Advanced: Real-World Patterns

Master complex patterns used in production MSBuild packages.

3. **[Recreating JD.Efcpt.Build Patterns](advanced/efcpt-patterns.md)** (45 minutes)
   - Multi-TFM task assembly selection
   - Complex target orchestration with dependencies
   - UsingTask with runtime detection
   - Property-driven feature toggles
   - Build profiling and lifecycle hooks
   - **What you'll learn**: Advanced target patterns, multi-TFM support, complex dependencies

4. **[Recreating Docker Container Patterns](advanced/containers-patterns.md)** (45 minutes)
   - Dockerfile generation targets
   - Build automation with Docker
   - Publish hooks and customization points
   - Pre/post script execution
   - Dynamic property computation
   - **What you'll learn**: Code generation patterns, publish integration, extensibility hooks

## Prerequisites

Before starting the tutorials, ensure you have:

- **.NET SDK 8.0 or later** installed
- **Basic MSBuild knowledge**: Understanding of properties, items, targets, and tasks
- **C# experience**: Familiarity with C# and fluent APIs
- **JD.MSBuild.Fluent installed**: See [Quick Start](../user-guides/getting-started/quick-start.md)

## Tutorial Structure

Each tutorial follows this structure:

1. **Overview**: What you'll build and why it matters
2. **Learning Objectives**: Specific skills you'll gain
3. **Prerequisites**: Required knowledge and tools
4. **Step-by-Step Implementation**: Guided code development with explanations
5. **Complete Code**: Full working example
6. **Generated XML**: The MSBuild output produced
7. **Testing**: How to validate your package
8. **What You Learned**: Key takeaways
9. **Next Steps**: Where to go from here

## How to Use These Tutorials

### Hands-On Learning

The most effective way to learn is by typing the code yourself. Don't just read—create a new project and follow along.

```bash
# Create a new class library for your package definitions
dotnet new classlib -n MyBuildPackages
cd MyBuildPackages
dotnet add package JD.MSBuild.Fluent
```

### Progressive Difficulty

Start with the beginner tutorial even if you have MSBuild experience. Each tutorial builds on concepts from the previous ones.

### Reference While Coding

Keep the [Fluent Builders](../user-guides/core-concepts/builders.md) guide open as a reference while working through tutorials.

## Companion Resources

As you work through tutorials, these resources will help:

- **[Quick Start Guide](../user-guides/getting-started/quick-start.md)**: Fast introduction to the framework
- **[Fluent Builders Reference](../user-guides/core-concepts/builders.md)**: Complete API reference for all builders
- **[Working with Targets](../user-guides/targets-tasks/targets.md)**: Deep dive into target orchestration
- **[Custom Tasks](../user-guides/targets-tasks/custom-tasks.md)**: Task declaration and invocation patterns
- **[Best Practices](../user-guides/best-practices/index.md)**: Patterns for robust packages
- **[Architecture](../user-guides/core-concepts/architecture.md)**: How the framework works internally

## Real-World Examples

After completing the tutorials, explore these real-world examples:

- **JD.Efcpt.Build**: Complex EF Core build pipeline with 40KB+ of generated targets
- **JD.MSBuild.Containers**: Docker integration with Dockerfile generation and publish hooks
- **MinimalSdkPackage**: Simple SDK-style package (in `/samples` directory)

## Getting Help

If you get stuck:

1. **Check the [user guides](../user-guides/index.md)** for detailed API documentation
2. **Review the [samples directory](../../samples/)** for working examples
3. **Look at generated XML** to understand what your fluent code produces
4. **Simplify your code** to isolate the problem
5. **File an issue** on GitHub if you find a bug or have questions

## Tutorial Quick Links

### Beginner
- [Building a Simple Properties Package](beginner/simple-props.md)

### Intermediate
- [Creating a Build Integration Package](intermediate/build-integration.md)

### Advanced
- [Recreating JD.Efcpt.Build Patterns](advanced/efcpt-patterns.md)
- [Recreating Docker Container Patterns](advanced/containers-patterns.md)

## What's Next?

Ready to start? Begin with **[Building a Simple Properties Package](beginner/simple-props.md)** to create your first MSBuild package in 15 minutes.

Already experienced with MSBuild packages? Jump to **[Creating a Build Integration Package](intermediate/build-integration.md)** to learn target orchestration.

Want to see advanced patterns? Explore **[Recreating JD.Efcpt.Build Patterns](advanced/efcpt-patterns.md)** for complex real-world scenarios.

---

**Note**: These tutorials use JD.MSBuild.Fluent 1.0+. Some APIs may differ in earlier versions.
