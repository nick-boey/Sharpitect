#!/bin/bash
# Publishes the CLI and sets up an alias for local development

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

dotnet publish "$ROOT_DIR/src/Sharpitect.CLI" -c Release -o "$ROOT_DIR/src/Sharpitect.CLI/bin/Publish"

echo "To use sharpitect, run:"
echo "  alias sharpitect='$ROOT_DIR/src/Sharpitect.CLI/bin/Publish/Sharpitect.CLI'"
echo ""
echo "Or source this script to set the alias automatically:"
echo "  source $0"

alias sharpitect="$ROOT_DIR/src/Sharpitect.CLI/bin/Publish/Sharpitect.CLI"
