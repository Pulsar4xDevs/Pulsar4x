#!/bin/bash
# Pulsar4X Native Deps Builder
# Builds from edwardgushchin's forks using 'main' for natives and 'master' for SDL3-CS.
# Patches both TargetFrameworks and LangVersion so it builds cleanly with .NET 8 SDK.

set -euo pipefail

# ================== CONFIGURATION ==================
SDL_BRANCH="main"           
SDL_IMAGE_BRANCH="main"     
SDL_TTF_BRANCH="main"       
SDL3_CS_BRANCH="master"     

TARGET_FRAMEWORK="net8.0"
LANG_VERSION="12"                     # .NET 8 SDK max = C# 12

BUILD_DIR="$(pwd)/build-dir"
OUTPUT_DIR="$(pwd)/../../Pulsar4X.Client/Libs/linux-x64/"

IMAGE="registry.gitlab.steamos.cloud/steamrt/sniper/sdk:latest"

SDL_REPO="https://github.com/edwardgushchin/SDL.git"
SDL_IMAGE_REPO="https://github.com/edwardgushchin/SDL_image.git"
SDL_TTF_REPO="https://github.com/edwardgushchin/SDL_ttf.git"
SDL3_CS_REPO="https://github.com/edwardgushchin/SDL3-CS.git"
# ===================================================

echo -e "\e[34m=== Starting build in Steam Runtime SDK ===\e[0m"

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
      libjpeg-dev libpng-dev libwebp-dev libtiff-dev \
      libfreetype6-dev libharfbuzz-dev

    # Install .NET 8 SDK
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    apt update
    apt install -y dotnet-sdk-8.0

    cd /work

    # === Build SDL3 native ===
    echo -e '\e[34m=== Build SDL3 native from fork (main) ===\e[0m'
    rm -rf SDL
    git clone --depth 1 --branch $SDL_BRANCH $SDL_REPO
    cd SDL
    mkdir build && cd build
    cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON
    make -j\$(nproc)
    cp libSDL3.so* /output/

    export PKG_CONFIG_PATH=\"/work/SDL/build\${PKG_CONFIG_PATH:+:\$PKG_CONFIG_PATH}\"

    # === Build SDL_image ===
    echo -e '\e[34m=== Build SDL_image from fork (main) ===\e[0m'
    cd /work
    rm -rf SDL_image
    git clone --depth 1 --branch $SDL_IMAGE_BRANCH $SDL_IMAGE_REPO
    cd SDL_image
    mkdir build && cd build
    cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON
    make -j\$(nproc)
    cp libSDL3_image.so* /output/

    # === Build SDL_ttf ===
    echo -e '\e[34m=== Build SDL_ttf from fork (main) ===\e[0m'
    cd /work
    rm -rf SDL_ttf
    git clone --depth 1 --branch $SDL_TTF_BRANCH $SDL_TTF_REPO
    cd SDL_ttf
    mkdir build && cd build
    cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON
    make -j\$(nproc)
    cp libSDL3_ttf.so* /output/

    # === Build SDL3-CS .dll ===
    echo -e '\e[34m=== Build SDL3-CS .dll from master ===\e[0m'
    cd /work
    rm -rf SDL3-CS
    git clone --depth 1 --branch $SDL3_CS_BRANCH $SDL3_CS_REPO

    MAIN_CSPROJ=\"SDL3-CS/SDL3-CS/SDL3-CS.csproj\"

    # Patch both settings
    sed -i \"s/<TargetFrameworks>.*<\/TargetFrameworks>/<TargetFrameworks>$TARGET_FRAMEWORK<\/TargetFrameworks>/\" \$MAIN_CSPROJ
    sed -i \"s/<LangVersion>.*<\/LangVersion>/<LangVersion>$LANG_VERSION<\/LangVersion>/\" \$MAIN_CSPROJ

    # === Feedback so we can see it worked ===
    echo '=== After patching ==='
    grep -E 'TargetFrameworks|LangVersion' \$MAIN_CSPROJ

    # Build ONLY the main library (skip all examples)
    dotnet build \$MAIN_CSPROJ -c Release --no-self-contained

    cp SDL3-CS/SDL3-CS/bin/Release/$TARGET_FRAMEWORK/SDL3-CS.dll /output/

    echo -e '\e[34m=== All builds finished successfully ===\e[0m'
  "

echo -e "\e[34m✅ Build complete!\e[0m"
echo "   Output files are in: $OUTPUT_DIR"
echo "      they should automaticaly be copied to the pulsar bin directory when pulsar is built"
