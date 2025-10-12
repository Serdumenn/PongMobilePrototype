#!/usr/bin/env bash
set -euo pipefail

# UnityCI imajındaki gerçek yol:
SDK_BASE="/usr/lib/android/sdk/cmdline-tools/latest"

# Unity Builder v4'ün aradığı sahte yolları oluştur.
mkdir -p /cmdline-tools || true
mkdir -p /tools || true

if [ -d "$SDK_BASE" ]; then
  ln -sfn "$SDK_BASE" /cmdline-tools/latest || true
  ln -sfn "$SDK_BASE/bin" /tools/bin || true
fi

# Tanı teşhis için kısa çıktı (logda göreceğiz)
echo "== Android SDK check =="
ls -al /cmdline-tools || true
ls -al /tools || true
command -v sdkmanager || true