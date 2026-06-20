#!/bin/bash
# Runs dotnet format --verify-no-changes on the relevant project after Edit or Write.
# Reads tool input JSON from stdin.

input=$(cat)

# Extract file_path from the tool input JSON
file=$(python3 -c "
import sys, json
try:
    d = json.loads(sys.argv[1])
    print(d.get('tool_input', {}).get('file_path', ''))
except Exception:
    print('')
" "$input" 2>/dev/null)

if [[ "$file" == *.cs ]] && [ -n "$file" ]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

    # Route to the project that owns this file
    if [[ "$file" == *"/Pulsar4X.Client/"* ]]; then
        proj="$PROJECT_ROOT/Pulsar4X/Pulsar4X.Client/Pulsar4X.Client.csproj"
    elif [[ "$file" == *"/Pulsar4X.Tests/"* ]]; then
        proj="$PROJECT_ROOT/Pulsar4X/Pulsar4X.Tests/Pulsar4X.Tests.csproj"
    elif [[ "$file" == *"/GameEngine/"* ]]; then
        proj="$PROJECT_ROOT/Pulsar4X/GameEngine/GameEngine.csproj"
    else
        proj="$PROJECT_ROOT/Pulsar4X/Pulsar4X.sln"
    fi

    if command -v dotnet &>/dev/null; then
        echo "[format] checking $(basename "$file") via $(basename "$proj")"
        dotnet format "$proj" --verify-no-changes 2>&1 | tail -5 || true
    else
        echo "[format] dotnet not found — skipping"
    fi
fi
