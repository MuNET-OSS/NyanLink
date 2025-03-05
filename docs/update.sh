#!/usr/bin/env bash
set -e

# Change these to match your GitHub account/repo
GITHUB_OWNER="MewoLab"
GITHUB_REPO="worldlinkd"

# Directory and jar name
INSTALL_DIR="/app/wl"
JAR_NAME="worldlinkd.jar"

# echo "[INFO] Fetching the latest release data from GitHub..."
# LATEST_RELEASE_JSON=$(curl -s https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/releases/latest)
#
# # Parse the .jar download URL from the release assets
# DOWNLOAD_URL=$(echo "${LATEST_RELEASE_JSON}" \
#   | grep "browser_download_url" \
#   | grep ".jar" \
#   | cut -d '"' -f 4)
#
# if [ -z "${DOWNLOAD_URL}" ]; then
#   echo "[ERROR] Unable to find a .jar download URL in the latest release."
#   exit 1
# fi

DOWNLOAD_URL="https://github.com/MewoLab/worldlinkd/releases/latest/download/worldlinkd.jar"

echo "[INFO] Downloading JAR from: ${DOWNLOAD_URL}"
curl -L -o "${INSTALL_DIR}/${JAR_NAME}" "${DOWNLOAD_URL}"

echo "[INFO] Setting permissions on the jar file..."
chown worldlinkd:worldlinkd "${INSTALL_DIR}/${JAR_NAME}"
chmod 750 "${INSTALL_DIR}/${JAR_NAME}"

echo "[INFO] Update complete."
