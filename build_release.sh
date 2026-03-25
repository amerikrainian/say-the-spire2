#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Building mod (Release) ==="
dotnet build -c Release

echo "=== Building documentation ==="
cp changes.md docs_src/src/changes.md
mdbook build docs_src

echo "=== Adding docs to release zip ==="
uv run python scripts/add_docs_to_release.py

echo "=== Done ==="
echo "Release zip: SayTheSpire2.zip"
echo "Installer:   SayTheSpire2Installer.exe"
