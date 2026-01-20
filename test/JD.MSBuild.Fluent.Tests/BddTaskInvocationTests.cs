using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JD.MSBuild.Fluent.Typed;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace JD.MSBuild.Fluent.Tests;

/// <summary>Feature: MSBuildTaskInvocation</summary>
public sealed class BddTaskInvocationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Fact]
    public async Task Scenario_Declare_UsingTask()
    {
        await Given("a targets builder", () => Package.Define("Test"))
            .When("declaring a UsingTask", b => b
                .Targets(t => t.UsingTask("MyTask", "$(MSBuildThisFileDirectory)MyTask.dll"))
                .Build())
            .Then("UsingTask is declared", d =>
            {
                var usingTask = d.Targets.UsingTasks.FirstOrDefault();
                return usingTask != null &&
                       usingTask.TaskName == "MyTask" &&
                       usingTask.AssemblyFile == "$(MSBuildThisFileDirectory)MyTask.dll";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Invoke_custom_task()
    {
        await Given("a target with UsingTask", () => Package.Define("Test"))
            .When("invoking the custom task", b => b
                .Targets(t => t
                    .UsingTask("MyCustomTask", "MyAssembly.dll")
                    .Target<TTarget>(tgt => tgt.Task("MyCustomTask", task => { })))
                .Build())
            .Then("task is invoked in target", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().FirstOrDefault();
                return taskStep?.TaskName == "MyCustomTask";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Set_task_parameters()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("invoking task with parameters", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt.Task("MyTask", task => task
                    .Param("Input", "$(InputValue)")
                    .Param("OutputDir", "bin/output"))))
                .Build())
            .Then("task has parameters", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().First();
                return taskStep.Parameters.Count == 2 &&
                       taskStep.Parameters["Input"] == "$(InputValue)" &&
                       taskStep.Parameters["OutputDir"] == "bin/output";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Capture_task_output_to_property()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("capturing task output to property", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt.Task("MyTask", task => task
                    .Param("Input", "value")
                    .OutputProperty("ResultParam", "ResultProperty"))))
                .Build())
            .Then("output is mapped to property", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().First();
                var output = taskStep.Outputs.FirstOrDefault();
                return output != null &&
                       output.TaskParameter == "ResultParam" &&
                       output.PropertyName == "ResultProperty" &&
                       output.ItemName == null;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Capture_task_output_to_item()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("capturing task output to item", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt.Task("MyTask", task => task
                    .Param("Input", "value")
                    .OutputItem("FilesParam", "GeneratedFiles"))))
                .Build())
            .Then("output is mapped to item", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().First();
                var output = taskStep.Outputs.FirstOrDefault();
                return output != null &&
                       output.TaskParameter == "FilesParam" &&
                       output.ItemName == "GeneratedFiles" &&
                       output.PropertyName == null;
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Chain_multiple_tasks()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("chaining multiple tasks", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .Task("Task1", t1 => t1.Param("P1", "V1"))
                    .Task("Task2", t2 => t2.Param("P2", "V2"))
                    .Task("Task3", t3 => t3.Param("P3", "V3"))))
                .Build())
            .Then("all tasks are present in order", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var tasks = target.Elements.OfType<MsBuildTaskStep>().ToList();
                return tasks.Count == 3 &&
                       tasks[0].TaskName == "Task1" &&
                       tasks[1].TaskName == "Task2" &&
                       tasks[2].TaskName == "Task3";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Use_strongly_typed_task_parameters()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("using strongly-typed parameters", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt.Task("MyTask", task => task
                    .Param<TParam>("ParamValue")
                    .OutputProperty<TOutParam, TProperty>())))
                .Build())
            .Then("parameters use correct names", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().First();
                var output = taskStep.Outputs.FirstOrDefault();
                return taskStep.Parameters["TParam"] == "ParamValue" &&
                       output?.TaskParameter == "TOutParam" &&
                       output?.PropertyName == "TProperty";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Add_task_with_condition()
    {
        await Given("a target builder", () => Package.Define("Test"))
            .When("adding task with condition", b => b
                .Targets(t => t.Target<TTarget>(tgt => tgt
                    .Task("MyTask", task => task.Param("P", "V"), "'$(RunTask)' == 'true'")))
                .Build())
            .Then("task has condition", d =>
            {
                var target = d.Targets.Targets.First(t => t.Name == "TTarget");
                var taskStep = target.Elements.OfType<MsBuildTaskStep>().First();
                return taskStep.Condition == "'$(RunTask)' == 'true'";
            })
            .AssertPassed();
    }

    [Fact]
    public async Task Scenario_Use_task_reference_from_type()
    {
        await Given("a CLR task type", () => typeof(Contoso.Build.Tasks.ResolveInputs))
            .When("declaring UsingTask from type", taskType => Package.Define("Test")
                .Targets(t => t.UsingTask<Contoso.Build.Tasks.ResolveInputs>("MyAssembly.dll"))
                .Build())
            .Then("UsingTask uses full type name", d =>
            {
                var usingTask = d.Targets.UsingTasks.FirstOrDefault();
                return usingTask?.TaskName.Contains("ResolveInputs") == true;
            })
            .AssertPassed();
    }

    private readonly struct TTarget : IMsBuildTargetName { public string Name => "TTarget"; }
    private readonly struct TParam : IMsBuildTaskParameterName { public string Name => "TParam"; }
    private readonly struct TOutParam : IMsBuildTaskParameterName { public string Name => "TOutParam"; }
    private readonly struct TProperty : IMsBuildPropertyName { public string Name => "TProperty"; }
}
