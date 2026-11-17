#!/bin/bash
# Build BDArmory Lite. Usage: ./build.sh [extra dotnet args]
# Set KSPManaged=/path/to/KSP_Data/Managed if your Managed folder lives elsewhere.
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 PATH="$PATH:$HOME/.dotnet"
cd "$(dirname "$0")/src" || exit 1
args=(-c Release -o ../build -v q -nologo -clp:NoSummary)
[ -n "$KSPManaged" ] && args+=("-p:KSPManaged=$KSPManaged")
dotnet build "${args[@]}" "$@"
