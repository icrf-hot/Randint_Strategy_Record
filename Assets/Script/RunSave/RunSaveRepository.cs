using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Randint.Data;
using UnityEngine;

public sealed class RunSaveRepository
{
    [Serializable] private sealed class Envelope
    {
        public int version;
        public string payload;
        public string checksum;
    }

    private const int MaxBytes = 1024 * 1024;
    private readonly string file;
    private readonly RunSaveBindings bindings;
    public string FilePath => file;
    public string DirectoryPath => Path.GetDirectoryName(file);
    public RunSaveBindings Bindings => bindings;
    public int SlotId { get; }
    public bool HasFiles => File.Exists(file) || File.Exists(file + ".bak") || File.Exists(file + ".tmp");

    public RunSaveRepository(string directory, RunSaveBindings bindings, int slotId = 1)
    {
        if (slotId < 1) throw new ArgumentOutOfRangeException(nameof(slotId));
        SlotId = slotId;
        file = Path.Combine(Path.GetFullPath(directory), slotId == 1 ? "run.json" : "slot_" + slotId.ToString("D4", System.Globalization.CultureInfo.InvariantCulture) + ".json");
        this.bindings = bindings;
    }

    public bool TryRead(out RunSaveSnapshot snapshot, out RunSaveError error, bool allowEnded = false)
    {
        snapshot = null;
        error = RunSaveError.NotFound;
        if (!File.Exists(file)) return false;
        try
        {
            if (new FileInfo(file).Length > MaxBytes) { error = RunSaveError.InvalidData; return false; }
            string json = File.ReadAllText(file, Encoding.UTF8);
            JsonSyntaxGuard.Validate(json);
            Envelope envelope = JsonUtility.FromJson<Envelope>(json);
            if (envelope.version != 1 || string.IsNullOrEmpty(envelope.payload) ||
                !string.Equals(Hash(envelope.payload), envelope.checksum, StringComparison.Ordinal))
            { error = RunSaveError.InvalidData; return false; }
            JsonSyntaxGuard.Validate(envelope.payload);
            var candidate = JsonUtility.FromJson<RunSaveSnapshot>(envelope.payload);
            if (!PreparedRunRestore.TryPrepare(candidate, bindings, out _, out error, allowEnded)) return false;
            snapshot = candidate;
            return true;
        }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
        catch (Exception) { error = RunSaveError.InvalidData; return false; }
    }

    public bool TryWrite(RunSaveSnapshot snapshot, out RunSaveError error)
    {
        if (!PreparedRunRestore.TryPrepare(snapshot, bindings, out _, out error, true)) return false;
        try
        {
            string payload = JsonUtility.ToJson(snapshot);
            string json = JsonUtility.ToJson(new Envelope { version = 1, payload = payload, checksum = Hash(payload) }, true);
            if (Encoding.UTF8.GetByteCount(json) > MaxBytes) { error = RunSaveError.InvalidData; return false; }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            // Keep the old primary intact until the complete temporary file reaches disk.
            using (var stream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(file)) File.Replace(file + ".tmp", file, file + ".bak");
            else File.Move(file + ".tmp", file);
            error = RunSaveError.None;
            return true;
        }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
    }

    public bool TryDelete(out RunSaveError error)
    {
        try
        {
            // Remove auxiliary files first so an interrupted deletion leaves a usable primary.
            foreach (string path in new[] { file + ".tmp", file + ".bak", file })
                if (File.Exists(path)) File.Delete(path);
            error = RunSaveError.None;
            return true;
        }
        catch (IOException) { error = RunSaveError.IoError; return false; }
        catch (UnauthorizedAccessException) { error = RunSaveError.IoError; return false; }
    }

    private static string Hash(string text)
    {
        using (SHA256 sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }
}
