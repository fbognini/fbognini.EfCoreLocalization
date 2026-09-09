---
name: commit
description: Analyze git changes, group them logically, and create commits with clear English messages
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep, Glob
effort: high
---

# Smart Git Commit

Analyze all pending git changes and commit them with clear, direct English messages. Split into multiple commits when changes are logically independent.

This repository works directly on `master`: there is no branch policy and no pull request flow, so committing on the current branch is the normal path. Never push — pushing is a separate, explicit request.

## Current git state

- Current branch: !`git branch --show-current`
- Status: !`git status --short`
- Staged diff: !`git diff --cached --stat`
- Unstaged diff: !`git diff --stat`
- Untracked files: !`git ls-files --others --exclude-standard`
- Submodule state: !`git submodule status`
- Recent commits (for style reference): !`git log --oneline -10`

## Instructions

1. **Analyze all changes** (staged, unstaged, untracked). Read the actual diffs and changed files to understand what each change does.

2. **Group changes logically**. Each group should represent one coherent unit of work. In this solution a group usually cuts across projects rather than following them:
   - A feature in the core library (entity + `LocalizationRepository` + `IStringLocalizer` wiring) together with its tests.
   - A Dashboard feature: the handler under `Handlers/`, its route in `Routes/`, and the `wwwroot/` assets (`index.html`, `css/site.css`, `js/app.js`) it needs — those belong in the same commit, because the embedded assets are useless without the endpoint that serves them.
   - A new `ITranslationsFormat` implementation and the service-collection extension that registers it.
   - Changes to `sample/SampleWebApp` that only demo an existing feature.
   - Build, packaging or CI changes: `.csproj` metadata, package versions, `.github/workflows/`, `.editorconfig`, `.gitmodules`.
   - Documentation: `README.md`, `CLAUDE.md`.

   Do NOT over-split: if all changes are part of the same feature/task, use a single commit.

   Grouping is per change, not per file: an unrelated change often hides inside a file that otherwise belongs to one group — a `PackageReference` bump in a `.csproj` full of real changes, a stray option on `DashboardOptions`. When that happens, revert that specific edit, commit the coherent group, then re-apply it and commit it on its own.

   The `vendor/claude-rules` submodule pointer is its own change. Never let a moved submodule commit ride along with unrelated work, and never stage it unless the bump is deliberate.

   These packages ship on NuGet, so a change to a public signature is breaking for consumers. When a group changes public API, say so in the commit body.

3. **For each group, create a commit**:
   - Stage only the files belonging to that group using `git add <specific files>`
   - Write a commit message that is:
     - In English
     - Direct and concise (1 line summary, optional body if needed)
     - Descriptive of WHAT changed and WHY (not HOW)
     - Using imperative mood ("Add", "Fix", "Refactor", "Remove", "Update")
   - Do NOT add any `Co-Authored-By` trailer or any other AI reference — see `vendor/claude-rules/rules/git.md`
   - Do NOT add an issue reference: this repository does not link commits to issues
   - Use a HEREDOC when the message has a body:
     ```
     git commit -m "$(cat <<'EOF'
     Summary line here

     Optional body explaining why.
     EOF
     )"
     ```

4. **Commit order**: If commits have dependencies, commit the foundational changes first (e.g., the entity and repository before the Dashboard handler that queries them, a new `ITranslationsFormat` before the sample that registers it).

5. **Deleted files**: Use `git add <file>` to stage deletions too. If a file was deleted and replaced by a new one, group them in the same commit.

6. **After all commits**, run `git log --oneline -5` to show the user what was committed.

## Rules

- NEVER use `git add -A` or `git add .` — always add specific files
- NEVER amend existing commits
- NEVER push to remote unless the user asks for it explicitly
- If there are no changes to commit, inform the user
- If unsure about grouping, prefer fewer commits over many tiny ones
