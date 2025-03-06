#!/usr/bin/env bash
set -e

# Directory and jar name
INSTALL_DIR="/app/wl"
JAR_NAME="worldlinkd.jar"

DOWNLOAD_URL="https://github.com/MewoLab/worldlinkd/releases/latest/download/worldlinkd.jar"

echo "[INFO] Downloading JAR from: ${DOWNLOAD_URL}"
curl -L -o "${INSTALL_DIR}/${JAR_NAME}" "${DOWNLOAD_URL}"

echo "[INFO] Setting permissions on the jar file..."
chown worldlinkd:worldlinkd "${INSTALL_DIR}/${JAR_NAME}"
chmod 750 "${INSTALL_DIR}/${JAR_NAME}"

echo "[INFO] Update complete."
