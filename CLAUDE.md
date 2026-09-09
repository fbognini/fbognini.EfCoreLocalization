# fbognini.EfCoreLocalization

Shared conventions live in the `vendor/claude-rules` submodule. Run `git submodule update --init` after cloning, or `git clone --recurse-submodules`.

@vendor/claude-rules/rules/comments.md
@vendor/claude-rules/rules/csharp.md
@vendor/claude-rules/rules/git.md
@vendor/claude-rules/rules/javascript.md
@vendor/claude-rules/rules/testing.md

## Solution layout

| Project | Role |
| --- | --- |
| `src/fbognini.EfCoreLocalization` | `IStringLocalizer` backed by EF Core, the `Language`/`Text`/`Translation` entities, `LocalizationRepository` and the import/export services under `Portability/`. |
| `src/fbognini.EfCoreLocalization.Dashboard` | Self-hosted admin UI mounted as middleware. Handlers under `Handlers/`, endpoints in `Routes/`, static assets embedded from `wwwroot/`. |
| `src/fbognini.EfCoreLocalization.Excel` | `xlsx` implementation of `ITranslationsFormat`, on ClosedXML. |
| `sample/SampleWebApp` | Razor Pages host used to try the packages end to end. |
| `tests/fbognini.EfCoreLocalization.Tests` | xUnit suite over a SQLite in-memory database. |

Everything targets `net8.0` and ships as NuGet packages, so a change to a public signature is a breaking change for consumers.

## Commands

```bash
dotnet build fbognini.EfCoreLocalization.sln
dotnet test tests/fbognini.EfCoreLocalization.Tests
dotnet run --project sample/SampleWebApp
```

## Dashboard assets

`wwwroot/**` is embedded into the Dashboard assembly, so an edit to `index.html`, `css/site.css` or `js/app.js` needs a rebuild of the Dashboard project before the sample app serves it. `{{BASE_PATH}}` in the static files is substituted at request time with the mount path.
