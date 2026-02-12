using k8s.Operator.Configuration;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(Command = "update", Description = "Updates the cli if available")]
public class UpdateCommand(OperatorConfiguration config) : IOperatorCommand
{
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "k8sOperator-CLI-Updater" } }
    };

    // Add a static readonly JsonSerializerOptions field to cache the instance
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    static UpdateCommand()
    {
        CachedJsonSerializerOptions.TypeInfoResolverChain.Add(UpdateCommandJsonSerializerContext.Default);
    }

    public async Task RunAsync(string[] args)
    {
        try
        {
            Console.WriteLine($"Current version: {config.Version}");
            Console.WriteLine("Checking for updates...");

            var latestRelease = await GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                Console.WriteLine("Could not retrieve latest release information.");
                return;
            }

            var latestVersion = latestRelease.TagName?.TrimStart('v');
            if (string.IsNullOrEmpty(latestVersion))
            {
                Console.WriteLine("Could not determine latest version.");
                return;
            }

            Console.WriteLine($"Latest version: {latestVersion}");

            if (IsNewerVersion(config.Version, latestVersion))
            {
                Console.WriteLine($"New version available: {latestVersion}");
                Console.Write("Do you want to update? (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLower();

                if (response == "y" || response == "yes")
                {
                    await DownloadAndInstallAsync(latestRelease);
                }
                else
                {
                    Console.WriteLine("Update cancelled.");
                }
            }
            else
            {
                Console.WriteLine("You are already running the latest version.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during update: {ex.Message}");
        }
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(config.UpdateUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<GitHubRelease>(json, CachedJsonSerializerOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static bool IsNewerVersion(string current, string latest)
    {
        var currentVersion = ParseVersionWithPrerelease(current);
        var latestVersion = ParseVersionWithPrerelease(latest);

        // Compare major.minor.patch
        for (int i = 0; i < 3; i++)
        {
            if (latestVersion.Parts[i] > currentVersion.Parts[i])
                return true;
            if (latestVersion.Parts[i] < currentVersion.Parts[i])
                return false;
        }

        // Versions are equal, check pre-release
        // Stable version (no pre-release) is always newer than pre-release
        if (latestVersion.Prerelease == null && currentVersion.Prerelease != null)
            return true;
        if (latestVersion.Prerelease != null && currentVersion.Prerelease == null)
            return false;

        // Both have pre-release tags
        if (latestVersion.Prerelease != null && currentVersion.Prerelease != null)
        {
            // Compare pre-release type (alpha < beta < rc)
            var currentPriority = GetPrereleasePriority(currentVersion.Prerelease);
            var latestPriority = GetPrereleasePriority(latestVersion.Prerelease);

            if (latestPriority > currentPriority)
                return true;
            if (latestPriority < currentPriority)
                return false;

            // Same type, compare numbers
            if (latestVersion.PrereleaseNumber > currentVersion.PrereleaseNumber)
                return true;
            if (latestVersion.PrereleaseNumber < currentVersion.PrereleaseNumber)
                return false;
        }

        return false; // Versions are equal
    }

    private static VersionInfo ParseVersionWithPrerelease(string version)
    {
        version = version.TrimStart('v');

        // Split on '-' to separate version from pre-release
        var versionParts = version.Split('-', 2);
        var mainVersion = versionParts[0];
        var prerelease = versionParts.Length > 1 ? versionParts[1] : null;

        // Parse main version (major.minor.patch)
        var parts = mainVersion.Split('.');
        var versionNumbers = new int[3];

        for (int i = 0; i < Math.Min(parts.Length, 3); i++)
        {
            if (int.TryParse(parts[i], out var number))
            {
                versionNumbers[i] = number;
            }
        }

        // Parse pre-release number (e.g., "alpha0004" -> 4)
        int prereleaseNumber = 0;
        if (prerelease != null)
        {
            // Extract trailing digits
            var digitStart = prerelease.Length - 1;
            while (digitStart >= 0 && char.IsDigit(prerelease[digitStart]))
            {
                digitStart--;
            }
            digitStart++;

            if (digitStart < prerelease.Length)
            {
                int.TryParse(prerelease[digitStart..], out prereleaseNumber);
            }
        }

        return new VersionInfo
        {
            Parts = versionNumbers,
            Prerelease = prerelease,
            PrereleaseNumber = prereleaseNumber
        };
    }

    private static int GetPrereleasePriority(string prerelease)
    {
        var lower = prerelease.ToLowerInvariant();

        if (lower.StartsWith("alpha"))
            return 1;
        if (lower.StartsWith("beta"))
            return 2;
        if (lower.StartsWith("rc"))
            return 3;

        return 0; // Unknown pre-release type
    }

    private record VersionInfo
    {
        public required int[] Parts { get; init; }
        public string? Prerelease { get; init; }
        public int PrereleaseNumber { get; init; }
    }

    private static async Task DownloadAndInstallAsync(GitHubRelease release)
    {
        var assetName = GetAssetNameForPlatform();
        var asset = release.Assets?.FirstOrDefault(a =>
            a.Name?.Contains(assetName, StringComparison.OrdinalIgnoreCase) ?? false);

        if (asset == null)
        {
            Console.WriteLine($"Could not find compatible release asset for platform: {assetName}");
            return;
        }

        Console.WriteLine($"Downloading {asset.Name}...");

        var tempFile = Path.Combine(Path.GetTempPath(), asset.Name);
        var downloadUrl = asset.BrowserDownloadUrl;

        if (string.IsNullOrEmpty(downloadUrl))
        {
            Console.WriteLine("Download URL is not available.");
            return;
        }

        using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(tempFile);
            await response.Content.CopyToAsync(fileStream);
        }

        Console.WriteLine("Download complete. Installing...");

        var currentExecutable = Environment.ProcessPath ?? Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrEmpty(currentExecutable))
        {
            Console.WriteLine("Could not determine current executable path.");
            return;
        }

        var currentDirectory = Path.GetDirectoryName(currentExecutable);
        if (string.IsNullOrEmpty(currentDirectory))
        {
            Console.WriteLine("Could not determine installation directory.");
            return;
        }

        // Extract if zip, otherwise just replace
        if (asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var extractPath = Path.Combine(Path.GetTempPath(), $"k8sOperator-{Guid.NewGuid()}");
            Directory.CreateDirectory(extractPath);
            await ZipFile.ExtractToDirectoryAsync(tempFile, extractPath);

            // Find the executable in the extracted files
            var executableName = Path.GetFileName(currentExecutable);
            var newExecutable = Directory.GetFiles(extractPath, executableName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (newExecutable != null)
            {
                await ReplaceExecutableAsync(newExecutable, currentExecutable);
            }

            Directory.Delete(extractPath, true);
        }
        else
        {
            await ReplaceExecutableAsync(tempFile, currentExecutable);
        }

        File.Delete(tempFile);
        Console.WriteLine("Update completed successfully!");
        Console.WriteLine("Please restart the application to use the new version.");
    }

    private static async Task ReplaceExecutableAsync(string newExecutable, string currentExecutable)
    {
        var backupPath = currentExecutable + ".bak";

        // Create backup
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }
        File.Move(currentExecutable, backupPath);

        try
        {
            File.Copy(newExecutable, currentExecutable, true);

            // Set executable permissions on Unix-like systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var chmod = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{currentExecutable}\"",
                    UseShellExecute = false
                });
                await chmod!.WaitForExitAsync();
            }

            // Delete backup if successful
            File.Delete(backupPath);
        }
        catch
        {
            // Restore backup on failure
            if (File.Exists(backupPath))
            {
                File.Move(backupPath, currentExecutable, true);
            }
            throw;
        }
    }

    private static string GetAssetNameForPlatform()
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
                 RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos" : "unknown";

        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }

    internal class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        public string? Name { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    internal class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}

[JsonSerializable(typeof(UpdateCommand.GitHubRelease))]
[JsonSerializable(typeof(UpdateCommand.GitHubAsset))]
[JsonSerializable(typeof(List<UpdateCommand.GitHubAsset>))]
internal partial class UpdateCommandJsonSerializerContext : JsonSerializerContext
{
}

