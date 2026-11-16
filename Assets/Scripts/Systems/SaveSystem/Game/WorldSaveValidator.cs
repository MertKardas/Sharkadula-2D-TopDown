using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WorldSaveValidator {
    private readonly ItemCatalog _catalog;
    public WorldSaveValidator(ItemCatalog catalog) { _catalog = catalog; }

    public Result Validate(GameSaveData data) {
        if (data?.Objects == null || string.IsNullOrWhiteSpace(data.ResumeSceneName)
            || !Application.CanStreamedLevelBeLoaded(data.ResumeSceneName))
            return Result.Fail("The saved scene is not available.");
        var dynamicIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in data.Objects) {
            var record = entry.Value;
            if (string.IsNullOrWhiteSpace(entry.Key) || record == null)
                return Result.Fail("The save contains an invalid object record.");
            if (record.IsRemoved) {
                if (!string.IsNullOrEmpty(record.PrefabId) || entry.Key == GameSaveService.PlayerId)
                    return Result.Fail("A dynamic object or player cannot have a persistent removal marker.");
                continue;
            }
            var state = record.State;
            if (entry.Key == GameSaveService.PlayerId && state is not PlayerSaveData)
                return Result.Fail("The player record has an invalid state type.");
            if (state is not (PlayerSaveData or DropObjectSaveData or ChestSaveData or DoorSaveData or TrapSaveData or EnemySaveData or SpawnerSaveData))
                return Result.Fail("The save contains an unknown object state type.");
            if (state is SpawnerSaveData spawner) {
                if (spawner.DynamicIds == null || !string.IsNullOrEmpty(record.PrefabId))
                    return Result.Fail("The save contains invalid spawner data.");
                foreach (var id in spawner.DynamicIds) {
                    if (string.IsNullOrWhiteSpace(id) || !dynamicIds.Add(id)
                        || !data.Objects.TryGetValue(id, out var item) || item == null || item.IsRemoved || string.IsNullOrEmpty(item.PrefabId))
                        return Result.Fail("The save contains missing or multiply owned dynamic objects.");
                }
            }
            if (!string.IsNullOrEmpty(record.PrefabId)) {
                if (!Finite(record.SpawnPosition.x) || !Finite(record.SpawnPosition.y) || !Finite(record.SpawnPosition.z)
                    || _catalog.GetDropPrefab(record.PrefabId) == null || state is not DropObjectSaveData dropState || dropState.itemID != record.PrefabId)
                    return Result.Fail("The save contains invalid dynamic object data.");
            }
            if (state is PlayerSaveData player) {
                if (entry.Key != GameSaveService.PlayerId || !string.IsNullOrEmpty(record.PrefabId)
                    || !Finite(player.position.x) || !Finite(player.position.y) || !Finite(player.velocity.x) || !Finite(player.velocity.y)
                    || player.maxHealth <= 0 || player.health < 0 || player.health > player.maxHealth || player.coin < 0
                    || !Finite(player.MaxSpeed) || !Finite(player.acceleration) || !Finite(player.deceleration))
                    return Result.Fail("The save contains invalid player data.");
                foreach (var item in player.items ?? new List<InventoryItem>()) {
                    if (item == null || item.quantity <= 0 || _catalog.GetItem(item.item) == null)
                        return Result.Fail("The save contains an unknown or invalid inventory item.");
                }
                var stateResult = ValidateState(player.StateData, true);
                if (!stateResult.Success) return stateResult;
            }
            if (state is DropObjectSaveData drop && (drop.quantity < 0 || _catalog.GetItem(drop.itemID) == null))
                return Result.Fail("The save contains an unknown or invalid dropped item.");
            if (state is TrapSaveData trap && !Finite(trap.animationTime)) return Result.Fail("The save contains invalid trap data.");
            if (state is EnemySaveData enemy && (!Finite(enemy.Position.x) || !Finite(enemy.Position.y) || !Finite(enemy.Position.z)))
                return Result.Fail("The save contains invalid enemy data.");
        }
        foreach (var entry in data.Objects)
            if (!string.IsNullOrEmpty(entry.Value.PrefabId) && !dynamicIds.Contains(entry.Key))
                return Result.Fail("The save contains an unowned dynamic object.");
        return Result.Ok();
    }

    private static Result ValidateState(StateData state, bool root) {
        if (state == null) return Result.Ok();
        var type = Type.GetType(state.stateTypeName ?? "");
        bool valid = root
            ? type == typeof(LocomotionState) || type == typeof(TakingDamageState) || type == typeof(DeathState)
            : type == typeof(IdleState) || type == typeof(MoveState) || type == typeof(RunState);
        if (!valid || state.customData == null || (!root && state.nestedStateData != null)
            || (root && type != typeof(LocomotionState) && state.nestedStateData != null))
            return Result.Fail("The save contains an invalid player state.");
        return ValidateState(state.nestedStateData, false);
    }

    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
}
