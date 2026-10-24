# Optimizing MSBuild Incremental Builds

Source: original `incremental-build` skill.

## How MSBuild Incremental Build Works

MSBuild can skip targets whose outputs are up to date, reducing work on subsequent
ordinary builds.

- **`Inputs` and `Outputs`:** file-producing targets use file timestamps to
  determine freshness. Outputs must exist and be at least as new as the relevant
  inputs. One-to-one item transforms can support partial incremental execution
  rather than requiring every output to be rebuilt.
- **Without file-based checks:** a target with neither attribute has no such
  freshness test when reached. It can still be skipped by conditions or because
  it already ran for that project instance. Cheap orchestration or discovery
  targets may correctly execute on every invocation that reaches them.
- **No `Incremental` target attribute:** `Incremental="false"` is not a valid
  MSBuild `Target` attribute. Do not add it to force execution.
- **Timestamps, not hashes:** touching an input can invalidate the target even
  without content changes. Conversely, unchanged timestamps can hide changes.

```xml
<!-- A one-to-one input/output mapping -->
<Target Name="Transform"
        Inputs="@(TransformFiles)"
        Outputs="@(TransformFiles->'$(OutputPath)%(Filename).out')">
  <!-- Existing transformation task goes here -->
</Target>

<!-- No file-based freshness check when this target is invoked -->
<Target Name="PrintMessage">
  <Message Text="This runs when the target is reached" />
</Target>
```

Give transformed outputs distinct paths if inputs can have duplicate basenames.
Do not add fictitious output files merely to skip necessary work.

## Why Incremental Builds Break (Top Causes)

1. **Missing Inputs/Outputs on file-producing custom targets.** Declare meaningful
   file dependencies for expensive repeatable work, not indiscriminately for every
   orchestration target.
2. **Volatile output paths.** Timestamps, build counters, or GUIDs in a path prevent
   finding the prior output.
3. **Writes outside declared Outputs.** A target may skip even when an undeclared
   generated file is missing or stale; track all required outputs.
4. **Missing FileWrites registration.** Standard Clean tracking may miss custom
   generated files. Merely putting a file under `obj` does not register it.
5. **Glob changes.** Additions and removals alter item membership, but general
   timestamp checks do not remember the previous set. Removing an input, or
   adding an older input, need not invalidate a custom target. Use appropriate
   input-set tracking and obsolete-output cleanup when the output depends on
   membership.
6. **Property changes.** MSBuild does not automatically compare prior property
   values used by a generator. Track relevant values explicitly. Changed
   configuration/TFM/RID paths can select missing outputs, but returning to an
   already-built configuration can reuse its isolated output set.
7. **NuGet package updates.** Assets and resolved reference changes can correctly
   invalidate compilation and resolution work. Preserve necessary invalidation.
8. **Compiler-server state.** A recycled `VBCSCompiler` can make a compilation that
   executes slower. It does not by itself explain why an up-to-date `CoreCompile`
   ran; separate execution decisions from compiler warm-up.

## Diagnosing "Why Did This Rebuild?"

Use binlogs to identify exactly why a target executed instead of skipping.

### Step-by-Step Using Binlog

1. Reuse matching first/second-build artifacts if available. Otherwise follow
   [binlog generation](binlog-generation.md) and capture two ordinary builds:

   Choose unused explicit names for both logs; the names below are illustrative.

   ```powershell
   dotnet build "-bl:first.binlog"
   if ($LASTEXITCODE -ne 0) { throw "First build failed; no valid incremental baseline." }
   dotnet build "-bl:second.binlog"
   ```

   Keep configuration, TFM/RID, properties, restore policy, and inputs unchanged.
   Analyze the second successful build. `Rebuild` and `--no-incremental` force
   full work and cannot establish a no-change skip result.
