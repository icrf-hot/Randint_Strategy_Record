using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Randint.Data;
using UnityEngine;

public sealed class RunSaveSlots
{
    [Serializable] private sealed class Pages { public int version = 1; public int count = 1; }
    public const int SlotsPerPage = 10;
    private readonly RunSaveRepository root;
    private string IndexPath => Path.Combine(root.DirectoryPath, "pages.json");
    public RunSaveSlots(RunSaveRepository root) { this.root = root; }
    public RunSaveRepository At(int slot) => new RunSaveRepository(root.DirectoryPath, root.Bindings, slot);

    public int[] ExistingSlotIds()
    {
        var ids = new HashSet<int>();
        if (!Directory.Exists(root.DirectoryPath)) return Array.Empty<int>();
        if (At(1).HasFiles) ids.Add(1);
        foreach (string path in Directory.EnumerateFiles(root.DirectoryPath, "slot_*.json*"))
        {
            string name = Path.GetFileName(path);
            int end = name.IndexOf(".json", StringComparison.Ordinal);
            if (end < 5) continue;
            string suffix = name.Substring(end);
            if (suffix != ".json" && suffix != ".json.bak" && suffix != ".json.tmp") continue;
            if (int.TryParse(name.Substring(5, end - 5), out int id) && id >= 2 && id <= int.MaxValue - 9 &&
                string.Equals(Path.GetFullPath(path), At(id).FilePath + suffix.Substring(5), StringComparison.OrdinalIgnoreCase))
                ids.Add(id);
        }
        return ids.OrderBy(id => id).ToArray();
    }

    public bool TryPageCount(out int count, out RunSaveError error)
    {
        count = 1;
        error = RunSaveError.None;
        try
        {
            int[] ids = ExistingSlotIds();
            if (ids.Length > 0) count = (ids[ids.Length - 1] - 1) / SlotsPerPage + 1;
            if (!File.Exists(IndexPath)) return true;
            if (new FileInfo(IndexPath).Length > 512) { error = RunSaveError.InvalidData; return false; }
            string json = File.ReadAllText(IndexPath);
            JsonSyntaxGuard.Validate(json);
            var pages = JsonUtility.FromJson<Pages>(json);
            if (pages == null || pages.version != 1 || pages.count < 1 || pages.count > int.MaxValue / SlotsPerPage)
            { error = RunSaveError.InvalidData; return false; }
            count = Math.Max(count, pages.count);
            return true;
        }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
        catch (Exception) { error = RunSaveError.InvalidData; return false; }
    }

    public bool TryAddPage(out int count, out RunSaveError error)
    {
        if (!TryPageCount(out count, out error)) return false;
        if (count >= int.MaxValue / SlotsPerPage) { error = RunSaveError.NotAllowed; return false; }
        try
        {
            Directory.CreateDirectory(root.DirectoryPath);
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(new Pages { count = count + 1 }));
            using (var stream = new FileStream(IndexPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(IndexPath)) File.Replace(IndexPath + ".tmp", IndexPath, null);
            else File.Move(IndexPath + ".tmp", IndexPath);
            count++;
            return true;
        }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
    }
}
