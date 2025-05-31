using litematic_to_sandmatic.LitematicaCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using TextCopy;
using Region = litematic_to_sandmatic.LitematicaCS.Region;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string owner = "Bknibb";
    private const string repo = "litematic-to-sandmatic";
    public static Assembly assembly = Assembly.GetEntryAssembly();
    private static FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
    private static string ProductName = versionInfo.ProductName;
    private static string ProductVersion = versionInfo.ProductVersion;
    private static Version Version = new Version(ProductVersion);
    private const string CacheFile = "release_cache.json";
    public class CacheData
    {
        public DateTime LastChecked { get; set; }
    }
    public static async Task<CacheData?> LoadCacheAsync()
    {
        if (!File.Exists(CacheFile)) return null;

        var json = await File.ReadAllTextAsync(CacheFile);
        return JsonSerializer.Deserialize<CacheData>(json);
    }

    public static async Task SaveCacheAsync(CacheData data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(CacheFile, json);
    }

    public static bool IsCacheValid(CacheData? data)
    {
        return data != null && (DateTime.UtcNow - data.LastChecked).TotalHours < 1;
    }

    public static async Task<string?> GetLatestReleaseAsync()
    {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductName.Replace(" ", "-"), ProductVersion));

        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching latest release: {response.StatusCode}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        var tagName = jsonDoc.RootElement.GetProperty("tag_name").GetString();

        return tagName;
    }
    static async Task UpdateCheck()
    {
        var cachedData = await LoadCacheAsync();
        if (!IsCacheValid(cachedData))
        {
            string? latestRelease = await GetLatestReleaseAsync();
            if (latestRelease != null && new Version(latestRelease) > Version)
            {
                Console.WriteLine($"Update Available: {latestRelease}");
                Console.WriteLine($"https://github.com/Bknibb/litematic-to-sandmatic/releases");
                MessageBox.Show($"Update Available: {latestRelease}", "Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await SaveCacheAsync(new CacheData { LastChecked = DateTime.UtcNow });
            }
        }
    }
    [STAThread] // Required for file dialogs and clipboard
    static void Main()
    {
        var updateCheckTask = UpdateCheck();
        updateCheckTask.Wait();
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Litematica (*.litematic)|*.litematic",
            Title = "Choose the schematic to convert"
        };

        if (openFileDialog.ShowDialog() != DialogResult.OK)
            return;

        string file = openFileDialog.FileName;

        Schematic schem = Schematic.load(file);
        Region reg = schem.Regions.Values.First();

        var outDict = new Dictionary<string, object>();
        outDict["PosEnd"] = $"{reg.maxX()},{reg.maxY()},{reg.maxZ()}";
        string name = String.IsNullOrEmpty(schem.name) ? Path.GetFileNameWithoutExtension(file) : schem.name;
        if (name.Length > 30)
        {
            MessageBox.Show("The name is too long, it will be trimmed", "Name Too Long", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Console.WriteLine("The name is too long, it will be trimmed");
            name = name.Substring(0, 30);
        }
        outDict["Name"] = name;
        outDict["Time"] = (int)(schem.created / 1000);

        var data = new List<Dictionary<string, object>>();

        Dictionary<string, object> ConvertBlock(string bid, string baxis, (double X, double Y, double Z) bpos)
        {
            var blockDict = new Dictionary<string, object>
            {
                ["p"] = $"{bpos.X},{bpos.Y},{bpos.Z}",
                ["b"] = bid,
                //["r"] = "u"
            };

            if (baxis == "x") blockDict["r"] = "f";
            else if (baxis == "z") blockDict["r"] = "l";

            return blockDict;
        }
        foreach (var pos in reg.blockPositions())
        {
            BlockState b = reg[pos.Item1, pos.Item2, pos.Item3];

            string bid = b.id.StartsWith("minecraft:") ? b.id[10..] : "air";
            if (bid == "air") continue;
            string? btype = b["type"];
            string? baxis = b["axis"];

            var bpos = (X: (double)pos.Item1, Y: (double)pos.Item2, Z: (double)pos.Item3);

            if (btype == "double")
            {
                bpos.Y -= 0.25;
                data.Add(ConvertBlock(bid, baxis, (bpos.X, bpos.Y + 0.5, bpos.Z)));
            }
            else if (btype == "bottom")
            {
                bpos.Y -= 0.25;
            }
            else if (btype == "top")
            {
                bpos.Y += 0.25;
            }

            data.Add(ConvertBlock(bid, baxis, bpos));
        }

        outDict["Data"] = data;
        outDict["PosStart"] = $"{-reg.minX()},{reg.minY()},{-reg.minZ()}";

        string output = JsonSerializer.Serialize(outDict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        const int maxSize = 200000;
        if (output.Length > maxSize)
        {
            MessageBox.Show("The output sandmatic is too large, please try a smaller schematic", "Sandmatic Too Big", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception("The output sandmatic is too large, please try a smaller schematic");
        }

        byte[] compressed;
        using (var outputStream = new MemoryStream())
        {
            using (var compressionStream = new ZLibStream(outputStream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(compressionStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(output);
            }
            compressed = outputStream.ToArray();
        }

        string compressedB64 = Convert.ToBase64String(compressed);

        if (compressedB64.Length > 50000)
        {
            MessageBox.Show("The output sandmatic is too large, please try a smaller schematic", "Sandmatic Too Big", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception("The output sandmatic is too large, please try a smaller schematic");
        }

        ClipboardService.SetText(compressedB64);

        Console.WriteLine("The output sandmatic has been copied to your clipboard");
        MessageBox.Show("The output sandmatic has been copied to your clipboard", "Conversion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