2. [Replay the second binlog](binlog-failure-analysis.md#replay-a-binary-log)
   locally with diagnostic verbosity and a performance summary, substituting `second.binlog`
   for `build.binlog`. The searches below use that command's `full-01.log`; substitute the
   actual path if you chose another name. Replay the first build to a different file when comparing.
3. Find executed/skipped targets and the accompanying reasons:

   ```powershell
   Select-String -Path .\full-01.log -Pattern 'Building target|Skipping target|was not skipped|is newer than output' -Context 0,5
   ```

4. Interpret messages in context (`...` stands for the target name):
   - **`Building target "..." completely.`:** full target execution; read the reason
     instead of assuming all outputs were missing.
   - **`Building target "..." partially, because some output files are out of date
     with respect to their input files.`:** partial execution for an applicable
     input/output mapping.
   - **`Skipping target "..." because all output files are up-to-date with respect
     to the input files.`:** freshness checks permitted a skip; verify the declared
     dependency set is complete.
5. Record the precise input/output path, timestamp, missing output, or condition
   responsible, for the correct project/global-property instance.

### Additional Diagnostic Techniques

- Compare the two replayed logs for changed properties, items, paths, and reasons.
- Find expensive second-build targets:

  ```powershell
  Select-String -Path .\full-01.log -Pattern 'Target Performance Summary|Task Performance Summary' -Context 0,30
  ```

- Aggregate/nested timings are not additive wall time. Follow orchestration waits
  to actual task work using [performance diagnostics](build-perf-diagnostics.md).
- A zero-duration target that runs is not automatically a defect. Inspect its
  dependencies and actual cost before changing it.
- If evaluation dominates while compilation correctly skips, use
  [evaluation performance](eval-performance.md), not a fake incremental shortcut.

## FileWrites and Clean Build

`FileWrites` lets the standard Clean infrastructure track build-generated files.
It is separate from target freshness and compiler inclusion.

- **`FileWrites`:** register owned generated files so the normal Clean pipeline
  can discover them.
- **`FileWritesShareable`:** do not assume reference-counted ownership or protection
  because another project still uses a file. Inspect the selected Clean targets,
  locations, and shared-file ownership before relying on it.
- **Missing registration:** files can accumulate, leaving stale inputs or outputs.
  A directory named `obj` is not automatic per-file tracking.

### Pattern for Registering Generated Files

For a generator with declared input files and generated line content:

```xml
<Target Name="MyGenerator"
        Inputs="$(MSBuildProjectFullPath);@(GeneratorInput)"
        Outputs="$(IntermediateOutputPath)generated.cs">
  <WriteLinesToFile File="$(IntermediateOutputPath)generated.cs"
                    Lines="@(GeneratedLines)"
                    Overwrite="true" />
  <ItemGroup>
    <FileWrites Include="$(IntermediateOutputPath)generated.cs" />
  </ItemGroup>
</Target>
```

Ensure the directory exists and that all settings/tool/template dependencies are
modeled; project-file timestamps do not track arbitrary property values. Use
`Overwrite="true"` when replacing generated content rather than appending it.

An `ItemGroup` inside a target with an up-to-date skip can still be evaluated by
**output inference**. Thus the source pattern of registering `FileWrites` and
`Compile` inside that target is valid when the paths/items can be inferred.
Do not move it solely because the task skipped. If an item depends only on task
output that cannot be inferred, make that item's definition available separately.

Use only authorized, scoped Clean operations. Never register user-authored files
or another project's outputs merely to make cleanup more aggressive.

## Visual Studio Fast Up-to-Date Check

Visual Studio's Fast Up-to-Date Check (FUTDC) is distinct from MSBuild's
`Inputs`/`Outputs`: it can skip invoking MSBuild entirely.

- **Fast checks:** the project system examines known inputs, outputs, copy items,
  and timestamps without a full build.
- **Custom work:** custom …14879 tokens truncated…meterization overrides inside compiled queries — EF Core 9](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes)
- [Efficient updating (ExecuteUpdate/ExecuteDelete) — EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-updating)
- [Single vs. split queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [Pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination)
- [Indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
