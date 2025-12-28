#!/bin/bash
# Packs and installs Sharpitect as a local .NET tool

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
NUPKG_DIR="$ROOT_DIR/nupkg"
CLI_PROJECT="$ROOT_DIR/src/Sharpitect.CLI/Sharpitect.CLI.csproj"

dotnet tool uninstall Sharpitect --global || true

echo -e "\033[36mPacking Sharpitect.CLI...\033[0m"
dotnet pack "$CLI_PROJECT" -c Release -o "$NUPKG_DIR"

echo -e "\n\033[36mInstalling Sharpitect tool...\033[0m"
dotnet tool install --global --add-source "$NUPKG_DIR" Sharpitect

echo -e "\n\033[32mSharpitect installed successfully!\033[0m"
echo "Run 'sharpitect --help' to get started."
