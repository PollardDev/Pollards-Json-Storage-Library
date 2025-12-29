using PollardsStorageSys;
using PollardsStorageSys.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,AllowMultiple = false)]
public class SaveAttribute : Attribute { }
// Mark fields or properties with [Save] to include them in auto-save/load operations.
public static class AutoSave {
    private static DataStorage _storage = new DataStorage(@"./AutoSaves");

    public static void SetSaveFolder(string folderPath) {
        _storage = new DataStorage(folderPath);
    }

    public static void SetCustomFileName<T>(string fileName) {
        string key = typeof(T).FullName ?? typeof(T).Name;
        _storage.SetFileName(key,fileName);
    }

    public static async Task SaveAsync<T>(T data) where T : class {
        string key = typeof(T).FullName ?? typeof(T).Name;
        var dict = BuildSaveDictionary(data);

        await _storage.SaveAsync(dict,key);
        Log.LogMessage($"[AutoSave] Saved {typeof(T).Name}");
    }

    public static void Save<T>(T data) where T : class {
        string key = typeof(T).FullName ?? typeof(T).Name;
        var dict = BuildSaveDictionary(data);

        _storage.Save(dict,key);
        Log.LogMessage($"[AutoSave] Saved (sync) {typeof(T).Name}");
    }

    public static async Task<T> LoadAsync<T>() where T : class, new() {
        string key = typeof(T).FullName ?? typeof(T).Name;

        var dict = await _storage.LoadAsync<Dictionary<string, JsonElement>>(key);

        if(dict == null) {
            Log.LogMessage($"[AutoSave] No save found for {typeof(T).Name}");
            return new T();
        }

        return ApplySaveDictionary(new T(),dict);
    }

    public static T Load<T>() where T : class, new() {
        string key = typeof(T).FullName ?? typeof(T).Name;

        var dict = _storage.Load<Dictionary<string, JsonElement>>(key);

        if(dict == null) {
            return new T();
        }

        return ApplySaveDictionary(new T(),dict);
    }

    private static Dictionary<string,object> BuildSaveDictionary<T>(T data) {
        var dict = new Dictionary<string, object>();
        var type = typeof(T);

        foreach(var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if(Attribute.IsDefined(prop,typeof(SaveAttribute))) {
                dict[prop.Name] = prop.GetValue(data);
            }
        }

        foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if(Attribute.IsDefined(field,typeof(SaveAttribute))) {
                dict[field.Name] = field.GetValue(data);
            }
        }

        return dict;
    }

    private static T ApplySaveDictionary<T>(T instance,Dictionary<string,JsonElement> dict) {
        var type = typeof(T);

        foreach(var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if(Attribute.IsDefined(prop,typeof(SaveAttribute)) && dict.TryGetValue(prop.Name,out var element)) {
                object value = element.Deserialize(prop.PropertyType);
                prop.SetValue(instance,value);
            }
        }

        foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            if(Attribute.IsDefined(field,typeof(SaveAttribute)) && dict.TryGetValue(field.Name,out var element)) {
                object value = element.Deserialize(field.FieldType);
                field.SetValue(instance,value);
            }
        }

        return instance;
    }
}