"""
Build script for the STS2 Accessibility Mod.
1. Runs dotnet build
2. Creates a PCK file containing mod_manifest.json and localization files
3. Copies the DLL and PCK to the game's mods/ directory
"""
import struct
import json
import os
import subprocess
import sys
import shutil

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
MOD_NAME = "sts2-accessibility-mod"
GAME_DIR = r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
MODS_DIR = os.path.join(GAME_DIR, "mods")
BUILD_DIR = os.path.join(SCRIPT_DIR, "bin", "Release", "net9.0", "win-x64")


def build_dll():
    print("Building DLL...")
    result = subprocess.run(
        ["dotnet", "build", "-c", "Release"],
        cwd=SCRIPT_DIR,
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.returncode != 0:
        print("BUILD FAILED:")
        print(result.stderr)
        sys.exit(1)
    print("Build succeeded.")


def collect_pck_files():
    """Collect all files to include in the PCK."""
    files = []

    # mod_manifest.json
    manifest_path = os.path.join(SCRIPT_DIR, "mod_manifest.json")
    with open(manifest_path, "rb") as f:
        files.append(("res://mod_manifest.json", f.read()))

    # Localization files
    loc_dir = os.path.join(SCRIPT_DIR, "localization")
    for root, dirs, filenames in os.walk(loc_dir):
        for filename in filenames:
            if not filename.endswith(".json"):
                continue
            full_path = os.path.join(root, filename)
            rel_path = os.path.relpath(full_path, SCRIPT_DIR).replace("\\", "/")
            res_path = f"res://{rel_path}"
            with open(full_path, "rb") as f:
                files.append((res_path, f.read()))

    return files


def create_pck(output_path: str):
    """Create a Godot PCK file containing mod resources."""
    files = collect_pck_files()

    # Pre-calculate file entry sizes for each file
    entries = []
    for res_path, data in files:
        path_bytes = res_path.encode("utf-8")
        path_padded_len = (len(path_bytes) + 4) & ~3
        path_bytes_padded = path_bytes.ljust(path_padded_len, b"\x00")
        entries.append((path_bytes_padded, path_padded_len, data))

    # Header: 4 (magic) + 4 (ver) + 4*3 (engine) + 4 (flags) + 8 (file_base) + 64 (reserved) = 96
    header_size = 96
    file_count_size = 4

    # Calculate total directory size
    directory_size = file_count_size
    for _, path_padded_len, _ in entries:
        # 4 (path_len) + path_padded_len + 8 (offset) + 8 (size) + 16 (md5) + 4 (flags)
        directory_size += 4 + path_padded_len + 8 + 8 + 16 + 4

    data_start = header_size + directory_size
    data_start_aligned = (data_start + 63) & ~63

    # Calculate file data offsets
    file_offsets = []
    current_offset = data_start_aligned
    for _, _, data in entries:
        file_offsets.append(current_offset)
        current_offset += len(data)
        # Align each file to 64 bytes
        current_offset = (current_offset + 63) & ~63

    with open(output_path, "wb") as f:
        # Header
        f.write(b"GDPC")
        f.write(struct.pack("<I", 2))   # pack format version
        f.write(struct.pack("<I", 4))   # engine major
        f.write(struct.pack("<I", 5))   # engine minor
        f.write(struct.pack("<I", 1))   # engine patch
        f.write(struct.pack("<I", 0))   # flags
        f.write(struct.pack("<Q", 0))   # file_base
        f.write(b"\x00" * 64)          # reserved

        # File count
        f.write(struct.pack("<I", len(entries)))

        # File entries
        for i, (path_bytes_padded, path_padded_len, data) in enumerate(entries):
            f.write(struct.pack("<I", path_padded_len))
            f.write(path_bytes_padded)
            f.write(struct.pack("<Q", file_offsets[i]))
            f.write(struct.pack("<Q", len(data)))
            f.write(b"\x00" * 16)  # md5
            f.write(struct.pack("<I", 0))  # flags

        # Pad to data start
        current = f.tell()
        if current < data_start_aligned:
            f.write(b"\x00" * (data_start_aligned - current))

        # File data
        for i, (_, _, data) in enumerate(entries):
            f.write(data)
            # Pad to 64-byte alignment
            current = f.tell()
            aligned = (current + 63) & ~63
            if current < aligned:
                f.write(b"\x00" * (aligned - current))

    file_list = ", ".join(res_path for res_path, _ in collect_pck_files())
    print(f"Created PCK with {len(entries)} files: {file_list}")


def deploy():
    os.makedirs(MODS_DIR, exist_ok=True)

    # Copy DLL
    dll_src = os.path.join(BUILD_DIR, f"{MOD_NAME}.dll")
    dll_dst = os.path.join(MODS_DIR, f"{MOD_NAME}.dll")
    shutil.copy2(dll_src, dll_dst)
    print(f"Copied DLL to {dll_dst}")

    # Copy System.Speech.dll to mods dir (our assembly resolver loads from there)
    speech_dll = os.path.join(BUILD_DIR, "System.Speech.dll")
    if os.path.exists(speech_dll):
        shutil.copy2(speech_dll, os.path.join(MODS_DIR, "System.Speech.dll"))
        print("Copied System.Speech.dll to mods dir")

    # Copy Tolk DLLs
    # TolkDotNet.dll (managed) goes to mods/ - our assembly resolver handles it
    # Native DLLs (Tolk.dll, nvdaControllerClient64.dll, SAAPI64.dll) go next to
    # the game exe - Windows DLL search path needs them there
    tolk_build_dir = os.path.join(SCRIPT_DIR, "tolk build")
    tolk_libs_dir = os.path.join(SCRIPT_DIR, "..", "tolk", "libs", "x64")

    # Managed wrapper -> mods/
    tolk_dotnet_src = os.path.join(tolk_build_dir, "TolkDotNet.dll")
    if os.path.exists(tolk_dotnet_src):
        shutil.copy2(tolk_dotnet_src, os.path.join(MODS_DIR, "TolkDotNet.dll"))
        print("Copied TolkDotNet.dll to mods dir")

    # Native DLLs -> game root (next to exe)
    native_dlls = [
        (os.path.join(tolk_build_dir, "x64", "Tolk.dll"), "Tolk.dll"),
        (os.path.join(tolk_libs_dir, "nvdaControllerClient64.dll"), "nvdaControllerClient64.dll"),
        (os.path.join(tolk_libs_dir, "SAAPI64.dll"), "SAAPI64.dll"),
    ]
    for src, dst_name in native_dlls:
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(GAME_DIR, dst_name))
            print(f"Copied {dst_name} to game dir")
        else:
            print(f"WARNING: {src} not found")

    # Create and copy PCK (includes mod_manifest.json and localization files)
    pck_path = os.path.join(MODS_DIR, f"{MOD_NAME}.pck")
    create_pck(pck_path)

    print(f"\nMod deployed to {MODS_DIR}")
    print("Start the game to test!")


if __name__ == "__main__":
    build_dll()
    deploy()
