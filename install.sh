#!/bin/sh
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.
#
# Installs the Options+ plugin and sets up the Chrome extension:
#
#   curl -fsSL https://raw.githubusercontent.com/xfanta/onshape-mx/main/install.sh | sh
#
# Takes Onshape.lplug4 and onshape-mx-bridge.zip from the folder it is run from
# when they are there, and from the latest GitHub release otherwise.
#
#   --reset-profile   rebuild the Onshape profile from the plugin's defaults,
#                     dropping any changes made to it in Options+

set -eu

REPO="xfanta/onshape-mx"
RELEASE="https://github.com/$REPO/releases/latest/download"

LOGI="$HOME/Library/Application Support/Logi/LogiPluginService"
PLUGINS="$LOGI/Plugins"
APPLICATIONS="$LOGI/Applications"
LOG="$LOGI/Logs/plugin_logs/Onshape.log"
SERVICE="/Applications/Utilities/LogiPluginService.app"

# The extension lives here rather than in Downloads: Chrome loads an unpacked
# extension from its folder every time, so the folder has to stay.
EXTENSION="$HOME/Library/Application Support/Onshape MX/extension"

RESET_PROFILE=0
for arg in "$@"; do
    case "$arg" in
        --reset-profile) RESET_PROFILE=1 ;;
        *) echo "Unknown option: $arg" >&2; exit 2 ;;
    esac
done

say() { printf '\n\033[1m%s\033[0m\n' "$1"; }
fail() { printf '\n\033[31m%s\033[0m\n' "$1" >&2; exit 1; }

[ "$(uname -s)" = "Darwin" ] || fail "Onshape MX Creative is for macOS."
[ -d "$SERVICE" ] || fail "Logi Plugin Service is not installed. Install Logi Options+ first: https://www.logitech.com/software/logi-options-plus.html"

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

# Next to the script when run from a download folder; `curl | sh` has no such
# folder, and gets the release instead.
fetch() {
    name="$1"
    if [ -f "./$name" ]; then
        cp "./$name" "$WORK/$name"
        echo "Using ./$name"
    else
        echo "Downloading $name"
        curl -fsSL "$RELEASE/$name" -o "$WORK/$name" || fail "Could not download $name from $RELEASE"
    fi
}

say "1/3  The Options+ plugin"

fetch Onshape.lplug4

if [ -e "$PLUGINS/OnshapePlugin.link" ]; then
    echo "Note: $PLUGINS/OnshapePlugin.link points at a development build and"
    echo "      takes precedence over the installed plugin. Delete it to use this one."
fi

rm -rf "$PLUGINS/Onshape"
mkdir -p "$PLUGINS/Onshape"
unzip -q "$WORK/Onshape.lplug4" -d "$PLUGINS/Onshape"
echo "Installed to $PLUGINS/Onshape"

# Options+ builds a profile from the plugin's defaults only when it creates one.
# A first install has none; an update keeps the user's, unless asked otherwise.
if [ "$RESET_PROFILE" = 1 ]; then
    rm -rf "$APPLICATIONS"/Loupedeck7*/@_onshape
    echo "Removed the old Onshape profile; Options+ rebuilds it from the defaults."
fi

# Only what the log says after this point counts. Its lines start with a
# timestamp like 2026-09-27T09-43-38-797, which sorts as text.
STARTED=$(date +%Y-%m-%dT%H-%M-%S)
open "loupedeck://plugin/Onshape/reload" || true

say "2/3  The Chrome extension"

fetch onshape-mx-bridge.zip
rm -rf "$EXTENSION"
mkdir -p "$(dirname "$EXTENSION")"
unzip -q "$WORK/onshape-mx-bridge.zip" -d "$WORK/extension-zip"
mv "$WORK/extension-zip/extension" "$EXTENSION"
printf '%s' "$EXTENSION" | pbcopy
echo "Unpacked to $EXTENSION"

if [ -d "/Applications/Google Chrome.app" ]; then
    open -a "Google Chrome" "chrome://extensions" || true
fi

cat <<EOF

  In Chrome's Extensions page:
    1. switch on "Developer mode" (top right),
    2. click "Load unpacked",
    3. press Cmd+Shift+G, paste with Cmd+V - the folder is on the clipboard - and choose it.
  Updating later? Click the reload arrow on "MX Console Bridge for Onshape" instead.
EOF

say "3/3  Onshape as a Chrome app"

if grep -qs "onshape.com" "$HOME/Applications/Chrome Apps.localized/"*.app/Contents/Info.plist; then
    echo "Found it in ~/Applications/Chrome Apps."
else
    cat <<EOF
  Not installed yet. In Chrome, open https://cad.onshape.com and choose
  menu > Cast, save and share > Install page as app...
  The console switches to Onshape only when that app is in front.
EOF
fi

say "Checking the plugin"

new_log() {
    [ -f "$LOG" ] || return 0
    awk -v since="$STARTED" '$1 >= since' "$LOG"
}

# A first load can take the service half a minute.
i=0
while [ "$i" -lt 30 ]; do
    i=$((i + 1))
    if new_log | grep -q "Plugin 'Onshape' version"; then
        break
    fi
    sleep 1
done

if new_log | grep -q "Plugin 'Onshape' version"; then
    new_log | grep "Plugin 'Onshape' version" | tail -1 | sed 's/.*| INFO  | /  /'
    echo
    echo "Done. Open Onshape (reload any window that was already open) and the"
    echo "keypad follows it."
else
    echo "  The plugin has not reported in yet. If it does not appear in Options+,"
    echo "  quit and reopen Logi Options+, and see $LOG"
fi
