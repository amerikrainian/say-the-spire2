"""Build a release zip for SayTheSpire2."""

import subprocess
import sys
import zipfile
from pathlib import Path

REPO_ROOT = Path(__file__).parent
BUILD_OUTPUT = REPO_ROOT / "bin" / "Release" / "net9.0" / "win-x64"
TOLK_DIR = REPO_ROOT / "tolk build"
GAME_DIR = Path("C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2")
MODS_DIR = GAME_DIR / "mods"
OUTPUT_ZIP = REPO_ROOT / "SayTheSpire2.zip"

# Files that go into mods/ in the zip
MODS_FILES = {
    MODS_DIR / "SayTheSpire2.dll": "mods/SayTheSpire2.dll",
    MODS_DIR / "SayTheSpire2.pck": "mods/SayTheSpire2.pck",
    BUILD_OUTPUT / "System.Speech.dll": "mods/System.Speech.dll",
    TOLK_DIR / "TolkDotNet.dll": "mods/TolkDotNet.dll",
}

# Files that go into the game root in the zip
ROOT_FILES = {
    TOLK_DIR / "x64" / "Tolk.dll": "Tolk.dll",
    TOLK_DIR / "x64" / "nvdaControllerClient64.dll": "nvdaControllerClient64.dll",
    TOLK_DIR / "x64" / "SAAPI64.dll": "SAAPI64.dll",
}


def main():
    # Build
    print("Building mod...")
    result = subprocess.run(
        ["dotnet", "build", "-c", "Release"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(result.stdout)
        print(result.stderr)
        print("Build failed!")
        sys.exit(1)
    print("Build succeeded.")

    # Collect files
    all_files = {**MODS_FILES, **ROOT_FILES}
    missing = [str(src) for src, _ in all_files.items() if not src.exists()]
    if missing:
        print("Missing files:")
        for f in missing:
            print(f"  {f}")
        sys.exit(1)

    # Create zip
    with zipfile.ZipFile(OUTPUT_ZIP, "w", zipfile.ZIP_DEFLATED) as zf:
        for src, arc_name in all_files.items():
            print(f"  {arc_name} <- {src}")
            zf.write(src, arc_name)

    print(f"\nCreated {OUTPUT_ZIP} ({OUTPUT_ZIP.stat().st_size / 1024:.1f} KB)")


if __name__ == "__main__":
    main()
