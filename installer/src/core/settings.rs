use std::fs;
use std::path::Path;

use super::paths::user_data_dir;

/// Strip UTF-8 BOM from raw bytes if present, return the string content.
pub fn strip_bom(raw: &[u8]) -> &[u8] {
    if raw.len() >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF {
        &raw[3..]
    } else {
        raw
    }
}

/// Enable mods in all settings.save files found under the user data steam directory.
pub fn enable_mods_in_settings() -> Result<String, String> {
    let steam_dir = user_data_dir().join("steam");
    if !steam_dir.exists() {
        return Err(format!(
            "Steam directory not found at {}. Please run the game once first.",
            steam_dir.display()
        ));
    }

    let mut settings_files = Vec::new();
    let entries = fs::read_dir(&steam_dir)
        .map_err(|e| format!("Failed to read steam dir: {}", e))?;

    for entry in entries {
        let entry = entry.map_err(|e| format!("Failed to read entry: {}", e))?;
        if entry.path().is_dir() {
            let sf = entry.path().join("settings.save");
            if sf.exists() {
                settings_files.push(sf);
            }
        }
    }

    if settings_files.is_empty() {
        return Err(format!(
            "No settings.save found in {}. Please run the game once first.",
            steam_dir.display()
        ));
    }

    let mut errors = Vec::new();
    for sf in &settings_files {
        if let Err(e) = enable_mods_in_file(sf) {
            errors.push(format!("{}: {}", sf.display(), e));
        }
    }

    if !errors.is_empty() {
        return Err(format!("Failed to modify settings:\n{}", errors.join("\n")));
    }

    Ok(format!("Mods enabled in {} settings file(s).", settings_files.len()))
}

fn enable_mods_in_file(path: &Path) -> Result<(), String> {
    let raw = fs::read(path).map_err(|e| format!("Failed to read: {}", e))?;
    let text = std::str::from_utf8(strip_bom(&raw))
        .map_err(|e| format!("Invalid UTF-8: {}", e))?;

    let mut data: serde_json::Value =
        serde_json::from_str(text).map_err(|e| format!("Invalid JSON: {}", e))?;

    // Ensure mod_settings exists and is an object
    if !data.get("mod_settings").map_or(false, |v| v.is_object()) {
        data["mod_settings"] = serde_json::json!({});
    }
    data["mod_settings"]["mods_enabled"] = serde_json::json!(true);

    let output = serde_json::to_string_pretty(&data)
        .map_err(|e| format!("Failed to serialize: {}", e))?;

    fs::write(path, output).map_err(|e| format!("Failed to write: {}", e))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn strip_bom_with_bom() {
        let data = b"\xEF\xBB\xBF{\"key\": \"value\"}";
        let result = strip_bom(data);
        assert_eq!(result, b"{\"key\": \"value\"}");
    }

    #[test]
    fn strip_bom_without_bom() {
        let data = b"{\"key\": \"value\"}";
        let result = strip_bom(data);
        assert_eq!(result, b"{\"key\": \"value\"}");
    }

    #[test]
    fn strip_bom_empty() {
        let data = b"";
        let result = strip_bom(data);
        assert_eq!(result, b"");
    }

    #[test]
    fn strip_bom_short() {
        let data = b"\xEF\xBB";
        let result = strip_bom(data);
        assert_eq!(result, b"\xEF\xBB");
    }

    #[test]
    fn enable_mods_adds_to_empty_settings() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        fs::write(&sf, "{}").unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }

    #[test]
    fn enable_mods_preserves_existing_settings() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        fs::write(
            &sf,
            r#"{"audio": {"volume": 80}, "mod_settings": {"some_mod": true}}"#,
        )
        .unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["audio"]["volume"], 80);
        assert_eq!(result["mod_settings"]["some_mod"], true);
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }

    #[test]
    fn enable_mods_handles_bom() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        let mut data = vec![0xEF, 0xBB, 0xBF];
        data.extend_from_slice(b"{}");
        fs::write(&sf, data).unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }

    #[test]
    fn enable_mods_already_enabled() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        fs::write(&sf, r#"{"mod_settings": {"mods_enabled": true}}"#).unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }

    #[test]
    fn enable_mods_overwrites_disabled() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        fs::write(&sf, r#"{"mod_settings": {"mods_enabled": false}}"#).unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }

    #[test]
    fn enable_mods_replaces_non_object_mod_settings() {
        let dir = tempfile::tempdir().unwrap();
        let sf = dir.path().join("settings.save");
        fs::write(&sf, r#"{"mod_settings": "corrupted"}"#).unwrap();

        enable_mods_in_file(&sf).unwrap();

        let result: serde_json::Value =
            serde_json::from_str(&fs::read_to_string(&sf).unwrap()).unwrap();
        assert_eq!(result["mod_settings"]["mods_enabled"], true);
    }
}
