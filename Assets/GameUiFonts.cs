using System;
using UnityEngine;

/// <summary>
/// Unity 2022.3+ throws if you request Arial.ttf via Resources.GetBuiltinResource.
/// Prefer LegacyRuntime.ttf, then guarded Arial, then an OS dynamic font.
/// </summary>
public static class GameUiFonts
{
    static Font _cached;

    /// <returns>Best available font for uGUI Text; rarely null.</returns>
    public static Font DefaultUIFont()
    {
        if (_cached != null)
            return _cached;

        Font f = TryBuiltin("LegacyRuntime.ttf");
        if (f != null)
        {
            _cached = f;
            return _cached;
        }

        f = TryBuiltin("Arial.ttf");
        if (f != null)
        {
            _cached = f;
            return _cached;
        }

        try
        {
            _cached = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Helvetica", "Liberation Sans", "sans-serif" },
                22);
        }
        catch
        {
            _cached = null;
        }

        if (_cached == null)
            Debug.LogWarning("GameUiFonts: No UI font available; Text may not render.");

        return _cached;
    }

    static Font TryBuiltin(string resourceFileName)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(resourceFileName);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
