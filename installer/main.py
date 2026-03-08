"""SayTheSpire2 Mod Installer"""

import json
import os
import re
import shutil
import threading
import zipfile
from io import BytesIO
from pathlib import Path

import requests
import wx

GITHUB_REPO = "bradjrenshaw/say-the-spire2"
GITHUB_API_URL = f"https://api.github.com/repos/{GITHUB_REPO}/releases/latest"
STEAM_DEFAULT = Path("C:/Program Files (x86)/Steam")
GAME_DIR_NAME = "Slay the Spire 2"
APPDATA_MOD_DIR = Path(os.environ.get("APPDATA", "")) / "SlayTheSpire2" / "mods" / "SayTheSpire2"
VERSION_FILE = APPDATA_MOD_DIR / "version"

# Files the installer manages (for uninstall)
MOD_FILES = [
    "mods/SayTheSpire2.dll",
    "mods/SayTheSpire2.pck",
    "mods/System.Speech.dll",
    "mods/TolkDotNet.dll",
]
ROOT_FILES = [
    "Tolk.dll",
    "nvdaControllerClient64.dll",
    "SAAPI64.dll",
]


def detect_game_path():
    """Try to find the game install directory automatically."""
    # 1. Try libraryfolders.vdf
    vdf_path = STEAM_DEFAULT / "steamapps" / "libraryfolders.vdf"
    if vdf_path.exists():
        game_path = _find_game_in_vdf(vdf_path)
        if game_path and game_path.exists():
            return game_path

    # 2. Try default Steam common directory
    default_path = STEAM_DEFAULT / "steamapps" / "common" / GAME_DIR_NAME
    if default_path.exists():
        return default_path

    return None


def _find_game_in_vdf(vdf_path):
    """Parse libraryfolders.vdf to find library folders, then check for the game."""
    try:
        text = vdf_path.read_text(encoding="utf-8")
        # Extract "path" values from the VDF
        paths = re.findall(r'"path"\s+"([^"]+)"', text)
        for lib_path in paths:
            lib_path = Path(lib_path.replace("\\\\", "\\"))
            game_path = lib_path / "steamapps" / "common" / GAME_DIR_NAME
            if game_path.exists():
                return game_path
    except Exception:
        pass
    return None


def validate_game_path(path):
    """Check that the path looks like a valid game install."""
    p = Path(path)
    return (p / "data_sts2_windows_x86_64").exists() or (p / "Slay the Spire 2.exe").exists()


def get_installed_version():
    """Read the installed version from appdata."""
    if VERSION_FILE.exists():
        return VERSION_FILE.read_text(encoding="utf-8").strip()
    return None


def save_installed_version(version):
    """Write the installed version to appdata."""
    APPDATA_MOD_DIR.mkdir(parents=True, exist_ok=True)
    VERSION_FILE.write_text(version, encoding="utf-8")


def is_mod_installed(game_path):
    """Check if mod files exist in the game directory."""
    p = Path(game_path)
    return (p / "mods" / "SayTheSpire2.dll").exists()


def enable_mods_in_settings():
    """Find settings.save and enable mods."""
    steam_dir = Path(os.environ.get("APPDATA", "")) / "SlayTheSpire2" / "steam"
    if not steam_dir.exists():
        return False

    # Find the most recently modified settings.save
    settings_files = []
    for user_dir in steam_dir.iterdir():
        if user_dir.is_dir():
            settings_file = user_dir / "settings.save"
            if settings_file.exists():
                settings_files.append(settings_file)

    if not settings_files:
        return False

    settings_file = max(settings_files, key=lambda f: f.stat().st_mtime)

    try:
        data = json.loads(settings_file.read_text(encoding="utf-8"))
        if "mod_settings" not in data:
            data["mod_settings"] = {}
        data["mod_settings"]["mods_enabled"] = True
        settings_file.write_text(json.dumps(data, indent=2), encoding="utf-8")
        return True
    except Exception:
        return False


def fetch_latest_release():
    """Get latest release info from GitHub API."""
    resp = requests.get(GITHUB_API_URL, timeout=15)
    resp.raise_for_status()
    data = resp.json()
    return {
        "version": data.get("tag_name", "unknown"),
        "notes": data.get("body", ""),
        "assets": data.get("assets", []),
    }


def find_zip_asset(assets):
    """Find the .zip asset from the release."""
    for asset in assets:
        if asset["name"].endswith(".zip"):
            return asset["browser_download_url"], asset["name"]
    return None, None


