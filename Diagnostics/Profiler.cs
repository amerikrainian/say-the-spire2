using System;
using System.Collections.Generic;
using System.Diagnostics;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Events;

namespace SayTheSpire2.Diagnostics;

/// <summary>
/// Frame profiler aimed at the failure mode plain per-section timing misses:
/// intermittent stutter. Per-call averages stay flat even when the game
/// hitches, because a single 30 ms GC pause is invisible once divided across
/// hundreds of fast frames. Instead this tracks, per frame, the inter-frame
/// period (so dropped frames surface), the bytes allocated, and GC collection
/// counts — and attributes time *and* allocations to our timed sections. It
/// emits a rolling window summary plus a one-line breakdown whenever a frame
/// exceeds the spike threshold.
///
/// Why these signals: if frame periods spike but our section times stay low,
/// the cost is game-side or GC, not our per-frame work. If allocations are
/// high and gen0/gen1 counts climb during spikes, it's GC pressure. The
/// per-section allocation column then says which of our sections is the
/// source. Everything is gated on the Advanced / Performance Profiling toggle
/// (<see cref="EventDispatcher.Profiling"/>) and is allocation-free on the hot
/// path (struct scopes, constant string keys, counter reads only).
/// </summary>
public static class Profiler
{
    /// <summary>A frame period above this (ms) is treated as a stutter.</summary>
    private const double SpikeMs = 25.0;

    /// <summary>Frames per rolling summary (~10s at 60 fps).</summary>
    private const int WindowFrames = 600;

    public static bool Enabled => EventDispatcher.Profiling;

    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

    // Per-frame scratch (single-threaded: all of this runs on the main thread).
    private static long _lastFrameTs;
    private static long _frameStartTs;
    private static long _frameStartAlloc;
    private static double _periodMs;
    private static readonly Dictionary<string, long> _frameSectionTicks = new();
    private static readonly Dictionary<string, long> _frameSectionAlloc = new();

    // Window aggregates.
    private static bool _wasEnabled;
    private static int _windowFrames;
    private static double _windowPeriodSumMs;
    private static double _windowPeriodMaxMs;
    private static int _windowDropped;
    private static long _windowAllocTotal;
    private static long _windowAllocMaxFrame;
    private static readonly int[] _gcStart = new int[3];
    private static readonly List<double> _windowPeriods = new();
    private static readonly Dictionary<string, SectionAgg> _windowSections = new();

    private struct SectionAgg
    {
        public double TotalMs;
        public double MaxMs;
        public long TotalAlloc;
        public int Count;
    }

    /// <summary>
    /// Times and measures the allocations of the wrapped block, accumulating
    /// into the current frame. Use with <c>using</c>. Inert when profiling is
    /// off (no timestamp / counter reads).
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly long _startTs;
        private readonly long _startAlloc;

        internal Scope(string? name)
        {
            _name = name;
            if (name != null)
            {
                _startTs = Stopwatch.GetTimestamp();
                _startAlloc = GC.GetTotalAllocatedBytes(false);
            }
            else
            {
                _startTs = 0;
                _startAlloc = 0;
            }
        }

