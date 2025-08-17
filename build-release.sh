#!/bin/zsh
# Or #!/bin/bash for Bash

# --- Configuration ---
PROJECT_NAME="KeepSoundbarAwake"
PROJECT_FILE="${PROJECT_NAME}/${PROJECT_NAME}.csproj" # Assuming path relative to script
RELEASE_DIR_ROOT="dist" # Root directory for all releases locally
VERSION="v0.2" # Manually update this for each new release! Or get from CI/CD.

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
rm -rf "bin/Release" # Clean the primary release output
rm -rf "${RELEASE_DIR_ROOT}/${VERSION}" # Clean previous specific version release assets
mkdir -p "${RELEASE_DIR_ROOT}/${VERSION}" # Create target dir for this version
echo "Cleanup complete."

# 2. Build for each RID
for RID in "${RIDS[@]}"; do
    echo "--- Building for RID: ${RID} ---"
    
    # --- FIX: Define the publish output directory explicitly ---
    # We will tell dotnet publish where to put the output directly,
    # making the path predictable. This is often cleaner.
    CURRENT_PUBLISH_DIR="temp_publish_output/${RID}" # Temporary directory for each RID's publish output
    
    # Ensure this temporary directory is clean before publishing to it
    rm -rf "${CURRENT_PUBLISH_DIR}"
    mkdir -p "${CURRENT_PUBLISH_DIR}"

    TARGET_EXECUTABLE="${PROJECT_NAME}" # For Linux/macOS
    if [[ "${RID}" == "win-x64" || "${RID}" == "win-x86" || "${RID}" == "win-arm64" ]]; then
        TARGET_EXECUTABLE="${PROJECT_NAME}.exe"
    fi

    # Run the publish command
    # Use -o (or --output) to specify the exact output directory
    dotnet publish "${PROJECT_FILE}" -c Release -r "${RID}" --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o "${CURRENT_PUBLISH_DIR}"

    # Check if publish was successful
    if [ $? -eq 0 ]; then
        echo "Build successful for ${RID}. Copying to release directory."
        
        # Now, the executable should be directly in CURRENT_PUBLISH_DIR
        if [ -f "${CURRENT_PUBLISH_DIR}/${TARGET_EXECUTABLE}" ]; then
            cp "${CURRENT_PUBLISH_DIR}/${TARGET_EXECUTABLE}" "${RELEASE_DIR_ROOT}/${VERSION}/${PROJECT_NAME}-${RID}"
            
            # --- IMPORTANT: Apply xattr for macOS binaries ---
            if [[ "${RID}" == "osx-x64" || "${RID}" == "osx-arm64" ]]; then
                echo "Applying xattr cleanup for macOS binary: ${RELEASE_DIR_ROOT}/${VERSION}/${PROJECT_NAME}-${RID}"
                xattr -d com.apple.quarantine "${RELEASE_DIR_ROOT}/${VERSION}/${PROJECT_NAME}-${RID}" 2>/dev/null || true
            fi

            echo "Copied ${PROJECT_NAME}-${RID} to ${RELEASE_DIR_ROOT}/${VERSION}/"
        else
            echo "Error: Executable ${TARGET_EXECUTABLE} not found in ${CURRENT_PUBLISH_DIR} for ${RID}. Something went wrong with publish output."
        fi
    else
        echo "Error: Build failed for ${RID}. Aborting."
        exit 1 # Exit script with an error code
    fi
done

echo "--- All builds complete! ---"
echo "Release assets prepared in: ${RELEASE_DIR_ROOT}/${VERSION}/"
echo "You can now draft a new release on GitHub and upload these files."
echo "Consider creating a Git tag: git tag -a ${VERSION} -m \"Release ${VERSION}\" && git push --tags"