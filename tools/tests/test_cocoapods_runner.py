from __future__ import annotations

import os
import stat
import subprocess
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
RUNNER = PROJECT_ROOT / "tools" / "run_cocoapods.sh"
RELEASE_SCRIPT = PROJECT_ROOT / "tools" / "mac_setup_and_build.sh"


class CocoaPodsRunnerTests(unittest.TestCase):
    def test_preloads_ruby_logger_for_cocoapods(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            fake_pod = Path(temporary_directory) / "pod"
            fake_pod.write_text(
                "#!/usr/bin/env bash\n"
                "set -euo pipefail\n"
                "ruby -e 'abort(\"Logger was not preloaded\") unless "
                "defined?(Logger); print Logger.name'\n",
                encoding="utf-8",
            )
            fake_pod.chmod(fake_pod.stat().st_mode | stat.S_IXUSR)

            environment = os.environ.copy()
            environment["PATH"] = f"{temporary_directory}:{environment['PATH']}"
            environment.pop("RUBYOPT", None)
            result = subprocess.run(
                ["bash", str(RUNNER), "--version"],
                cwd=PROJECT_ROOT,
                env=environment,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "Logger")

    def test_release_script_can_be_sourced_without_running_main(self) -> None:
        environment = os.environ.copy()
        environment["TDOF_RELEASE_SCRIPT"] = str(RELEASE_SCRIPT)
        result = subprocess.run(
            [
                "bash",
                "-c",
                "set -- source-only-sentinel; "
                "source \"$TDOF_RELEASE_SCRIPT\"; "
                "declare -F validate_export >/dev/null",
            ],
            cwd=PROJECT_ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