        public void Dispose()
        {
            if (_name == null) return;
            long ticks = Stopwatch.GetTimestamp() - _startTs;
            long bytes = GC.GetTotalAllocatedBytes(false) - _startAlloc;

            _frameSectionTicks.TryGetValue(_name, out var t);
            _frameSectionTicks[_name] = t + ticks;
            _frameSectionAlloc.TryGetValue(_name, out var b);
            _frameSectionAlloc[_name] = b + bytes;
        }
    }

    public static Scope Section(string name) => new(Enabled ? name : null);

    public static void BeginFrame()
    {
        if (!Enabled)
        {
            _wasEnabled = false;
            return;
        }

        long now = Stopwatch.GetTimestamp();

        if (!_wasEnabled)
        {
            // Just turned on (or first frame) — reset state so a stale
            // timestamp doesn't manufacture a bogus opening spike.
            _wasEnabled = true;
            ResetWindow();
            _lastFrameTs = now;
            Log.Info($"[Profile] Frame profiler started (spike>{SpikeMs:F0}ms, window={WindowFrames} frames).");
        }

        _periodMs = (now - _lastFrameTs) * TicksToMs;
        _lastFrameTs = now;
        _frameStartTs = now;
        _frameStartAlloc = GC.GetTotalAllocatedBytes(false);
        _frameSectionTicks.Clear();
        _frameSectionAlloc.Clear();
    }

    public static void EndFrame()
    {
        if (!Enabled) return;

        long allocFrame = GC.GetTotalAllocatedBytes(false) - _frameStartAlloc;
        double ourWorkMs = (Stopwatch.GetTimestamp() - _frameStartTs) * TicksToMs;

        _windowFrames++;
        _windowPeriodSumMs += _periodMs;
        if (_periodMs > _windowPeriodMaxMs) _windowPeriodMaxMs = _periodMs;
        if (_periodMs > SpikeMs) _windowDropped++;
        _windowPeriods.Add(_periodMs);
        _windowAllocTotal += allocFrame;
        if (allocFrame > _windowAllocMaxFrame) _windowAllocMaxFrame = allocFrame;

        foreach (var kv in _frameSectionTicks)
        {
            double ms = kv.Value * TicksToMs;
            _frameSectionAlloc.TryGetValue(kv.Key, out var bytes);
            _windowSections.TryGetValue(kv.Key, out var agg);
            agg.TotalMs += ms;
            if (ms > agg.MaxMs) agg.MaxMs = ms;
            agg.TotalAlloc += bytes;
            agg.Count++;
            _windowSections[kv.Key] = agg;
        }

        if (_periodMs > SpikeMs)
            LogSpike(allocFrame, ourWorkMs);

        if (_windowFrames >= WindowFrames)
            FlushWindow();
    }

    private static void LogSpike(long allocFrame, double ourWorkMs)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in _frameSectionTicks)
            sb.Append(' ').Append(kv.Key).Append('=').Append((kv.Value * TicksToMs).ToString("F1")).Append("ms");
        Log.Info($"[Profile] SPIKE {_periodMs:F1}ms frame (our work {ourWorkMs:F1}ms, alloc {Mb(allocFrame)}):{sb}");
    }

    private static void FlushWindow()
    {
        double wallMs = _windowPeriodSumMs;
        double avgMs = _windowFrames > 0 ? _windowPeriodSumMs / _windowFrames : 0;
        double p99 = Percentile(_windowPeriods, 0.99);
        double rateMbs = wallMs > 0 ? _windowAllocTotal / 1048576.0 / (wallMs / 1000.0) : 0;

        Log.Info($"[Profile] === window: {_windowFrames} frames over {wallMs / 1000.0:F1}s ===");
        Log.Info($"[Profile] frame ms: avg={avgMs:F1} max={_windowPeriodMaxMs:F1} p99={p99:F1} dropped(>{SpikeMs:F0}ms)={_windowDropped}");
        Log.Info($"[Profile] alloc: total={Mb(_windowAllocTotal)} avg={Kb(_windowAllocTotal / Math.Max(1, _windowFrames))}/frame max={Mb(_windowAllocMaxFrame)}/frame rate={rateMbs:F1}MB/s");
        Log.Info($"[Profile] GC collections: gen0={GC.CollectionCount(0) - _gcStart[0]} gen1={GC.CollectionCount(1) - _gcStart[1]} gen2={GC.CollectionCount(2) - _gcStart[2]}");

        // Sections ordered by total time descending.
        var ordered = new List<KeyValuePair<string, SectionAgg>>(_windowSections);
        ordered.Sort((a, b) => b.Value.TotalMs.CompareTo(a.Value.TotalMs));
        Log.Info("[Profile] sections (avg ms / max ms / avg alloc):");
        foreach (var kv in ordered)
        {
            var s = kv.Value;
            int n = Math.Max(1, s.Count);
            Log.Info($"[Profile]   {kv.Key}: {s.TotalMs / n:F3} / {s.MaxMs:F3} / {Kb(s.TotalAlloc / n)}");
        }

        ResetWindow();
    }

    private static void ResetWindow()
    {
        _windowFrames = 0;
        _windowPeriodSumMs = 0;
        _windowPeriodMaxMs = 0;
        _windowDropped = 0;
        _windowAllocTotal = 0;
        _windowAllocMaxFrame = 0;
        _windowPeriods.Clear();
        _windowSections.Clear();
        for (int g = 0; g < 3; g++) _gcStart[g] = GC.CollectionCount(g);
    }

    private static double Percentile(List<double> values, double p)
    {
        if (values.Count == 0) return 0;
        var copy = new List<double>(values);
        copy.Sort();
        int idx = (int)Math.Ceiling(p * copy.Count) - 1;
        idx = Math.Clamp(idx, 0, copy.Count - 1);
        return copy[idx];
    }

    private static string Mb(long bytes) => $"{bytes / 1048576.0:F2}MB";
    private static string Kb(long bytes) => $"{bytes / 1024.0:F0}KB";
}