def download_and_extract(url, game_path, progress_callback=None):
    """Download the release zip and extract to game directory."""
    resp = requests.get(url, stream=True, timeout=60)
    resp.raise_for_status()

    total = int(resp.headers.get("content-length", 0))
    downloaded = 0
    buffer = BytesIO()

    for chunk in resp.iter_content(chunk_size=8192):
        buffer.write(chunk)
        downloaded += len(chunk)
        if progress_callback and total > 0:
            progress_callback(int(downloaded * 100 / total))

    buffer.seek(0)
    with zipfile.ZipFile(buffer) as zf:
        zf.extractall(game_path)


def uninstall_mod(game_path):
    """Remove mod files from the game directory."""
    p = Path(game_path)
    removed = []
    for f in MOD_FILES + ROOT_FILES:
        fp = p / f
        if fp.exists():
            fp.unlink()
            removed.append(f)
    return removed


class ChangelogDialog(wx.Dialog):
    """Dialog showing release notes with Update/Cancel buttons."""

    def __init__(self, parent, version, notes):
        super().__init__(parent, title=f"Update to {version}", size=(500, 400))

        sizer = wx.BoxSizer(wx.VERTICAL)

        label = wx.StaticText(self, label=f"Changes in {version}:")
        sizer.Add(label, 0, wx.ALL, 10)

        text = wx.TextCtrl(self, value=notes, style=wx.TE_MULTILINE | wx.TE_READONLY)
        sizer.Add(text, 1, wx.EXPAND | wx.LEFT | wx.RIGHT, 10)

        btn_sizer = wx.StdDialogButtonSizer()
        update_btn = wx.Button(self, wx.ID_OK, "Update")
        cancel_btn = wx.Button(self, wx.ID_CANCEL, "Cancel")
        btn_sizer.AddButton(update_btn)
        btn_sizer.AddButton(cancel_btn)
        btn_sizer.Realize()
        sizer.Add(btn_sizer, 0, wx.EXPAND | wx.ALL, 10)

        self.SetSizer(sizer)
        update_btn.SetFocus()


