#!/bin/zsh

# --- Configuration ---
PROJECT_NAME="KeepSoundbarAwake"
PROJECT_FILE="${PROJECT_NAME}/${PROJECT_NAME}.csproj" # Assuming path relative to script
RELEASE_DIR_ROOT="dist" # Root directory for all releases locally
VERSION="v0.2.1" # Manually update this for each new release!

./
# --- Native Libraries Source Paths (relative to script location, assuming they are in the project root) ---
BASS_NATIVE_MAC="KeepSoundbarAwake/libbass.dylib"
BASS_NATIVE_WIN="KeepSoundbarAwake/bass.dll"
BASS_NATIVE_LINUX_X64="KeepSoundbarAwake/libbass_x86_64.so"
BASS_NATIVE_LINUX_ARM64="KeepSoundbarAwake/libbass_aarch64.so"

# --- RIDs to Build ---
RIDS=(
    "win-x64"
    "osx-x64"
    "osx-arm64"
    "linux-x64"
    "linux-arm64"
)

# --- Start the Build Process ---
echo "--- Starting Release Build for ${PROJECT_NAME} (Version: ${VERSION}) ---"

# 1. Clean previous build outputs
echo "1. Cleaning local build artifacts..."
rm -rf "bin/Release"
rm -rf "${RELEASE_DIR_ROOT}/${VERSION}"
mkdir -p "${RELEASE_DIR_ROOT}/${VERSION}"
echo "Cleanup complete."

# 2. Build and Package for each RID
for RID in "${RIDS[@]}"; do
    echo "--- Building and Packaging for RID: ${RID} ---"
    
    # Define a temporary folder where we'll assemble the package contents for this RID
    ASSEMBLE_DIR="${RELEASE_DIR_ROOT}/_assemble_${RID}"
    rm -rf "${ASSEMBLE_DIR}" # Clean previous assembly temp dir
    mkdir -p "${ASSEMBLE_DIR}"

    PUBLISHED_EXECUTABLE_NAME="${PROJECT_NAME}"
    NATIVE_LIBRARY_TO_COPY="" # Which native library to copy for this RID
    FINAL_ZIP_NAME="${PROJECT_NAME}-${RID}"

    # Set platform-specific names and libraries
    case "${RID}" in
        "win-x64"|"win-x86"|"win-arm64")
            PUBLISHED_EXECUTABLE_NAME="${PROJECT_NAME}.exe"
            NATIVE_LIBRARY_TO_COPY="${BASS_NATIVE_WIN}"
            ;;
        "osx-x64"|"osx-arm64")
            NATIVE_LIBRARY_TO_COPY="${BASS_NATIVE_MAC}"
            ;;
        "linux-x64")
            NATIVE_LIBRARY_TO_COPY="${BASS_NATIVE_LINUX_X64}"
            ;;
        "linux-arm64")
            NATIVE_LIBRARY_TO_COPY="${BASS_NATIVE_LINUX_ARM64}"
            ;;
        *)
            echo "Warning: Unrecognized RID ${RID}. Skipping native library copy."
            ;;
    esac

    # --- Publish the self-contained single-file executable ---
    # Publish directly to our assemble directory to avoid extra copying of the main executable
    dotnet publish "${PROJECT_FILE}" -c Release -r "${RID}" --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o "${ASSEMBLE_DIR}"

    # Check if publish was successful
    if [ $? -ne 0 ]; then
        echo "Error: Publish failed for ${RID}. Aborting."
        # rm -rf "${ASSEMBLE_DIR}" # Clean up temp
        exit 1
    fi

    # --- Copy the required native library alongside the executable ---
    # The dotnet publish with single file embedding might not correctly embed Bass.
    # So we ensure the native library is always placed next to the executable.
    if [[ -n "${NATIVE_LIBRARY_TO_COPY}" && -f "${NATIVE_LIBRARY_TO_COPY}" ]]; then
        echo "Copying native library: ${NATIVE_LIBRARY_TO_COPY} to ${ASSEMBLE_DIR}"
        cp "${NATIVE_LIBRARY_TO_COPY}" "${ASSEMBLE_DIR}/"
    else
        echo "Warning: Native library ${NATIVE_LIBRARY_TO_COPY} not found or not specified for RID ${RID}. Might cause runtime errors."
    fi

    # --- Apply xattr for macOS binaries (both executable and dylib) ---
    if [[ "${RID}" == "osx-x64" || "${RID}" == "osx-arm64" ]]; then
        echo "Applying xattr cleanup for macOS binary and dylib in ${ASSEMBLE_DIR}"
        xattr -d com.apple.quarantine "${ASSEMBLE_DIR}/${PUBLISHED_EXECUTABLE_NAME}" 2>/dev/null || true
        xattr -d com.apple.quarantine "${ASSEMBLE_DIR}/libbass.dylib" 2>/dev/null || true
    fi

    # --- Create the zip archive ---
    echo "Creating zip archive for ${RID}..."
    mkdir -p "$(dirname "${RELEASE_DIR_ROOT}/${VERSION}/${FINAL_ZIP_NAME}")"
    # cd into the assemble directory to zip its contents directly
    cd "${ASSEMBLE_DIR}" 
    zip -r "${FINAL_ZIP_NAME}.zip" ./*
    
    # Check if zip was successful
    if [ $? -eq 0 ]; then
        echo "Successfully packaged ${FINAL_ZIP_NAME}"
    else
        echo "Error: Failed to create zip archive for ${RID}. Aborting."
        # rm -rf "${ASSEMBLE_DIR}"
        exit 1
    fi

    cp "${FINAL_ZIP_NAME}.zip" "../../${RELEASE_DIR_ROOT}/${VERSION}/"
    cd ../..

    # Clean up the temporary assemble directory
    rm -rf "${ASSEMBLE_DIR}"

done

echo "--- All builds and packages complete! ---"
echo "Release assets prepared in: ${RELEASE_DIR_ROOT}/${VERSION}/"
echo "You can now draft a new release on GitHub and upload these ZIP files."
echo "Consider creating a Git tag: git tag -a ${VERSION} -m \"Release ${VERSION}\" && git push --tags"