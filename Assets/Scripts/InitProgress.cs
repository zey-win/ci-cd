using System;
using UnityEngine;

public static class InitProgress
{
    public static event Action<float, string> OnProgress;

    private static float _last;

    public static float Last => _last;

    public static void Reset(float start = 0f)
    {
        _last = Mathf.Clamp01(start);
        OnProgress?.Invoke(_last, null);
    }

    public static void Report(float normalized, string stage = null)
    {
        normalized = Mathf.Clamp01(normalized);

        // прогресс не должен уменьшаться
        if (normalized < _last) normalized = _last;

        _last = normalized;
        OnProgress?.Invoke(_last, stage);
    }
}
