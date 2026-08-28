#!/usr/bin/env bash

set -euo pipefail

command -v pod >/dev/null 2>&1 || {
    printf 'error: CocoaPods is not on PATH.\n' >&2
    exit 1
}

export RUBYOPT="${RUBYOPT:+${RUBYOPT} }-rlogger"
exec pod "$@"
