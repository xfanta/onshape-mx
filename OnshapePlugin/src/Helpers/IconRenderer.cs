// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Text.RegularExpressions;

    internal enum IconTone
    {
        Active,
        Idle,
        Disabled,
    }

    // Recolours Onshape's icons for a black key and rasterises them.
    //
    // Onshape draws its icons for a light toolbar: a dark outline, white faces,
    // grey shading and a blue accent, each written as a CSS custom property with a
    // fallback, e.g. fill="var(--os-icon-outline-primary--static, #333333)". The
    // property names say what each colour is for, so they map onto a dark palette
    // by role. Plain hex colours are greys more often than not and have their
    // lightness flipped.
    //
    // State shows in brightness, as in the Blender plugin: the whole palette is
    // scaled towards black for an idle or a disabled key.

    internal sealed class IconRenderer
    {
        private static readonly Regex Colour = new Regex(
            @"var\(\s*--([\w-]+)\s*(?:,\s*(#[0-9A-Fa-f]{3,8}|[a-z]+))?\s*\)|#[0-9A-Fa-f]{6}\b|#[0-9A-Fa-f]{3}\b",
            RegexOptions.Compiled);

        private static readonly Regex SvgTag = new Regex(@"<svg\b", RegexOptions.Compiled);

        private const String Outline = "#f2f2f2";
        private const String FacePrimary = "#343434";
        private const String FaceSecondary = "#8f8f8f";
        private const String Accent = "#5c9dff";

        private readonly IconStore _store;
        private readonly ConcurrentDictionary<String, BitmapImage> _cache = new ConcurrentDictionary<String, BitmapImage>();

        public IconRenderer(IconStore store)
        {
            this._store = store;
            store.Changed += (s, e) => this._cache.Clear();
        }

        // Null when the icon has not been fetched yet; the key then shows its name.
        public BitmapImage Render(String name, IconTone tone, Int32 size)
        {
            var svg = this._store.Get(name);
            if (svg == null)
            {
                return null;
            }

            return this._cache.GetOrAdd($"{name}|{tone}|{size}", _ => Build(svg, tone, size));
        }

        private static BitmapImage Build(String svg, IconTone tone, Int32 size)
        {
            var brightness = tone switch
            {
                IconTone.Active => 1.0,
                IconTone.Idle => 0.62,
                _ => 0.28,
            };

            svg = Colour.Replace(svg, m => Scale(Recolour(m), brightness));
            svg = SvgTag.Replace(svg, $@"<svg width=""{size}"" height=""{size}""", 1);
            return BitmapImage.FromSvg(svg);
        }

        private static String Recolour(Match match)
        {
            if (!match.Groups[1].Success)
            {
                return FlipGrey(match.Value);
            }

            var role = match.Groups[1].Value;
            var fallback = match.Groups[2].Success ? match.Groups[2].Value : "#333333";

            if (role.Contains("outline"))
            {
                return Outline;
            }

            if (role.Contains("accent"))
            {
                return Accent;
            }

            if (role.Contains("fill-secondary"))
            {
                return FaceSecondary;
            }

            if (role.Contains("fill"))
            {
                return FacePrimary;
            }

            return FlipGrey(fallback);
        }

        // Greys swap ends so a dark stroke stays visible on a black key; colours
        // (errors, warnings, highlights) are kept as they are.
        private static String FlipGrey(String hex)
        {
            if (!TryParse(hex, out var r, out var g, out var b))
            {
                return hex;
            }

            if (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 24)
            {
                return hex;
            }

            var v = 255 - (r + g + b) / 3;
            return Format(v, v, v);
        }

        private static String Scale(String hex, Double factor)
        {
            if (factor >= 1.0 || !TryParse(hex, out var r, out var g, out var b))
            {
                return hex;
            }

            return Format((Int32)(r * factor), (Int32)(g * factor), (Int32)(b * factor));
        }

        private static Boolean TryParse(String hex, out Int32 r, out Int32 g, out Int32 b)
        {
            r = g = b = 0;
            if (hex == null || !hex.StartsWith("#"))
            {
                return false;
            }

            var digits = hex.Substring(1);
            if (digits.Length == 3)
            {
                digits = String.Concat(digits[0], digits[0], digits[1], digits[1], digits[2], digits[2]);
            }

            if (digits.Length < 6
                || !Int32.TryParse(digits.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                return false;
            }

            r = (rgb >> 16) & 0xff;
            g = (rgb >> 8) & 0xff;
            b = rgb & 0xff;
            return true;
        }

        private static String Format(Int32 r, Int32 g, Int32 b) => $"#{r:x2}{g:x2}{b:x2}";
    }
}
