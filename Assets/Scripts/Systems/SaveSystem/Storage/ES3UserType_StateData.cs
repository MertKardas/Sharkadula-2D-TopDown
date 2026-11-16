using System.Collections.Generic;
using ES3Types;
using UnityEngine.Scripting;

[Preserve]
public sealed class ES3UserType_StateData : ES3ObjectType {
    public ES3UserType_StateData() : base(typeof(StateData)) { priority = 1; }

    protected override void WriteObject(object obj, ES3Writer writer) {
        var state = (StateData)obj;
        writer.WriteProperty("stateTypeName", state.stateTypeName);
        writer.WriteProperty("customData", state.customData);
        // ES3's default reflection skips fields whose type is their own declaring type.
        writer.WriteProperty("nestedStateData", state.nestedStateData);
    }

    protected override object ReadObject<T>(ES3Reader reader) {
        var state = new StateData();
        foreach (string property in reader.Properties) {
            switch (property) {
                case "stateTypeName": state.stateTypeName = reader.Read<string>(); break;
                case "customData": state.customData = reader.Read<Dictionary<string, object>>(); break;
                case "nestedStateData": state.nestedStateData = reader.Read<StateData>(); break;
                default: reader.Skip(); break;
            }
        }
        return state;
    }
}
