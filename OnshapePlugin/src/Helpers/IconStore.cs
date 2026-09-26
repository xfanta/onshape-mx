// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    // Onshape's own toolbar icons, cached on disk.
    //
    // The icons are PTC's, so they are neither in the repository nor in the
    // package. The extension lifts them out of the page's SVG sprite - one
    // <symbol id="svg-icon-NAME-button"> per tool - and the plugin keeps them in
    // its data folder. They therefore always match the Onshape the user has.

    internal sealed class IconStore
    {
        private static readonly Regex SymbolTag = new Regex(@"^\s*<symbol\b([^>]*)>", RegexOptions.Compiled);
        private static readonly Regex ViewBox = new Regex(@"\bviewBox\s*=\s*""([^""]*)""", RegexOptions.Compiled);
        private static readonly Regex SymbolEnd = new Regex(@"</symbol>\s*$", RegexOptions.Compiled);
        private static readonly Regex SafeName = new Regex(@"^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);

        public const String OwnPrefix = "mx-";

        private readonly String _folder;
        private readonly ConcurrentDictionary<String, String> _cache = new ConcurrentDictionary<String, String>();

        public IconStore(String folder)
        {
            this._folder = folder;
            Directory.CreateDirectory(folder);
        }

        public event EventHandler Changed;

        public Boolean Has(String name) => this.Get(name) != null;

        // The icon as a standalone SVG document, or null if it has not been fetched.
        public String Get(String name)
        {
            if (String.IsNullOrEmpty(name) || !SafeName.IsMatch(name))
            {
                return null;
            }

            return this._cache.GetOrAdd(name, n =>
            {
                if (n.StartsWith(OwnPrefix, StringComparison.Ordinal))
                {
                    return PluginResources.ReadTextFile(n + ".svg");
                }

                var file = this.PathOf(n);
                return File.Exists(file) ? File.ReadAllText(file) : null;
            });
        }

        public void Save(IReadOnlyDictionary<String, String> symbols)
        {
            var changed = 0;
            foreach (var (name, symbol) in symbols)
            {
                if (!SafeName.IsMatch(name) || String.IsNullOrEmpty(symbol))
                {
                    continue;
                }

                var svg = ToSvg(symbol);
                if (svg == null || svg == this.Get(name))
                {
                    continue;
                }

                File.WriteAllText(this.PathOf(name), svg);
                this._cache[name] = svg;
                changed++;
            }

            if (changed > 0)
            {
                PluginLog.Info($"Stored {changed} Onshape icons in {this._folder}");
                this.Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private String PathOf(String name) => Path.Combine(this._folder, name + ".svg");

        // <symbol id="…" viewBox="0 0 20 20">…</symbol> -> <svg viewBox="0 0 20 20">…</svg>
        private static String ToSvg(String symbol)
        {
            var tag = SymbolTag.Match(symbol);
            if (!tag.Success)
            {
                return null;
            }

            var viewBox = ViewBox.Match(tag.Groups[1].Value);
            var box = viewBox.Success ? viewBox.Groups[1].Value : "0 0 20 20";

            var body = SymbolEnd.Replace(symbol.Substring(tag.Length), "");
            return $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" viewBox=""{box}"">{body}</svg>";
        }
    }
}
