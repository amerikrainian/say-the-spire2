# Installation

There are two approaches to installing Say the Spire 2. You can either use the installer program (recommended) or manually install.

## Disabling Steam Input

If you want to play with a controller, you must disable Steam Input for Slay the Spire 2. This is because the game has a particularly unwieldy Steam Input configuration that prevents many controller inputs from working properly, for example it treats both sticks as being the left stick among other things. You can disable Steam Input for Slay the Spire 2 without changing it for other games. To disable it, do the following:

* **In Big Picture Mode:** Navigate to Slay the Spire 2 in your library. Move right to Manage, select it, then move down to Properties. Under the Controller tab, find the combo box labeled "Override for Slay the Spire 2". Select Disable Steam Input.
* **In Regular View:** In your game library, right-click Slay the Spire 2 (or click the manage/gear button). Click Properties, go to the Controller tab, and set the Steam Input override to Disable.

## Using the Installer (Recommended)

The recommended way to install Say the Spire 2 is using the provided installer program. This downloads the latest release, extracts all required files to the right directory, and modifies game settings to enable mods accessibly. You will not need to download an installer to update your mod to a newer version and can just simply use the same program (unless the installer itself is updated.) You can download it from the [latest release page](https://github.com/bradjrenshaw/say-the-spire2/releases/latest).

### First-Time Setup

1. Important: **Launch Slay the Spire 2 at least once** before running the installer. The game needs to generate its settings file.
2. Run the installer. It will auto-detect the game directory. If it doesn't, you can browse to find it with the browse button next to the input field.
3. Click **Install** and select the version you want.
4. The installer will prompt you with two options:
   - **Screen reader support**: Enable this if you are blind or visually impaired.
   - **Disable Godot UIA**: Recommended Yes for screen reader (particularly NVDA) users. Select No if you are sighted or use other accessibility tools that rely on UIA.
5. The installer will enable mods in your game settings automatically. If it can't find the settings file, it will prompt you to launch the game first and retry.
6. Launch the game normally. If done correctly, you should hear the mod speak Say the Spire and the mod version.

Note that you can update to newer versions of the mod with the same installer; simply click the update button in the installer UI.

### JAWS Configuration

If you use JAWS, the installer can copy JAWS configuration files to your JAWS settings directory. This is highly recommended. The jaws configuration files allow keyboard input to pass through to the program that jaws would otherwise prevent, such as the arrow keys. They also disable some annoying repeating announcements due to some UIA glitches (frequent reporting of an invisible UI control that handles the game's crash reporting.)

Click **Install JAWS config** after installing the mod and the mod will prompt you to select your Jaws settings directory. This will be something like C:\Users\user\AppData\Roaming\Freedom Scientific\JAWS\year\Settings\locale.


## Manual Installation

If you prefer not to use the installer or it doesn't work, you can set things up manually.

1. Important: **Launch Slay the Spire 2 at least once** before running the installer. The game needs to generate its settings file.
2. Download the latest zip release from https://github.com/bradjrenshaw/say-the-spire2/releases/latest
3. Extract the zip to your game's root directory. On Windows, the default is `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`. You should see screen reader files in the root directory and a `mods` subfolder.
4. Next, modify the game's settings file. On Windows, this is located in `%APPDATA%/SlayTheSpire2`.
5. Open the `steam` subfolder, then open the subfolder with a long number (your Steam account ID).
6. Open the `settings.save` file in a text editor (Notepad works well; avoid Word or similar). Search for `mod_settings` (Ctrl+F is helpful as it's a large JSON file). You should find `"mod_settings": null`. Replace `null` with `{"mods_enabled": true}`, so the line reads `"mod_settings": {"mods_enabled": true}`. Take care not to alter any surrounding formatting. Save and close the file.
7. Launch the game normally. If done correctly, you should hear the mod speak Say the Spire and the mod version.

### Jaws

If you use jaws, configuration files are provided that significantly improve the experience. The jaws configuration files allow keyboard input to pass through to the program that jaws would otherwise prevent, such as the arrow keys. They also disable some annoying repeating announcements due to some UIA glitches (frequent reporting of an invisible UI control that handles the game's crash reporting.)

The jaws scripts are located in the jaws subfolder of the release zip. Open this folder, copy them all to clipboard, and then paste them in your Jaws settings directory. This will be something like C:/Users/user/AppData/Roaming/Freedom Scientific/JAWS/year/Settings/locale.