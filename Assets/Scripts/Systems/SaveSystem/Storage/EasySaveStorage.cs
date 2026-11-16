using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class EasySaveStorage : ISaveStorage {
    private readonly string _directory;

    public EasySaveStorage(string directory) {
        _directory = Path.GetFullPath(directory);
    }

    private ES3Settings Settings(string slotName) {
        if (string.IsNullOrWhiteSpace(slotName) || slotName == "." || slotName == ".."
            || slotName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || slotName.Contains('/') || slotName.Contains('\\') || Path.IsPathRooted(slotName))
            throw new ArgumentException("Invalid save slot name.");
        return new ES3Settings(Path.Combine(_directory, slotName + ".es3")) {
            location = ES3.Location.File,
            encryptionType = ES3.EncryptionType.None,
            compressionType = ES3.CompressionType.None,
            safeReflection = true
        };
    }

    private static MetaData ReadMetadata(ES3Settings settings, string slotName) {
        var metadata = ES3.Load<MetaData>("metadata", settings);
        if (metadata == null || metadata.SaveName != slotName || metadata.FormatVersion != MetaData.CurrentFormatVersion
            || metadata.SaveTime == default)
            throw new InvalidDataException("Invalid or incompatible save metadata.");
        // Easy Save stores DateTime ticks without Kind; this format always writes UTC.
        metadata.SaveTime = DateTime.SpecifyKind(metadata.SaveTime, DateTimeKind.Utc);
        if (!ES3.KeyExists("data", settings)) throw new InvalidDataException("Save data is missing.");
        return metadata;
    }

    public Result Save(GameSaveData data, MetaData metadata) {
        try {
            if (data?.Objects == null || metadata == null || metadata.FormatVersion != MetaData.CurrentFormatVersion
                || metadata.SaveTime == default || metadata.SaveTime.Kind != DateTimeKind.Utc)
                return Result.Fail("Invalid save data or metadata.");
            var settings = Settings(metadata.SaveName);
            Directory.CreateDirectory(_directory);
            var file = new ES3File(settings, false);
            file.Save("metadata", metadata);
            file.Save("data", data);
            file.Sync();
            return Result.Ok();
        } catch (Exception error) {
            return Result.Fail("Save failed: " + error.Message);
        }
    }

    public Result Load(string slotName, out GameSaveData data) {
        data = null;
        try {
            var settings = Settings(slotName);
            ReadMetadata(settings, slotName);
            var loaded = ES3.Load<GameSaveData>("data", settings);
            if (loaded?.Objects == null) return Result.Fail("Invalid save data.");
            data = loaded;
            return Result.Ok();
        } catch (Exception error) {
            return Result.Fail("Load failed: " + error.Message);
        }
    }

    public Result DeleteSave(string slotName) {
        try {
            var settings = Settings(slotName);
            if (!ES3.FileExists(settings)) return Result.Fail("Save file was not found.");
            ES3.DeleteFile(settings);
            return Result.Ok();
        } catch (Exception error) {
            return Result.Fail("Delete failed: " + error.Message);
        }
    }

    public Result GetAllMetaData(out List<MetaData> metadata) {
        metadata = new List<MetaData>();
        try {
            if (!SaveDirectoryAccess.Exists(_directory)) return Result.Ok();
            foreach (var path in Directory.GetFiles(_directory, "*.es3")) {
                var slot = Path.GetFileNameWithoutExtension(path);
                try {
                    metadata.Add(ReadMetadata(Settings(slot), slot));
                } catch (Exception error) {
                    Debug.LogWarning($"Skipping save '{slot}': {error.Message}");
                }
            }
        } catch (Exception error) {
            metadata.Clear();
            return Result.Fail("Could not list saves: " + error.Message);
        }
        return Result.Ok();
    }
}
