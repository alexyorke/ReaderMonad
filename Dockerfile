## Non-production dev container for the ReaderMonad blog-post sample.
## Intended for learning/demo use only (not production hardened).
##
## Default image target is "dev" (SDK + tools + long-running command).
## You can also build a small runtime image via: docker build --target runtime -t reader-monad-runtime .

# -------- Dev container (default) --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /workspace

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# Useful dev tools (optional but handy in a dev container)
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Optional: install dotnet-format for formatting inside the container
RUN dotnet tool install -g dotnet-format
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy project files first for better layer caching (useful even when bind-mounting later)
COPY ReaderMonad.sln ./
COPY ReaderMonad/ReaderMonad.csproj ReaderMonad/
COPY ReaderMonad.Tests/ReaderMonad.Tests.csproj ReaderMonad.Tests/
RUN dotnet restore ./ReaderMonad.sln

# Copy the rest of the source so the image works even without a bind mount
COPY . ./

# Keep the container running for interactive dev (VS Code Dev Containers / docker exec, etc.)
CMD ["sleep", "infinity"]

# -------- CI-ish build (tests + publish) --------
FROM dev AS build
RUN dotnet test ./ReaderMonad.sln -c Release --no-restore
RUN dotnet publish ./ReaderMonad/ReaderMonad.csproj -c Release --no-restore -o /app/publish

# -------- Runtime image (optional) --------
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
ENV DOTNET_ROLL_FORWARD=Major
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ReaderMonad.dll"]

# -------- Final image (default): dev container --------
# Docker builds the last stage by default; make that the dev container.
FROM dev AS final
CMD ["sleep", "infinity"]

