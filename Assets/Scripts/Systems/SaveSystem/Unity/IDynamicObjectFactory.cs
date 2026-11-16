using UnityEngine;

public interface IDynamicObjectFactory {
    SaveableEntity Create(string prefabId, string uniqueId, Vector3 position);
}
