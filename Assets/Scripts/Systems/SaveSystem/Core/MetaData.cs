using System;

[Serializable]
public class MetaData {
    public const int CurrentFormatVersion = 2;
    public string SaveName;
    public string DisplayName;
    public DateTime SaveTime;
    public string GameVersion;
    public int FormatVersion;

    public MetaData() { }

    public MetaData(string saveName, string displayName, DateTime saveTime, string gameVersion) {
        SaveName = saveName;
        DisplayName = displayName;
        SaveTime = saveTime;
        GameVersion = gameVersion;
        FormatVersion = CurrentFormatVersion;
    }
}
