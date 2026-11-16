using System.Collections.Generic;

public interface ISaveStorage {
    Result Save(GameSaveData data, MetaData metadata);
    Result Load(string slotName, out GameSaveData data);
    Result DeleteSave(string slotName);
    Result GetAllMetaData(out List<MetaData> metadata);
}
