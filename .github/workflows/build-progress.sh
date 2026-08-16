#!/usr/bin/env bash
# Tails a Unity batch build log and appends major phase transitions to
# $GITHUB_STEP_SUMMARY, so the run summary page shows live build status.
# Usage: build-progress.sh <logfile> <platform-label>
# Marker strings verified against real build logs (run 31335181779, 2026-08-09);
# the same set appears on all four platforms.
set -u

log="$1"
platform="$2"

emit() {
    printf -- '- `%s` %s\n' "$(date -u +%H:%M:%S)" "$1" >> "$GITHUB_STEP_SUMMARY"
}

echo "## $platform build" >> "$GITHUB_STEP_SUMMARY"
emit "Build started, waiting for Unity log output"

while [ ! -f "$log" ]; do sleep 2; done

shaders_seen=0
tail -n +1 -F "$log" 2>/dev/null | while IFS= read -r line; do
    case "$line" in
        *"DisplayProgressbar: Compiling Scripts"*)
            emit "Compiling scripts (editor)" ;;
        *"Recompiling scripts for player build"*)
            emit "Compiling scripts (player)" ;;
        *'Compiling shader "'*)
            if [ "$shaders_seen" -eq 0 ]; then
                emit "Compiling shader variants"
            fi
            shaders_seen=$((shaders_seen + 1)) ;;
        *"DisplayProgressbar: Incremental Player Build"*)
            emit "Building player" ;;
        *"*** Tundra requires additional run"*)
            emit "IL2CPP native build pass" ;;
        *"***Player size statistics***"*)
            emit "Player size report" ;;
        *"SUCCESS BUILD"*)
            emit "✅ Build succeeded" ;;
        *"FAILED BUILD"*)
            emit "❌ Build FAILED" ;;
    esac
done
