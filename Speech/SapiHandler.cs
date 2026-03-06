using System;
using System.Speech.Synthesis;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Speech;

public class SapiHandler : ISpeechHandler
{
    private SpeechSynthesizer? _synth;

    public string Key => "sapi";

    public bool Detect()
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Load()
    {
        try
        {
            _synth = new SpeechSynthesizer();
            _synth.Rate = 2;
            _synth.Volume = 100;
            Log.Info("[AccessibilityMod] SAPI handler loaded.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] SapiHandler failed to load: {ex}");
            return false;
        }
    }

    public void Unload()
    {
        _synth?.Dispose();
        _synth = null;
    }

    public bool Speak(string text, bool interrupt = false)
    {
        if (_synth == null) return false;
        if (interrupt) _synth.SpeakAsyncCancelAll();
        _synth.SpeakAsync(text);
        return true;
    }

    public bool Output(string text, bool interrupt = false)
    {
        return Speak(text, interrupt);
    }

    public bool Silence()
    {
        if (_synth == null) return false;
        _synth.SpeakAsyncCancelAll();
        return true;
    }
}
