using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class IdSaveSceneFixture {
    private const string FixturePath = "Assets/_IdSaveRegressionB.unity";

    public static string Setup() {
        if (Application.isPlaying) throw new Exception("Setup outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixturePath) != null) throw new Exception("Fixture already exists; do not overwrite.");
        var original = SceneManager.GetActiveScene();
        SaveableEntityEditor.ValidateBuildSceneIds();
        if (!AssetDatabase.CopyAsset("Assets/Scenes/Episode(01).unity", FixturePath)) throw new Exception("Could not create isolated B fixture.");
        var test = EditorSceneManager.OpenScene(FixturePath, OpenSceneMode.Additive);
        try {
        foreach (var root in test.GetRootGameObjects()) {
            foreach (var entity in root.GetComponentsInChildren<SaveableEntity>(true))
                if (entity.GetComponent<PlayerController>() == null) { entity.GenerateNewId(); EditorUtility.SetDirty(entity); }
            foreach (var scope in root.GetComponentsInChildren<SceneLifetimeScope>(true)) {
                var serialized = new SerializedObject(scope);
                serialized.FindProperty("spawnerSaveId").stringValue = Guid.NewGuid().ToString();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var player in root.GetComponentsInChildren<PlayerController>(true)) player.transform.position = new Vector3(10000, 10000, 0);
        }
        EditorSceneManager.SaveScene(test);
        } finally {
            EditorSceneManager.CloseScene(test, true);
            SceneManager.SetActiveScene(original);
        }
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(FixturePath, true) }).ToArray();
        return "PASS: global IDs checked; only isolated additive B copy saved, existing open scenes preserved.";
    }

    public static string Cleanup() {
        if (Application.isPlaying) throw new Exception("Cleanup outside Play Mode.");
        var fixture = SceneManager.GetSceneByPath(FixturePath);
        if (fixture.isLoaded) EditorSceneManager.CloseScene(fixture, true);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(e => e.path != FixturePath).ToArray();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixturePath) != null && !AssetDatabase.DeleteAsset(FixturePath)) throw new Exception("Could not delete owned fixture.");
        SaveableEntityEditor.ValidateBuildSceneIds();
        return "PASS: isolated scene removed; original build scene list retained.";
    }
}
