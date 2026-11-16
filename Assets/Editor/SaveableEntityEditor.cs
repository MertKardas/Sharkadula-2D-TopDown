using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveableEntityEditor {
    [MenuItem("Tools/Save System/Validate Build Scene IDs")]
    public static void ValidateBuildSceneIds() {
        if (Application.isPlaying) throw new InvalidOperationException("Validate scene IDs outside Play Mode.");
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in EditorBuildSettings.scenes.Where(entry => entry.enabled)) {
            var scene = SceneManager.GetSceneByPath(entry.path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
            try {
                int players = 0;
                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var scope in root.GetComponentsInChildren<SceneLifetimeScope>(true))
                        CheckId(scope.SpawnerSaveId, entry.path, ids);
                    foreach (var entity in root.GetComponentsInChildren<SaveableEntity>(true)) {
                        if (entity.GetComponent<PlayerController>() != null) {
                            if (++players > 1) throw new BuildFailedException("Multiple saved players in " + entry.path);
                            continue;
                        }
                        CheckId(entity.UniqueId, entry.path, ids);
                    }
                }
            } finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("Save IDs are unique across all enabled build scenes.");
    }

    private static void CheckId(string id, string scenePath, Dictionary<string, string> ids) {
        if (string.IsNullOrWhiteSpace(id) || id == GameSaveService.PlayerId || ids.TryGetValue(id, out _))
            throw new BuildFailedException($"Missing, reserved or duplicate save ID '{id}' in '{scenePath}'.");
        ids.Add(id, scenePath);
    }

    [MenuItem("Tools/Save System/Fix Duplicate Scene IDs")]
    public static void FixDuplicateSceneIds() {
        if (Application.isPlaying) throw new InvalidOperationException("Fix scene IDs outside Play Mode.");
        int changed = 0;
        var scenes = UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include)
            .Where(entity => entity.gameObject.scene.IsValid() && entity.gameObject.scene.isLoaded)
            .GroupBy(entity => entity.gameObject.scene);
        foreach (var scene in scenes) {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entity in scene.OrderBy(entity => HierarchyPath(entity.transform), StringComparer.Ordinal)) {
                if (!string.IsNullOrWhiteSpace(entity.UniqueId) && ids.Add(entity.UniqueId)) continue;
                Undo.RecordObject(entity, "Assign unique save ID");
                entity.GenerateNewId();
                ids.Add(entity.UniqueId);
                PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
                EditorUtility.SetDirty(entity);
                EditorSceneManager.MarkSceneDirty(scene.Key);
                changed++;
            }
        }
        Debug.Log($"Assigned {changed} missing or duplicate scene save IDs. Save the scene to keep them.");
    }

    private static string HierarchyPath(Transform target) {
        return target.parent == null ? target.name : HierarchyPath(target.parent) + "/" + target.name;
    }
}

public sealed class SaveIdBuildCheck : IPreprocessBuildWithReport {
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { SaveableEntityEditor.ValidateBuildSceneIds(); }
}
