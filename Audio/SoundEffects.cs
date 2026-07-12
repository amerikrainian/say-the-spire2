using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Audio;

/// <summary>
/// Plays the mod's own sound effects through a Godot AudioStreamPlayer (the
/// game's SFX go through FMOD, which we can't feed raw wavs). Streams are
/// packed as raw wav bytes in the mod PCK and decoded via
/// AudioStreamWav.LoadFromBuffer.
/// </summary>
public static class SoundEffects
{
    private const string WrapSoundPath = "res://SayTheSpire2/audio/wrap.wav";
    /// <summary>Centers closer than this along the nav axis don't count as movement.</summary>
    private const float WrapEpsilon = 4f;

    /// <summary>Set by ModEntry when settings register.</summary>
    public static BoolSetting? WrapSoundSetting { get; set; }

    /// <summary>Master volume for all mod sounds, 0–100. Set by ModEntry.</summary>
    public static IntSetting? VolumeSetting { get; set; }

    private static AudioStreamPlayer? _player;
    private static AudioStream? _wrapStream;
    private static bool _loadFailed;

    /// <summary>
    /// Plays the wrap sound when a focus move went against the pressed nav
    /// direction — e.g. right from the rightmost hand card landing on the
    /// leftmost. Direction comes from the engine's ui_* actions, which both
    /// remapped keyboard input and controller d-pad/stick produce.
    /// </summary>
    public static void CheckWrap(Control? from, Control? to)
    {
        try
        {
            if (WrapSoundSetting is { Value: false })
                return;
            if (from == null || to == null || from == to)
                return;
            if (!GodotObject.IsInstanceValid(from) || !GodotObject.IsInstanceValid(to))
                return;

            var oldCenter = from.GetGlobalRect().GetCenter();
            var newCenter = to.GetGlobalRect().GetCenter();

            bool wrapped =
                (Godot.Input.IsActionPressed("ui_right") && newCenter.X < oldCenter.X - WrapEpsilon)
                || (Godot.Input.IsActionPressed("ui_left") && newCenter.X > oldCenter.X + WrapEpsilon)
                || (Godot.Input.IsActionPressed("ui_down") && newCenter.Y < oldCenter.Y - WrapEpsilon)
                || (Godot.Input.IsActionPressed("ui_up") && newCenter.Y > oldCenter.Y + WrapEpsilon);

            if (wrapped)
                PlayWrap();
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Wrap sound check failed: {e.Message}");
        }
    }

    public static void PlayWrap()
    {
        try
        {
            if (_loadFailed) return;
            EnsurePlayer();
            if (_player == null || !GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree())
                return;
            var volume = VolumeSetting?.Get() ?? 100;
            _player.VolumeDb = Mathf.LinearToDb(volume / 100f);
            _player.Play();
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Wrap sound playback failed: {e.Message}");
        }
    }

    private static void EnsurePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player))
            return;

        if (_wrapStream == null)
        {
            var bytes = FileAccess.GetFileAsBytes(WrapSoundPath);
            if (bytes == null || bytes.Length == 0)
            {
                _loadFailed = true;
                Log.Error($"[AccessibilityMod] Could not read {WrapSoundPath} from the mod PCK.");
                return;
            }
            _wrapStream = AudioStreamWav.LoadFromBuffer(bytes);
            if (_wrapStream == null)
            {
                _loadFailed = true;
                Log.Error("[AccessibilityMod] Failed to decode wrap.wav.");
                return;
            }
        }

        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        _player = new AudioStreamPlayer
        {
            Name = "AccessibilitySoundPlayer",
            Stream = _wrapStream,
        };
        tree.Root.AddChild(_player);
    }
}
