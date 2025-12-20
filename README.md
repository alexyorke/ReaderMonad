# ReaderMonad (Demo / Blog Post Code)

**Non-production use only.** This repository is sample code from a blog post / learning exercise.

## Prerequisites (local)

- .NET SDK 8 (or later SDKs that can build `net8.0`)

## Build / test / run (local)

- **Restore**:

```bash
dotnet restore .\ReaderMonad.sln
```

- **Run tests**:

```bash
dotnet test .\ReaderMonad.sln
```

- **Run the console app**:

```bash
dotnet run --project .\ReaderMonad\ReaderMonad.csproj
```

## Docker (non-production demo)

This Docker setup is intended for **demo/learning** only (not hardened for production).

### Dev container (recommended)

The root `Dockerfile` is a **dev container** by default (SDK + tools + long-running command).

Build the dev image:

```bash
docker build -t reader-monad-dev .
```

Run an interactive dev shell (bind-mounts your working tree into the container):

```bash
docker run --rm -it -v ${PWD}:/workspace -w /workspace reader-monad-dev bash
```

Inside the container:

```bash
dotnet test ./ReaderMonad.sln
dotnet format ./ReaderMonad.sln
dotnet run --project ./ReaderMonad/ReaderMonad.csproj
```

If you use VS Code Dev Containers, a minimal `.devcontainer/devcontainer.json` is included.

### Runtime image (optional)

Build a small image that just runs the demo console app:

```bash
docker build --target runtime -t reader-monad-runtime .
```

Run it:

```bash
docker run --rm reader-monad-runtime
```

## Dockerfile

The repository includes a `Dockerfile` at the root. It uses a multi-stage build:

- `dev` (default): `mcr.microsoft.com/dotnet/sdk:8.0` with dev tooling and `CMD ["sleep","infinity"]`
- `runtime` (optional): `mcr.microsoft.com/dotnet/runtime:8.0` running the published console app


