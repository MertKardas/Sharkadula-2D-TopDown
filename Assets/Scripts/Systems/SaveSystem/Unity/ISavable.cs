using System;

public interface ISavable {
    Type StateType { get; }
    object CaptureState();
    void RestoreState(object data);
}
