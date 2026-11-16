using System.Collections.Generic;

public class GameSaveData {
    public string ResumeSceneName;
    public Dictionary<string, EntitySaveData> Objects = new();
}
