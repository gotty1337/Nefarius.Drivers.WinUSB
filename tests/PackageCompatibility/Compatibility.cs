using System;
#if NET10_0 && !WINDOWS
using System.IO;
using System.Text.Json;
#endif

using Nefarius.Drivers.WinUSB;

internal static class Compatibility
{
    public static Type DeviceType => typeof(USBDevice);

#if NET10_0 && !WINDOWS
    internal static readonly (string Tfm, string Asset)[] ExpectedAssets =
    {
        ("net471", "lib/netstandard2.0/Nefarius.Drivers.WinUSB.dll"),
        ("net472", "lib/net472/Nefarius.Drivers.WinUSB.dll"),
        ("net48", "lib/net48/Nefarius.Drivers.WinUSB.dll"),
        ("net10.0", "lib/netstandard2.0/Nefarius.Drivers.WinUSB.dll"),
        ("net10.0-windows", "lib/net10.0-windows7.0/Nefarius.Drivers.WinUSB.dll"),
    };

    public static int Main(string[] args)
    {
        _ = DeviceType;

        string assetsPath = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "obj", "project.assets.json"));

        if (!File.Exists(assetsPath))
        {
            Console.Error.WriteLine("project.assets.json not found: " + assetsPath);
            return 1;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        if (!document.RootElement.TryGetProperty("targets", out JsonElement targets))
        {
            Console.Error.WriteLine("project.assets.json is missing the 'targets' property.");
            return 1;
        }

        bool failed = false;
        foreach ((string tfm, string expectedAsset) in ExpectedAssets)
        {
            if (!TryGetCompileAsset(targets, tfm, out string actualAsset))
            {
                Console.Error.WriteLine("No Nefarius.Drivers.WinUSB compile asset found for " + tfm + ".");
                failed = true;
                continue;
            }

            if (!string.Equals(actualAsset, expectedAsset, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(tfm + " selected '" + actualAsset + "' but expected '" + expectedAsset + "'.");
                failed = true;
            }
        }

        return failed ? 1 : 0;
    }

    private static bool TryGetCompileAsset(JsonElement targets, string tfm, out string asset)
    {
        asset = null;

        if (!TryGetTarget(targets, tfm, out JsonElement tfmTarget))
        {
            return false;
        }

        foreach (JsonProperty package in tfmTarget.EnumerateObject())
        {
            if (!package.Name.StartsWith("Nefarius.Drivers.WinUSB/", StringComparison.Ordinal))
            {
                continue;
            }

            if (!package.Value.TryGetProperty("compile", out JsonElement compile))
            {
                return false;
            }

            foreach (JsonProperty compileAsset in compile.EnumerateObject())
            {
                if (compileAsset.Name.EndsWith("Nefarius.Drivers.WinUSB.dll", StringComparison.OrdinalIgnoreCase))
                {
                    asset = compileAsset.Name;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetTarget(JsonElement targets, string tfm, out JsonElement tfmTarget)
    {
        if (targets.TryGetProperty(tfm, out tfmTarget))
        {
            return true;
        }

        foreach (JsonProperty property in targets.EnumerateObject())
        {
            if (property.Name.Equals(tfm, StringComparison.OrdinalIgnoreCase))
            {
                tfmTarget = property.Value;
                return true;
            }
        }

        tfmTarget = default;
        return false;
    }
#endif
}
