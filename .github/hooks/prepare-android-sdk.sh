set -euo pipefail

SDK_BASE="/usr/lib/android/sdk/cmdline-tools/latest"

mkdir -p /cmdline-tools || true
mkdir -p /tools || true

if [ -d "$SDK_BASE" ]; then
  ln -sfn "$SDK_BASE" /cmdline-tools/latest || true
  ln -sfn "$SDK_BASE/bin" /tools/bin || true
fi

echo "== Android SDK check =="
ls -al /cmdline-tools || true
ls -al /tools || true
command -v sdkmanager || true