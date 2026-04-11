#!/bin/bash
# Pulsar4X ImGui Deps Builder
# Builds cimgui (native) from behindcurtain3/ImGui.NET-nativebuild (using its submodule + build logic)
# and ImGui.NET (C# wrapper) from behindcurtain3/ImGui.NET
# Runs inside Steam Runtime sniper for maximum Linux compatibility.

set -euo pipefail

# ================== CONFIGURATION ==================
CIMGUI_BRANCH="v1.92.7"        # ImGui.NET-nativebuild tag
IMGUI_NET_BRANCH="v1.92.7"     # ImGui.NET tag

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build-dir"
OUTPUT_DIR="$SCRIPT_DIR/../../Pulsar4X.Client/Libs/linux-x64"

IMAGE="registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest"

CIMGUI_REPO="https://github.com/behindcurtain3/ImGui.NET-nativebuild.git"
IMGUI_NET_REPO="https://github.com/behindcurtain3/ImGui.NET.git"
# ===================================================

echo -e "\e[34m=== Starting ImGui build in Steam Runtime SDK ===\e[0m"

mkdir -p "$BUILD_DIR"
mkdir -p "$OUTPUT_DIR"

docker pull "$IMAGE"

docker run --rm \
  -v "$BUILD_DIR:/work" \
  -v "$OUTPUT_DIR:/output" \
  "$IMAGE" /bin/bash -c "
    set -euo pipefail
    set -x

    apt update
    apt install -y build-essential cmake git pkg-config wget \
      libgl1-mesa-dev libgles2-mesa-dev libegl1-mesa-dev \
      libx11-dev libxi-dev libxrandr-dev libxinerama-dev \
      libxcursor-dev libxext-dev libwayland-dev libxkbcommon-dev \
      libfreetype6-dev libharfbuzz-dev

    # Install .NET 8 SDK
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    apt update
    apt install -y dotnet-sdk-8.0

    cd /work

    # === Build cimgui (native library) using ImGui.NET-nativebuild ===
    echo -e '\e[34m=== Build cimgui native from ImGui.NET-nativebuild ===\e[0m'
    rm -rf ImGui.NET-nativebuild
    git clone --depth 1 --branch $CIMGUI_BRANCH $CIMGUI_REPO
    cd ImGui.NET-nativebuild

    # Initialise the cimgui submodule (exactly as their build process does)
    git submodule update --init --recursive

    # Build cimgui (following their standard Linux flow)
    cd cimgui
    mkdir -p build && cd build
    cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON \
             -DIMGUI_IMPL_SDL=ON -DIMGUI_IMPL_OPENGL3=ON
    make -j\$(nproc)

    echo 'Looking for cimgui output files...'
        ls -la .  # feedback to see what's actually there

    cp cimgui.so* /output/

    cd /work

    # === Build ImGui.NET .dll ===
    echo -e '\e[34m=== Build ImGui.NET .dll ===\e[0m'
    rm -rf ImGui.NET
    git clone --depth 1 --branch $IMGUI_NET_BRANCH $IMGUI_NET_REPO
    cd ImGui.NET

    # Clean build with suppressed warnings
    dotnet build src/ImGui.NET/ImGui.NET.csproj -c Release --no-self-contained \
      --nologo -v minimal --property:WarningLevel=0

    # Copy the dll
    echo 'Looking for ImGui output files...'
    cd /work/ImGui.NET/bin/Release/ImGui.NET/net8.0/
            ls -la .  # feedback to see what's actually there
    cp /work/ImGui.NET/bin/Release/ImGui.NET/net8.0/ImGui.NET.dll /output/

    echo -e '\e[34m=== All ImGui builds finished successfully ===\e[0m'
  "

echo -e "\e[34m✅ ImGui build complete!\e[0m"
echo "   Output files are in: $OUTPUT_DIR"
echo "      they should automatically be copied to the pulsar bin directory when pulsar is built"