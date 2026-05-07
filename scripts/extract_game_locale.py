"""
Extract STS2's official localization JSONs from the game's .pck.

Used to cross-check mod terminology against the game's translations
(card / relic / block / etc. should match what sighted players see).
Targets the v3 .pck format with PACK_REL_FILEBASE flag — directory
offset is stored at byte 32 of the header, file offsets are relative
to file_base (byte 24's 8-byte value).

Usage:
    python scripts/extract_game_locale.py [--out <dir>] [--all]

By default extracts only terminology-relevant files. Pass --all to
dump every localization/* path in the pck (a lot — be patient).

Output:
    <out>/<lang>/<file>.json   one per language (eng, fra, deu, ...)
"""
import argparse
import os
import struct
import sys


# Files most useful for cross-checking accessibility-mod terminology.
DEFAULT_TARGETS = {
    "card_keywords.json",
    "card_library.json",
    "card_reward_ui.json",
    "characters.json",
    "combat_messages.json",
    "epochs.json",
    "game_over_screen.json",
    "gameplay_ui.json",
    "intents.json",
    "main_menu_ui.json",
    "bestiary.json",
    "settings_ui.json",
    "rest_site_ui.json",
    "screen_titles.json",
    "shops.json",
}


def find_pck() -> str | None:
    """Locate the game's pck. Falls back to env var or known Steam path."""
    candidates = [
        os.environ.get("STS2_PCK"),
        r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck",
        r"C:\Program Files\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck",
    ]
    for p in candidates:
        if p and os.path.exists(p):
            return p
    return None


def parse_directory(pck_path: str) -> tuple[int, list[tuple[str, int, int]]]:
    """Return (file_base, [(path, offset, size), ...])."""
    with open(pck_path, "rb") as f:
        header = f.read(40)
        magic = header[:4]
        if magic != b"GDPC":
            raise SystemExit(f"Not a Godot .pck: {magic!r}")
        version = struct.unpack_from("<I", header, 4)[0]
        flags = struct.unpack_from("<I", header, 20)[0]
        file_base = struct.unpack_from("<Q", header, 24)[0]
        # Byte 32 (after the 8-byte file_base) holds the directory offset for
        # this game's pck v3 layout. (Standard Godot v3 puts file_count at
        # file_base + N; this game seems to use a footer-style directory.)
        dir_offset = struct.unpack_from("<Q", header, 32)[0]

        rel = bool(flags & 2)
        f.seek(dir_offset)
        file_count = struct.unpack("<I", f.read(4))[0]
        if file_count == 0 or file_count > 1_000_000:
            raise SystemExit(f"Implausible file_count {file_count} at offset {dir_offset}")

        entries = []
        for _ in range(file_count):
            path_len = struct.unpack("<I", f.read(4))[0]
            path_bytes = f.read(path_len).rstrip(b"\x00")
            offset = struct.unpack("<Q", f.read(8))[0]
            size = struct.unpack("<Q", f.read(8))[0]
            f.read(16 + 4)  # md5 + flags
            actual_offset = (file_base + offset) if rel else offset
            entries.append((path_bytes.decode("utf-8", errors="replace"), actual_offset, size))

    return file_base, entries


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default="game_locale",
                        help="Output directory (default: ./game_locale)")
    parser.add_argument("--all", action="store_true",
                        help="Extract every localization/* path, not just terminology files")
    parser.add_argument("--pck", default=None,
                        help="Path to .pck. Defaults to Steam install or $STS2_PCK")
    args = parser.parse_args()

    pck = args.pck or find_pck()
    if not pck:
        print("Could not find SlayTheSpire2.pck. Pass --pck or set STS2_PCK.", file=sys.stderr)
        return 2

    print(f"Reading {pck}")
    _, entries = parse_directory(pck)

    matches = []
    for path, offset, size in entries:
        if not path.startswith("localization/"):
            continue
        parts = path.split("/")
        if len(parts) != 3:
            continue
        if not args.all and parts[2] not in DEFAULT_TARGETS:
            continue
        matches.append((path, offset, size))

    print(f"Extracting {len(matches)} files to {args.out}/")
    with open(pck, "rb") as f:
        for path, offset, size in matches:
            out_path = os.path.join(args.out, path)
            os.makedirs(os.path.dirname(out_path), exist_ok=True)
            f.seek(offset)
            data = f.read(size)
            with open(out_path, "wb") as out:
                out.write(data)

    langs = sorted(set(p.split("/")[1] for p, _, _ in matches))
    print(f"Languages: {', '.join(langs)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
