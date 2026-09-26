// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

namespace Loupedeck.OnshapePlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    // Links the plugin to Onshape, which is a web page rather than an application.
    //
    // Chrome's "Install page as app" gives the page an application of its own: a
    // small app shim in ~/Applications/Chrome Apps with a bundle id derived from
    // the start URL (com.google.Chrome.app.<32 letters>). The id differs between
    // installs, so the shim is recognised by the URL in its Info.plist instead.
    //
    // A plain Chrome tab is covered too, for as long as Options+ asks about the
    // browser's URL.

    public class OnshapeApplication : ClientApplication
    {
        private const String ShimProcessName = "app_mode_loader";
        private const String ShimBundlePrefix = "com.google.Chrome.app.";

        private static readonly String ChromeAppsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Chrome Apps.localized");

        private static readonly Regex ShortcutUrl = new Regex(
            @"<key>CrAppModeShortcutURL</key>\s*<string>([^<]*)</string>", RegexOptions.Compiled);

        private static readonly Regex BundleId = new Regex(
            @"<key>CFBundleIdentifier</key>\s*<string>([^<]*)</string>", RegexOptions.Compiled);

        // Info.plist lookups by app bundle path; shims never change their URL.
        private readonly ConcurrentDictionary<String, Boolean> _shims = new ConcurrentDictionary<String, Boolean>();

        protected override String GetProcessName() => ShimProcessName;

        protected override String GetBundleName() => FindOnshapeShims().Select(s => s.BundleId).FirstOrDefault() ?? "";

        protected override String[] GetBundleNames() => FindOnshapeShims().Select(s => s.BundleId).ToArray();

        protected override Boolean IsProcessNameSupported(String processName) =>
            processName != null && processName.StartsWith(ShimBundlePrefix, StringComparison.Ordinal)
                ? FindOnshapeShims().Any(s => s.BundleId == processName)
                : false;

        // Every Chrome app shim runs the same app_mode_loader executable, so the name
        // alone would also catch Google Meet or YouTube Music. The path tells them apart.
        protected override Boolean IsProcessNameSupported(String processName, String processFilePath)
        {
            if (this.IsProcessNameSupported(processName))
            {
                return true;
            }

            var bundle = BundlePathOf(processFilePath);
            return bundle != null && this._shims.GetOrAdd(bundle, IsOnshapeShim);
        }

        protected override Boolean IsBrowserUrlSupported(String browserUrl) => IsOnshapeUrl(browserUrl);

        public override ClientApplicationStatus GetApplicationStatus() =>
            FindOnshapeShims().Any() ? ClientApplicationStatus.Installed : ClientApplicationStatus.Unknown;

        public static Boolean IsOnshapeUrl(String url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Host == "onshape.com" || uri.Host.EndsWith(".onshape.com", StringComparison.OrdinalIgnoreCase));

        private static (String Path, String BundleId)[] FindOnshapeShims()
        {
            try
            {
                return Directory.Exists(ChromeAppsFolder)
                    ? Directory.GetDirectories(ChromeAppsFolder, "*.app")
                        .Select(path => (Path: path, Plist: ReadPlist(path)))
                        .Where(s => s.Plist != null && IsOnshapeUrl(Match(ShortcutUrl, s.Plist)))
                        .Select(s => (s.Path, Match(BundleId, s.Plist)))
                        .ToArray()
                    : Array.Empty<(String, String)>();
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Could not scan Chrome apps");
                return Array.Empty<(String, String)>();
            }
        }

        private static Boolean IsOnshapeShim(String bundlePath)
        {
            var plist = ReadPlist(bundlePath);
            return plist != null && IsOnshapeUrl(Match(ShortcutUrl, plist));
        }

        // .../Onshape.app/Contents/MacOS/app_mode_loader -> .../Onshape.app
        private static String BundlePathOf(String executablePath)
        {
            if (String.IsNullOrEmpty(executablePath))
            {
                return null;
            }

            var index = executablePath.IndexOf(".app/", StringComparison.Ordinal);
            return index < 0 ? null : executablePath.Substring(0, index + 4);
        }

        // Chrome writes the shim's Info.plist as XML, so a regex is enough; no need
        // to pull in a plist parser for two keys.
        private static String ReadPlist(String bundlePath)
        {
            var file = Path.Combine(bundlePath, "Contents", "Info.plist");
            try
            {
                return File.Exists(file) ? File.ReadAllText(file) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static String Match(Regex regex, String text)
        {
            var match = regex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
