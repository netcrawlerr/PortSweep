#!/usr/bin/env bash

set -e

APP_NAME="PortSweep"
LOWER_APP_NAME="portsweep"
VERSION="${1:-1.0.1}"
TARGET_FRAMEWORK="net10.0"
RID="linux-x64"

# directories
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
PROJECT_PATH="${ROOT_DIR}/src/PortSweep.Desktop/PortSweep.Desktop.csproj"
DIST_DIR="${ROOT_DIR}/dist"

WORK_DIR="$(mktemp -d -t portsweep_build_XXXXXX)"
PUBLISH_DIR="${WORK_DIR}/publish"

cleanup() {
  rm -rf "${WORK_DIR}"
}
trap cleanup EXIT

echo "=========================================="
echo " Building ${APP_NAME} v${VERSION} for Linux (${RID})"
echo "=========================================="

rm -rf "${DIST_DIR}"
mkdir -p "${DIST_DIR}" "${PUBLISH_DIR}"

echo "[1/4] Publishing self-contained executable..."
dotnet publish "${PROJECT_PATH}" \
  -c Release \
  -r "${RID}" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishReadyToRun=true \
  -o "${PUBLISH_DIR}"

chmod +x "${PUBLISH_DIR}/PortSweep.Desktop"

echo "[2/4] Creating Tarball archive (.tar.gz)..."
TARBALL_NAME="${LOWER_APP_NAME}-${VERSION}-${RID}.tar.gz"
tar -czf "${DIST_DIR}/${TARBALL_NAME}" -C "${PUBLISH_DIR}" .
echo "  -> Created ${DIST_DIR}/${TARBALL_NAME}"

echo "[3/4] Building Debian package (.deb)..."

DEB_ROOT="${WORK_DIR}/deb_root"
DEB_OPT_DIR="${DEB_ROOT}/opt/${LOWER_APP_NAME}"
mkdir -p "${DEB_OPT_DIR}"
mkdir -p "${DEB_ROOT}/usr/bin"
mkdir -p "${DEB_ROOT}/usr/share/applications"
mkdir -p "${DEB_ROOT}/usr/share/icons/hicolor/512x512/apps"
mkdir -p "${DEB_ROOT}/DEBIAN"

cp -r "${PUBLISH_DIR}/"* "${DEB_OPT_DIR}/"

ln -s "/opt/${LOWER_APP_NAME}/PortSweep.Desktop" "${DEB_ROOT}/usr/bin/${LOWER_APP_NAME}"

ICON_SRC="${ROOT_DIR}/src/PortSweep.Desktop/Assets/logo.png"
if [ -f "${ICON_SRC}" ]; then
  cp "${ICON_SRC}" "${DEB_ROOT}/usr/share/icons/hicolor/512x512/apps/${LOWER_APP_NAME}.png"
fi

# Create Desktop entry
cat <<EOF > "${DEB_ROOT}/usr/share/applications/${LOWER_APP_NAME}.desktop"
[Desktop Entry]
Name=${APP_NAME}
Comment=Active Socket & Process Manager
Exec=${LOWER_APP_NAME}
Icon=${LOWER_APP_NAME}
Terminal=false
Type=Application
Categories=Utility;System;Network;
StartupWMClass=PortSweep
EOF

cat <<EOF > "${DEB_ROOT}/DEBIAN/control"
Package: ${LOWER_APP_NAME}
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: PortSweep Developers <developer@localhost>
Description: Active Socket & Process Manager
 PortSweep is a cross-platform desktop utility built with Avalonia UI
 for monitoring listening ports, socket activity, and process termination.
EOF

if command -v dpkg-deb &> /dev/null; then
  DEB_FILE="${DIST_DIR}/${LOWER_APP_NAME}_${VERSION}_amd64.deb"
  dpkg-deb --root-owner-group --build "${DEB_ROOT}" "${DEB_FILE}"
  echo "  -> Created ${DEB_FILE}"
else
  echo "  [Skipped] 'dpkg-deb' is not installed."
fi

echo "[4/4] Building AppImage..."

APPDIR="${WORK_DIR}/AppDir"
mkdir -p "${APPDIR}/usr/bin"
mkdir -p "${APPDIR}/usr/share/icons/hicolor/512x512/apps"

cp -r "${PUBLISH_DIR}/"* "${APPDIR}/usr/bin/"

if [ -f "${ICON_SRC}" ]; then
  cp "${ICON_SRC}" "${APPDIR}/usr/share/icons/hicolor/512x512/apps/${LOWER_APP_NAME}.png"
  cp "${ICON_SRC}" "${APPDIR}/${LOWER_APP_NAME}.png"
  cp "${ICON_SRC}" "${APPDIR}/.DirIcon"
fi

cat <<EOF > "${APPDIR}/${LOWER_APP_NAME}.desktop"
[Desktop Entry]
Name=${APP_NAME}
Comment=Active Socket & Process Manager
Exec=PortSweep.Desktop
Icon=${LOWER_APP_NAME}
Terminal=false
Type=Application
Categories=Utility;System;Network;
EOF

cat <<'EOF' > "${APPDIR}/AppRun"
#!/usr/bin/env bash
HERE="$(dirname "$(readlink -f "${0}")")"
export PATH="${HERE}/usr/bin:${PATH}"
exec "${HERE}/usr/bin/PortSweep.Desktop" "$@"
EOF
chmod +x "${APPDIR}/AppRun"

APPIMAGE_FILE="${DIST_DIR}/${APP_NAME}-${VERSION}-x86_64.AppImage"

# Download and extract appimagetool to bypass FUSE (libfuse.so.2) requirement on modern Linux
LOCAL_TOOL="${WORK_DIR}/appimagetool"
if command -v curl &> /dev/null; then
  curl -sSL -o "${LOCAL_TOOL}" "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
elif command -v wget &> /dev/null; then
  wget -q -O "${LOCAL_TOOL}" "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
else
  echo "  [Error] Neither curl nor wget found. Cannot download appimagetool."
  exit 1
fi

chmod +x "${LOCAL_TOOL}"

cd "${WORK_DIR}"
./appimagetool --appimage-extract > /dev/null
EXTRACTED_TOOL="${WORK_DIR}/squashfs-root/AppRun"

ARCH=x86_64 "${EXTRACTED_TOOL}" "${APPDIR}" "${APPIMAGE_FILE}"
echo "  -> Created ${APPIMAGE_FILE}"

# ---------------------------------------------------------
# Done
# ---------------------------------------------------------
echo ""
echo "=========================================="
echo " Build Completed successfully!"
echo " Outputs location: ${DIST_DIR}"
ls -lh "${DIST_DIR}"
echo "=========================================="
