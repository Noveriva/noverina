# Repository Guidelines

## Project Structure & Modules

Noverina is a .NET 10 modular monolith with a single Web API host.
- `src/Noverina.Api/` contains the HTTP API, its service configuration, its health checks and its Dockerfile.
- `src/Noverina.AppHost/` defines the local and container topology with .NET Aspire 13.5.
- `src/Modules/` and `src/Shared/` are reserved solution folders for future projects.
- `deploy/docker-compose.yaml` is generated from the AppHost and committed for developers who only have Docker.
- Shared build settings live in `Directory.Build.props`, and centrally managed NuGet versions live in `Directory.Packages.props`.

## Build, Run, and Development Commands

Use the SDK pinned in `global.json` (10.0.400) and Aspire CLI 13.5 for the full developer workflow.

- `dotnet restore Noverina.slnx` restores solution dependencies.
- `dotnet build Noverina.slnx` builds all solution projects.
- `aspire run --apphost src/Noverina.AppHost` starts the API and the Aspire dashboard locally, and prints the dashboard login URL.
- `aspire deploy --apphost src/Noverina.AppHost -o deploy --non-interactive` builds the API image and starts the Compose stack.
- `aspire destroy --apphost src/Noverina.AppHost -o deploy --non-interactive --yes` stops that stack.
- `aspire publish --apphost src/Noverina.AppHost -o deploy --non-interactive --nologo` regenerates the committed Compose definition after AppHost changes.
- `docker compose -f deploy/docker-compose.yaml up --build -d` builds and starts the Docker-only stack. Stop it with `docker compose -f deploy/docker-compose.yaml down`.

The AppHost sets `AspireUseCliBundle=true`, so DCP and the dashboard come from the installed Aspire CLI rather than from NuGet packages. `dotnet run --project src/Noverina.AppHost` works only because it hands off to the CLI.

## Architecture

**The AppHost is the source of truth for the local topology.** `deploy/docker-compose.yaml` is generated output, so never edit it by hand. Change `AppHost.cs`, re-run `aspire publish ... -o deploy`, and commit the result; the output is deterministic. `deploy/.env*` is generated and git-ignored.

**Compose customizations are made in `AppHost.cs` through `PublishAsDockerComposeService`:**
- The dashboard is pinned to the stable `mcr.microsoft.com/dotnet/aspire-dashboard:13.5`, because Aspire's default is a floating nightly tag.
- The API image is `${API_IMAGE:-noverina-api:local}` and has a `build:` section.
  - `aspire deploy` fills `API_IMAGE` in `deploy/.env.Production`. Plain `docker compose` falls back to building the image locally.
  - The build context `..` is relative to `deploy/`, so always publish and deploy with `-o deploy`.
- The API runs as `ASPNETCORE_ENVIRONMENT=Development`. The Compose file is local-only, and Development is what exposes `/openapi/v1.json`.

**Both container flows build the API from `src/Noverina.Api/Dockerfile`.** The AppHost uses `PublishAsDockerFile`, and the build context is the repository root.
- The restore layer copies only `NuGet.Config`, `Directory.Packages.props`, `Directory.Build.props`, `global.json` and the API csproj.
- `dotnet publish` runs with `--no-restore`.
- When the API gains a project reference (a module or a shared project), add that csproj's `COPY` line before `dotnet restore`, or the image build breaks.

**Service defaults live inside the API on purpose,** in `src/Noverina.Api/Extensions/ServiceDefaultsExtensions.cs`. This covers OpenTelemetry, health checks, service discovery and HTTP resilience. Because there is a single host, do not introduce a separate ServiceDefaults project.
- `/health` and `/alive` are mapped in every environment and are excluded from tracing.
- The OTLP exporter is enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, which Aspire injects.

**Runtime quirks:**
- `aspire deploy` uses the Compose project name `aspire-compose-<hash>` instead of `noverina`, so `docker compose -f deploy/docker-compose.yaml down` does not stop a deploy stack. Use `aspire destroy` for that.
- Both container flows bind the same host ports, 8080 and 18888, so only one can run at a time.
- Each `aspire deploy` leaves `api:aspire-deploy-<timestamp>` images behind.

## Coding Style & Naming

Follow standard C# conventions: four-space indentation, file-scoped namespaces, PascalCase for types and public members, and camelCase for locals and parameters. Keep nullable reference types enabled. Place setup that is specific to the API host in `Noverina.Api`, and keep orchestration in `Noverina.AppHost`. Add package versions centrally and use versionless `PackageReference` entries in project files. No formatter or linter is currently configured.

## Testing Guidelines

There are currently no test projects and no coverage threshold. When adding tests, create a clearly named test project (for example, `Noverina.Api.Tests`). Use descriptive test names that state the behavior and the expected result. Run the relevant test project with `dotnet test <project.csproj>`, and use `dotnet build Noverina.slnx` to check that the solution still builds.

## Commits & Pull Requests

The repository has no commit history yet, so no established commit convention can be inferred. Use a short, imperative subject (for example, `Add API health endpoint`). Pull requests should:
- summarize the change
- note build and test results
- link related issues
- include screenshots only when UI or dashboard behavior changes

If `AppHost.cs` changes, include the regenerated `deploy/docker-compose.yaml`.

## Configuration & Secrets

Do not commit credentials or machine-specific secrets. Keep local overrides in ignored development configuration or in environment variables. Container ports are bound to `127.0.0.1`; keep that scope when changing deployment configuration.

The Compose dashboard has no custom keys.
- It uses its default browser-token login. The token appears in the container log and changes on every restart.
- OTLP (port 18889) is unsecured but reachable only inside the Compose network.
- If a stable secret is ever needed, use `builder.AddParameter(name, secret: true)` with `WithEnvironment`. Aspire then writes a `${...}` placeholder into the Compose file and a matching `.env` entry. Never write literal secrets into a Compose service.

## Upgrading Aspire

Keep these three in step:
- the `Aspire.AppHost.Sdk/<version>` in the AppHost csproj (Central Package Management does not cover MSBuild SDKs)
- `Aspire.Hosting.Docker` in `Directory.Packages.props`
- the dashboard image tag in `AppHost.cs`

The Mermaid diagrams in the README describe the run flows; update them when those flows change.