class InstallerFrame(wx.Frame):
    """Main installer window."""

    def __init__(self):
        super().__init__(None, title="SayTheSpire2 Installer", size=(500, 350))

        self._game_path = None
        self._release_info = None

        panel = wx.Panel(self)
        sizer = wx.BoxSizer(wx.VERTICAL)

        # Status
        self._status = wx.StaticText(panel, label="Detecting game directory...")
        sizer.Add(self._status, 0, wx.ALL, 10)

        # Game path
        path_sizer = wx.BoxSizer(wx.HORIZONTAL)
        path_label = wx.StaticText(panel, label="Game directory:")
        path_sizer.Add(path_label, 0, wx.ALIGN_CENTER_VERTICAL | wx.RIGHT, 5)
        self._path_text = wx.TextCtrl(panel, style=wx.TE_PROCESS_ENTER)
        path_sizer.Add(self._path_text, 1, wx.EXPAND | wx.RIGHT, 5)
        self._browse_btn = wx.Button(panel, label="Browse...")
        path_sizer.Add(self._browse_btn, 0)
        sizer.Add(path_sizer, 0, wx.EXPAND | wx.LEFT | wx.RIGHT, 10)

        # Progress bar
        self._progress = wx.Gauge(panel, range=100)
        self._progress.Hide()
        sizer.Add(self._progress, 0, wx.EXPAND | wx.ALL, 10)

        # Log area
        self._log = wx.TextCtrl(panel, style=wx.TE_MULTILINE | wx.TE_READONLY)
        sizer.Add(self._log, 1, wx.EXPAND | wx.LEFT | wx.RIGHT | wx.BOTTOM, 10)

        # Buttons
        btn_sizer = wx.BoxSizer(wx.HORIZONTAL)
        self._install_btn = wx.Button(panel, label="Install")
        self._install_btn.Disable()
        btn_sizer.Add(self._install_btn, 0, wx.RIGHT, 5)
        self._install_file_btn = wx.Button(panel, label="Install from file...")
        self._install_file_btn.Disable()
        btn_sizer.Add(self._install_file_btn, 0, wx.RIGHT, 5)
        self._uninstall_btn = wx.Button(panel, label="Uninstall")
        self._uninstall_btn.Disable()
        btn_sizer.Add(self._uninstall_btn, 0)
        sizer.Add(btn_sizer, 0, wx.ALIGN_RIGHT | wx.RIGHT | wx.BOTTOM, 10)

        panel.SetSizer(sizer)

        # Events
        self._browse_btn.Bind(wx.EVT_BUTTON, self._on_browse)
        self._path_text.Bind(wx.EVT_TEXT_ENTER, self._on_path_enter)
        self._path_text.Bind(wx.EVT_KILL_FOCUS, self._on_path_focus_lost)
        self._install_btn.Bind(wx.EVT_BUTTON, self._on_install)
        self._install_file_btn.Bind(wx.EVT_BUTTON, self._on_install_from_file)
        self._uninstall_btn.Bind(wx.EVT_BUTTON, self._on_uninstall)

        # Start detection
        self.Show()
        wx.CallAfter(self._initialize)

    def _log_message(self, msg):
        self._log.AppendText(msg + "\n")

    def _initialize(self):
        # Detect game path
        detected = detect_game_path()
        if detected:
            self._set_game_path(str(detected))
        else:
            self._status.SetLabel("Game directory not found. Please browse to select it.")
            self._log_message("Could not auto-detect game directory.")

        # Fetch release info
        self._check_release()

    def _set_game_path(self, path):
        if not validate_game_path(path):
            self._log_message(f"Invalid game directory: {path}")
            self._status.SetLabel("Invalid game directory. Please browse to select it.")
            return False

        self._game_path = path
        self._path_text.SetValue(path)
        self._install_file_btn.Enable()
        self._log_message(f"Game directory: {path}")

        if is_mod_installed(path):
            version = get_installed_version()
            if version:
                self._log_message(f"Installed version: {version}")
            else:
                self._log_message("Mod is installed (unknown version).")
            self._uninstall_btn.Enable()
        else:
            self._log_message("Mod is not installed.")

        self._update_ui_state()
        return True

    def _check_release(self):
        self._log_message("Checking for latest release...")
        try:
            self._release_info = fetch_latest_release()
            self._log_message(f"Latest version: {self._release_info['version']}")
            self._update_ui_state()
        except Exception as e:
            self._log_message(f"Failed to check for updates: {e}")
            self._status.SetLabel("Could not connect to GitHub. Install/update unavailable.")

    def _update_ui_state(self):
        if not self._game_path or not self._release_info:
            return

        installed = get_installed_version()
        latest = self._release_info["version"]

        if not is_mod_installed(self._game_path):
            self._install_btn.SetLabel("Install")
            self._install_btn.Enable()
            self._status.SetLabel(f"Ready to install version {latest}.")
        elif installed == latest:
            self._install_btn.SetLabel("Install")
            self._install_btn.Disable()
            self._status.SetLabel(f"SayTheSpire2 is up to date (version {latest}).")
        else:
            self._install_btn.SetLabel("Update")
            self._install_btn.Enable()
            self._status.SetLabel(f"Update available: {installed or 'unknown'} \u2192 {latest}")

    def _on_browse(self, event):
        dlg = wx.DirDialog(self, "Select Slay the Spire 2 game directory",
                           style=wx.DD_DEFAULT_STYLE | wx.DD_DIR_MUST_EXIST)
        if dlg.ShowModal() == wx.ID_OK:
            self._set_game_path(dlg.GetPath())
        dlg.Destroy()

    def _on_path_enter(self, event):
        self._apply_manual_path()

    def _on_path_focus_lost(self, event):
        self._apply_manual_path()
        event.Skip()

    def _apply_manual_path(self):
        path = self._path_text.GetValue().strip()
        if not path or path == (self._game_path or ""):
            return
        self._set_game_path(path)

    def _on_install(self, event):
        if not self._game_path or not self._release_info:
            return

        is_update = self._install_btn.GetLabel() == "Update"

        # Show changelog for updates
        if is_update:
            dlg = ChangelogDialog(self, self._release_info["version"],
                                  self._release_info.get("notes", "No release notes available."))
            if dlg.ShowModal() != wx.ID_OK:
                dlg.Destroy()
                return
            dlg.Destroy()

        # Find zip asset
        url, name = find_zip_asset(self._release_info["assets"])
        if not url:
            self._log_message("Error: No .zip asset found in the latest release.")
            return

        # Disable buttons during install
        self._install_btn.Disable()
        self._install_file_btn.Disable()
        self._uninstall_btn.Disable()
        self._browse_btn.Disable()
        self._progress.SetValue(0)
        self._progress.Show()
        self.Layout()

        self._log_message(f"Downloading {name}...")

        # Run download in background thread
        thread = threading.Thread(target=self._do_install, args=(url,), daemon=True)
        thread.start()

    def _on_install_from_file(self, event):
        if not self._game_path:
            return

        dlg = wx.FileDialog(self, "Select mod zip file",
                            wildcard="Zip files (*.zip)|*.zip",
                            style=wx.FD_OPEN | wx.FD_FILE_MUST_EXIST)
        if dlg.ShowModal() != wx.ID_OK:
            dlg.Destroy()
            return

        zip_path = dlg.GetPath()
        dlg.Destroy()

        try:
            with zipfile.ZipFile(zip_path) as zf:
                zf.extractall(self._game_path)
        except Exception as e:
            self._log_message(f"Failed to extract: {e}")
            wx.MessageBox(f"Failed to extract zip:\n{e}", "Error", wx.OK | wx.ICON_ERROR)
            return

        if enable_mods_in_settings():
            self._log_message("Mods enabled in game settings.")
        else:
            self._log_message("Note: Could not find settings.save to enable mods. "
                              "You may need to run the game once first, then re-run the installer.")

        self._log_message(f"Installed from {os.path.basename(zip_path)}.")
        self._uninstall_btn.Enable()
        self._update_ui_state()

        wx.MessageBox("SayTheSpire2 installed successfully from file!",
                      "Installation Complete", wx.OK | wx.ICON_INFORMATION)

    def _do_install(self, url):
        try:
            def progress(pct):
                wx.CallAfter(self._progress.SetValue, pct)

            download_and_extract(url, self._game_path, progress_callback=progress)
            wx.CallAfter(self._on_install_complete)
        except Exception as e:
            wx.CallAfter(self._on_install_error, str(e))

    def _on_install_complete(self):
        version = self._release_info["version"]
        save_installed_version(version)

        # Enable mods in settings
        if enable_mods_in_settings():
            self._log_message("Mods enabled in game settings.")
        else:
            self._log_message("Note: Could not find settings.save to enable mods. "
                              "You may need to run the game once first, then re-run the installer.")

        self._log_message(f"Successfully installed version {version}.")
        self._progress.Hide()
        self._browse_btn.Enable()
        self._install_file_btn.Enable()
        self._uninstall_btn.Enable()
        self._update_ui_state()
        self.Layout()

        wx.MessageBox(f"SayTheSpire2 version {version} installed successfully!",
                      "Installation Complete", wx.OK | wx.ICON_INFORMATION)

    def _on_install_error(self, error):
        self._log_message(f"Installation failed: {error}")
        self._progress.Hide()
        self._install_btn.Enable()
        self._install_file_btn.Enable()
        self._browse_btn.Enable()
        if is_mod_installed(self._game_path):
            self._uninstall_btn.Enable()
        self.Layout()

        wx.MessageBox(f"Installation failed:\n{error}", "Error", wx.OK | wx.ICON_ERROR)

    def _on_uninstall(self, event):
        dlg = wx.MessageDialog(self,
                               "Remove SayTheSpire2 mod files?\n\n"
                               "This will remove the mod from the game directory.",
                               "Confirm Uninstall",
                               wx.YES_NO | wx.NO_DEFAULT | wx.ICON_QUESTION)
        if dlg.ShowModal() != wx.ID_YES:
            dlg.Destroy()
            return
        dlg.Destroy()

        removed = uninstall_mod(self._game_path)
        if removed:
            self._log_message(f"Removed: {', '.join(removed)}")
        else:
            self._log_message("No mod files found to remove.")

        # Ask about settings
        dlg = wx.MessageDialog(self,
                               "Also remove mod settings and saved preferences?",
                               "Remove Settings",
                               wx.YES_NO | wx.NO_DEFAULT | wx.ICON_QUESTION)
        if dlg.ShowModal() == wx.ID_YES:
            if APPDATA_MOD_DIR.exists():
                shutil.rmtree(APPDATA_MOD_DIR)
                self._log_message("Removed mod settings.")
        dlg.Destroy()

        self._log_message("Uninstall complete.")
        self._uninstall_btn.Disable()
        self._update_ui_state()

        wx.MessageBox("SayTheSpire2 has been uninstalled.", "Uninstall Complete",
                      wx.OK | wx.ICON_INFORMATION)


def main():
    app = wx.App()
    InstallerFrame()
    app.MainLoop()


if __name__ == "__main__":
    main()
