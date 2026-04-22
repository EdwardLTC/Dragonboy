#!/usr/bin/env bash
set -euo pipefail

# Usage examples:
#   ./build.sh Android aab
#   ./build.sh Android apk "BuildArtifacts/{product}/{platform}/{version}_{datetime}"
#   ./build.sh StandaloneOSX
#   ./build.sh StandaloneWindows64

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_VERSION="2022.3.62f3"
UNITY_BIN="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"

TARGET="${1:-Android}"
ANDROID_FORMAT="${2:-}"        # apk | aab (optional)
PATTERN="${3:-BuildArtifacts/{product}/{platform}/{version}_{datetime}}"

if [[ ! -x "${UNITY_BIN}" ]]; then
  echo "Unity not found at: ${UNITY_BIN}" >&2
  echo "Edit UNITY_VERSION/UNITY_BIN in build.sh to match your install." >&2
  exit 2
fi

LOG_DIR="${PROJECT_DIR}/BuildArtifacts/_logs"
mkdir -p "${LOG_DIR}"
LOG_FILE="${LOG_DIR}/build_${TARGET}_$(date +%Y%m%d_%H%M%S).log"

"${UNITY_BIN}" \
  -batchmode -nographics -quit \
  -projectPath "${PROJECT_DIR}" \
  -executeMethod DragonBoy.Build.BuildPlayerCLI.Build \
  -buildTarget "${TARGET}" \
  ${ANDROID_FORMAT:+-androidFormat "${ANDROID_FORMAT}"} \
  -outputFolderPattern "${PATTERN}" \
  -logFile "${LOG_FILE}"

echo "Build log: ${LOG_FILE}"
