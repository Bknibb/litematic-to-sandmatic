using litematic_to_sandmatic.LitematicaCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using TextCopy;
using Region = litematic_to_sandmatic.LitematicaCS.Region;

class Program
{
    [STAThread] // Required for file dialogs and clipboard
    static void Main()
    {
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
        Debug.WriteLine(reg.Palette.Count);

        var outDict = new Dictionary<string, object>();
        outDict["PosEnd"] = $"{reg.maxX()},{reg.maxY()},{reg.maxZ()}";
        outDict["Name"] = schem.name;
        outDict["Time"] = (int)(schem.created / 1000);

        var data = new List<Dictionary<string, object>>();

        Dictionary<string, object> ConvertBlock(string bid, string baxis, (double X, double Y, double Z) bpos)
        {
            var blockDict = new Dictionary<string, object>
            {
                ["p"] = $"{bpos.X},{bpos.Y},{bpos.Z}",
                ["b"] = bid,
                ["r"] = "u"
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
        outDict["PosStart"] = $"{-reg.minX()},{reg.minY()},{reg.minZ()}";

        string output = JsonSerializer.Serialize(outDict, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        const int maxSize = 4 * 1024 * 1024;
        if (Encoding.UTF8.GetByteCount(output) > maxSize)
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
