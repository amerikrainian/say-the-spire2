use std::fs;
use std::path::Path;

pub fn has_jaws_files(game_path: &Path) -> bool {
    let jaws_dir = game_path.join("jaws");
    if !jaws_dir.exists() {
        return false;
    }
    fs::read_dir(&jaws_dir)
        .map(|entries| entries.filter_map(|e| e.ok()).any(|e| e.path().is_file()))
        .unwrap_or(false)
}

pub fn get_jaws_files(game_path: &Path) -> Vec<String> {
    let jaws_dir = game_path.join("jaws");
    if !jaws_dir.exists() {
        return Vec::new();
    }
    fs::read_dir(&jaws_dir)
        .map(|entries| {
            entries
                .filter_map(|e| e.ok())
                .filter(|e| e.path().is_file())
                .filter_map(|e| e.file_name().into_string().ok())
                .collect()
        })
        .unwrap_or_default()
}

/// Copy JAWS config files from game_path/jaws/ to dest_dir.
/// Returns (copied_files, errors).
pub fn install_jaws_config(
    game_path: &Path,
    dest_dir: &Path,
) -> (Vec<String>, Vec<String>) {
    let jaws_dir = game_path.join("jaws");
    let mut copied = Vec::new();
    let mut errors = Vec::new();

    let entries = match fs::read_dir(&jaws_dir) {
        Ok(e) => e,
        Err(e) => {
            errors.push(format!("Failed to read jaws directory: {}", e));
            return (copied, errors);
        }
    };

    for entry in entries.filter_map(|e| e.ok()) {
        let path = entry.path();
        if !path.is_file() {
            continue;
        }
        let file_name = match path.file_name().and_then(|n| n.to_str()) {
            Some(n) => n.to_string(),
            None => continue,
        };

        let dest = dest_dir.join(&file_name);
        match fs::copy(&path, &dest) {
            Ok(_) => copied.push(file_name),
            Err(e) => errors.push(format!("{}: {}", file_name, e)),
        }
    }

    (copied, errors)
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    #[test]
    fn has_jaws_files_with_files() {
        let dir = tempfile::tempdir().unwrap();
        let jaws = dir.path().join("jaws");
        fs::create_dir(&jaws).unwrap();
        fs::write(jaws.join("test.JCF"), "content").unwrap();
        assert!(has_jaws_files(dir.path()));
    }

    #[test]
    fn has_jaws_files_empty_dir() {
        let dir = tempfile::tempdir().unwrap();
        let jaws = dir.path().join("jaws");
        fs::create_dir(&jaws).unwrap();
        assert!(!has_jaws_files(dir.path()));
    }

    #[test]
    fn has_jaws_files_no_dir() {
        let dir = tempfile::tempdir().unwrap();
        assert!(!has_jaws_files(dir.path()));
    }

    #[test]
    fn get_jaws_files_lists_files() {
        let dir = tempfile::tempdir().unwrap();
        let jaws = dir.path().join("jaws");
        fs::create_dir(&jaws).unwrap();
        fs::write(jaws.join("a.JCF"), "").unwrap();
        fs::write(jaws.join("b.JDF"), "").unwrap();

        let files = get_jaws_files(dir.path());
        assert_eq!(files.len(), 2);
        assert!(files.contains(&"a.JCF".to_string()));
        assert!(files.contains(&"b.JDF".to_string()));
    }

    #[test]
    fn get_jaws_files_no_dir() {
        let dir = tempfile::tempdir().unwrap();
        let files = get_jaws_files(dir.path());
        assert!(files.is_empty());
    }

    #[test]
    fn install_jaws_copies_files() {
        let src = tempfile::tempdir().unwrap();
        let dest = tempfile::tempdir().unwrap();

        let jaws = src.path().join("jaws");
        fs::create_dir(&jaws).unwrap();
        fs::write(jaws.join("test.JCF"), "jcf content").unwrap();
        fs::write(jaws.join("test.jkm"), "jkm content").unwrap();

        let (copied, errors) = install_jaws_config(src.path(), dest.path());

        assert_eq!(copied.len(), 2);
        assert!(errors.is_empty());
        assert_eq!(
            fs::read_to_string(dest.path().join("test.JCF")).unwrap(),
            "jcf content"
        );
        assert_eq!(
            fs::read_to_string(dest.path().join("test.jkm")).unwrap(),
            "jkm content"
        );
    }

    #[test]
    fn install_jaws_no_source_dir() {
        let src = tempfile::tempdir().unwrap();
        let dest = tempfile::tempdir().unwrap();

        let (copied, errors) = install_jaws_config(src.path(), dest.path());
        assert!(copied.is_empty());
        assert!(!errors.is_empty());
    }
}
