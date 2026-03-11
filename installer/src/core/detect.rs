use std::path::{Path, PathBuf};
use regex::Regex;

use super::paths::{steam_defaults, GAME_DIR_NAME};

const VALIDATION_MARKERS: &[&str] = &[
    "data_sts2_windows_x86_64",
    "data_sts2_linux_x86_64",
    "data_sts2_macos_arm64",
    "data_sts2_macos_x86_64",
    "Slay the Spire 2.exe",
    "Slay the Spire 2.x86_64",
    "Slay the Spire 2.app",
];

pub fn detect_game_path() -> Option<PathBuf> {
    for steam_dir in steam_defaults() {
        // Try libraryfolders.vdf
        let vdf_path = steam_dir.join("steamapps").join("libraryfolders.vdf");
        if vdf_path.exists() {
            if let Ok(content) = std::fs::read_to_string(&vdf_path) {
                for lib_path in parse_vdf_library_paths(&content) {
                    let game_path = lib_path.join("steamapps").join("common").join(GAME_DIR_NAME);
                    if game_path.exists() {
                        return Some(game_path);
                    }
                }
            }
        }

        // Try default location
        let default_path = steam_dir.join("steamapps").join("common").join(GAME_DIR_NAME);
        if default_path.exists() {
            return Some(default_path);
        }
    }
    None
}

pub fn parse_vdf_library_paths(content: &str) -> Vec<PathBuf> {
    let re = Regex::new(r#""path"\s+"([^"]+)""#).unwrap();
    re.captures_iter(content)
        .map(|cap| {
            let raw = cap[1].replace("\\\\", "\\");
            PathBuf::from(raw)
        })
        .collect()
}

pub fn validate_game_path(path: &Path) -> bool {
    VALIDATION_MARKERS.iter().any(|m| path.join(m).exists())
}

pub fn is_mod_installed(game_path: &Path) -> bool {
    game_path.join("mods").join("SayTheSpire2.dll").exists()
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    #[test]
    fn parse_vdf_empty_content() {
        let paths = parse_vdf_library_paths("");
        assert!(paths.is_empty());
    }

    #[test]
    fn parse_vdf_single_path() {
        let content = r#"
        "0"
        {
            "path"		"C:\\Program Files (x86)\\Steam"
            "label"		""
        }
        "#;
        let paths = parse_vdf_library_paths(content);
        assert_eq!(paths.len(), 1);
        assert_eq!(paths[0], PathBuf::from("C:\\Program Files (x86)\\Steam"));
    }

    #[test]
    fn parse_vdf_multiple_paths() {
        let content = r#"
        "0"
        {
            "path"		"C:\\Program Files (x86)\\Steam"
        }
        "1"
        {
            "path"		"D:\\SteamLibrary"
        }
        "2"
        {
            "path"		"E:\\Games\\Steam"
        }
        "#;
        let paths = parse_vdf_library_paths(content);
        assert_eq!(paths.len(), 3);
        assert_eq!(paths[0], PathBuf::from("C:\\Program Files (x86)\\Steam"));
        assert_eq!(paths[1], PathBuf::from("D:\\SteamLibrary"));
        assert_eq!(paths[2], PathBuf::from("E:\\Games\\Steam"));
    }

    #[test]
    fn parse_vdf_malformed_content() {
        let content = r#"this is not valid vdf content at all"#;
        let paths = parse_vdf_library_paths(content);
        assert!(paths.is_empty());
    }

    #[test]
    fn validate_game_path_with_exe() {
        let dir = tempfile::tempdir().unwrap();
        fs::write(dir.path().join("Slay the Spire 2.exe"), "").unwrap();
        assert!(validate_game_path(dir.path()));
    }

    #[test]
    fn validate_game_path_with_data_dir() {
        let dir = tempfile::tempdir().unwrap();
        fs::create_dir(dir.path().join("data_sts2_windows_x86_64")).unwrap();
        assert!(validate_game_path(dir.path()));
    }

    #[test]
    fn validate_game_path_with_linux_binary() {
        let dir = tempfile::tempdir().unwrap();
        fs::write(dir.path().join("Slay the Spire 2.x86_64"), "").unwrap();
        assert!(validate_game_path(dir.path()));
    }

    #[test]
    fn validate_game_path_empty_dir() {
        let dir = tempfile::tempdir().unwrap();
        assert!(!validate_game_path(dir.path()));
    }

    #[test]
    fn is_mod_installed_true() {
        let dir = tempfile::tempdir().unwrap();
        let mods_dir = dir.path().join("mods");
        fs::create_dir(&mods_dir).unwrap();
        fs::write(mods_dir.join("SayTheSpire2.dll"), "").unwrap();
        assert!(is_mod_installed(dir.path()));
    }

    #[test]
    fn is_mod_installed_false() {
        let dir = tempfile::tempdir().unwrap();
        assert!(!is_mod_installed(dir.path()));
    }
}
