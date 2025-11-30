using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Applies a subtle phosphor glow effect to the editor
/// </summary>
public static class ScreenGlowManager
{
    private static readonly Dictionary<FrameworkElement, Effect?> _originalEffects = new();

    public static void EnableGlow(FrameworkElement element, Color glowColor, double radius = 8, double opacity = 0.6)
    {
        // Store original effect if not already stored
        if (!_originalEffects.ContainsKey(element))
        {
            _originalEffects[element] = element.Effect;
        }

        // Create subtle drop shadow effect for glow
        var glow = new DropShadowEffect
        {
            Color = glowColor,
            BlurRadius = radius,
            ShadowDepth = 0,
            Opacity = opacity,
            Direction = 0
        };

        element.Effect = glow;
    }

    public static void DisableGlow(FrameworkElement element)
    {
        if (_originalEffects.TryGetValue(element, out var originalEffect))
        {
            element.Effect = originalEffect;
        }
        else
        {
            element.Effect = null;
        }
    }

    public static void SetEnabled(FrameworkElement element, bool enabled, Color? glowColor = null)
    {
        if (enabled)
        {
            var color = glowColor ?? Color.FromRgb(0, 180, 255); // Cyan glow by default
            EnableGlow(element, color);
        }
        else
        {
            DisableGlow(element);
        }
    }
}
