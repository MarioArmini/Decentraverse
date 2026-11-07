#!/usr/bin/env bash
# Starts the indexer API (with watch) and the web app together. Ctrl+C stops both.
set -euo pipefail
cd "$(dirname "$0")"

dotnet build src/Decentraverse.Indexer -v q -nologo
# Run the built DLL directly: a single process that the trap below can stop cleanly.
dotnet src/Decentraverse.Indexer/bin/Debug/net10.0/Decentraverse.Indexer.dll serve --watch &
indexer=$!
trap 'kill "$indexer" 2>/dev/null || true' EXIT INT TERM

echo "Web app: http://localhost:5080 (indexer API: http://localhost:8787)"
dotnet run --project src/Decentraverse.Web
