using System;
using System.Reflection;

public static class SaveArchitectureChecks {
    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }

    public static string Contract() {
        Check(typeof(ISavable).GetProperty("UniqueId") == null, "Identity must belong only to SaveableEntity.");
        Check(typeof(ISavable).GetProperty("StateType")?.PropertyType == typeof(Type), "State type must be declared without calling Capture.");
        Check(typeof(GameSaveService).GetMethod("CaptureSceneData") == null, "The save session must not capture scene objects.");
        Check(typeof(GameSaveService).GetProperty("CurrentGameSaveData") == null, "The session must not expose a mutable current snapshot.");
        Check(typeof(GameSaveService).GetMethod("SaveScene") == null, "Raw snapshots must enter the session only through its scene context.");
        Check(typeof(GameSaveService).GetMethod("Clear") != null && typeof(GameSaveService).GetProperty("HasLoadedGame") != null,
            "Session reset and state must have explicit APIs.");
        var assembly = typeof(GameSaveService).Assembly;
        Check(assembly.GetType("SceneSaveContext") != null, "Scene capture and startup need one scene-scoped owner.");
        Check(assembly.GetType("DynamicSaveableEntity") == null && assembly.GetType("ISaveDataReader") == null,
            "Replaced identity/reader layers must not remain.");
        Check(typeof(GameSaveData).GetField("Entries") == null && typeof(GameSaveData).GetField("Objects") != null
            && assembly.GetType("WorldSaveData") == null && assembly.GetType("SceneSaveData") == null,
            "One ID table must replace the generic and scene data wrappers.");
        Check(typeof(SaveableEntity).GetProperty("IsRemoved") != null && typeof(SaveableEntity).GetMethod("MarkRemoved") != null,
            "One entity must expose explicit removal intent.");
        return "PASS: identity/state contracts, encapsulated session and scene-scoped ownership.";
    }
}
