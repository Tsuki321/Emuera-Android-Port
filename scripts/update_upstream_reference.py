"""Refresh reference sources from the exact upstream.json revision. Does not build.

Usage: python scripts/update_upstream_reference.py PATH_TO_UPSTREAM_GIT_CHECKOUT
The Android engine is adapted separately; this script never changes it.
"""
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def git(repo, *args):
    return subprocess.check_output(["git", "-C", str(repo), *args])


def main():
    repo = Path(sys.argv[1]).resolve()
    manifest = json.loads((ROOT / "upstream.json").read_text(encoding="utf-8"))
    revision = manifest["revision"]
    if git(repo, "rev-parse", revision + "^{commit}").decode().strip() != revision:
        raise RuntimeError("Upstream revision does not match the manifest")
    directories = manifest["reference_directories"]
    paths = git(repo, "ls-tree", "-r", "--name-only", "-z", revision, "--", *directories).decode().split("\0")
    paths = {p for p in paths if p}
    old = git(ROOT, "ls-files", "-z", "--", *directories).decode().split("\0")
    for name in set(old) - paths - {""}:
        destination = (ROOT / name).resolve()
        if not any(destination.is_relative_to(ROOT / directory) for directory in directories):
            raise RuntimeError("Reference path outside the intended directories")
        if destination.exists():
            destination.unlink()
    for name in sorted(paths):
        destination = (ROOT / name).resolve()
        if not any(destination.is_relative_to(ROOT / directory) for directory in directories):
            raise RuntimeError("Reference path outside the intended directories")
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(git(repo, "show", f"{revision}:{name}"))
    print(f"Updated {len(paths)} reference files to {revision}")


if __name__ == "__main__":
    main()
