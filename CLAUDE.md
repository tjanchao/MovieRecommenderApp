# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project State

This is an **unmodified ASP.NET Core Razor Pages template** (`dotnet new webapp`) named `movieRecommender`. There is no movie/recommendation domain code yet — `Pages/Index.cshtml.cs` is an empty `OnGet()`, `Program.cs` only calls `AddRazorPages()`, and there are no NuGet dependencies, no data layer, and no test project.

The actual content of this repo so far is the **planning scaffolding**: empty arc42 architecture docs in `docs/architecture/` and the agent/skill pipeline in `.claude/` (see Planning Workflow below). Expect to be building the domain from scratch.

## Commands

```bash
dotnet run                          # runs on http://localhost:5274 (Development env)
dotnet run --launch-profile https   # adds https://localhost:7279
dotnet build
dotnet watch run                    # hot reload
dotnet format                       # SDK-built-in analyzers only; no .editorconfig in repo
```

Target framework is `net10.0` (SDK 10.0.400), with `Nullable` and `ImplicitUsings` enabled.

**Tests:** no test project exists. If adding one, create a sibling project (e.g. `tests/movieRecommender.Tests/`) and a `.sln`, then `dotnet test --filter "FullyQualifiedName~MyTest"` for a single test. Do not add a test project unless asked.

## Planning Workflow

The repo encodes a specific three-stage flow from idea to implementable spec. Respect the file locations — later stages read earlier ones by path.

1. **Story map** — the `story-mapping` agent (`.claude/agents/story-mapping.md`) interviews the user and writes `docs/product/story-map.md` with globally numbered stories (001, 002, ...). Invoked via the `story-mapping` skill. This agent must not write code.
2. **Spec** — the `/spec` skill (`.claude/skills/spec/SKILL.md`, manual-invocation only) walks the user through six phases and writes `docs/specs/NNN-<slug>.md` using `.claude/skills/spec/spec-template.md`. **NNN must match the story number from the story map**, and the spec cross-references arc42 sections.
3. **Architecture** — `docs/architecture/` holds the 12 arc42 chapters as empty templates. All tables and sections are placeholders waiting to be filled; specs reference them in their "Architecture References" section. ADRs go in `docs/architecture/adr/` (referenced by chapter 09, not yet created).

Neither `docs/product/` nor `docs/specs/` exists yet — the first run of each stage creates it.

## Razor Pages Conventions

Page routing is convention-based off the `Pages/` directory. Each page is a `.cshtml` + `.cshtml.cs` PageModel pair in namespace `movieRecommender.Pages` (set by `Pages/_ViewImports.cshtml`). Shared layout is `Pages/Shared/_Layout.cshtml`, applied via `_ViewStart.cshtml`. Static assets use the .NET 10 `MapStaticAssets()` / `.WithStaticAssets()` pipeline, so client libraries live checked-in under `wwwroot/lib/` (Bootstrap, jQuery, jQuery validation) rather than being restored by a package manager.

## Repo Hygiene

There is **no `.gitignore`**, and `obj/` build artifacts are currently tracked in git. Avoid committing further `bin/`/`obj/` churn; adding a standard .NET `.gitignore` and untracking those files is a reasonable first cleanup if the user wants it.
