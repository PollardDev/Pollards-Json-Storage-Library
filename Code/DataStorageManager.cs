using PollardsStorageSys.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PollardsStorageSys {

    // Use At You Own Risk. The Creator Of This Library Is Not Responsible For Any Damage, Harm, Data Loss, Or Other Issues Caused By Using This Library.

    public class DataStorage {
        private readonly DataStorageManager _manager;

        // Creates a new DataStorage instance.

        /// <param name="folderPath">
        /// Optional custom folder for saving files.
        /// Default: %AppData%
        /// </param>
        public DataStorage(string folderPath = null) {
            _manager = new DataStorageManager(folderPath);
        }

        // Changes the folder where all data files are saved
        public void SetFolder(string folderPath) {
            _manager.SetBasePath(folderPath);
        }

        // Sets a custom file name for a data key
        public void SetFileName(string key,string fileName) {
            _manager.SetCustomFileName(key,fileName);
        }

        
        // Saves data asynchronously (recommended for UI apps).
        public async Task SaveAsync<T>(T data,string key) {
            await _manager.SaveDataAsync(data,key);
        }
        
        // Saves data synchronously
        public void Save<T>(T data,string key) {
            _manager.SaveData(data,key);
        }

        // Loads data asynchronously
        public async Task<T> LoadAsync<T>(string key) where T : class {
            return await _manager.LoadDataAsync<T>(key);
        }

        // Loads data synchronously
        public T Load<T>(string key) where T : class {
            return _manager.LoadData<T>(key);
        }

        // Modifies existing data or creates new if missing
        public async Task ModifyAsync<T>(string key,Action<T> modifyAction) where T : class, new() {
            await _manager.ModifyDataAsync(key,modifyAction);
        }

        // Appends an item to a list stored under the key
        public async Task AppendToListAsync<T>(T item,string key) where T : class {
            await _manager.AppendToListAsync(item,key);
        }

        // Deletes the saved data for the key
        public void Delete(string key) {
            _manager.DeleteData(key);
        }

        // Checks if data exists for the key
        public bool Exists(string key) {
            return _manager.Exists(key);
        }

        // Gets all saved data keys
        public IEnumerable<string> GetAllKeys() {
            return _manager.GetAllKeys();
        }

        // Backs up all saved files to another folder
        public void BackupTo(string backupFolderPath) {
            _manager.BackupTo(backupFolderPath);
        }
    }

    internal class DataStorageManager {
        private string _basePath;
        private readonly object _fileLock = new object();
        private readonly Dictionary<string, string> _customFileNames = new Dictionary<string, string>();

        public DataStorageManager(string basePath = null) {
            SetBasePath(basePath);
        }

        public void SetBasePath(string newBasePath = null) {
            try {
                _basePath = newBasePath ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MyAppData");

                if(!Directory.Exists(_basePath)) {
                    Directory.CreateDirectory(_basePath);
                    Log.Log.LogMessage($"Created storage folder: {_basePath}");
                } else {
                    Log.Log.LogMessage($"Using storage folder: {_basePath}");
                }
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to set or create storage folder '{newBasePath ?? "default"}': {ex.Message}");
                throw;
            }
        }

        public void SetCustomFileName(string key,string customFileName) {
            if(string.IsNullOrWhiteSpace(customFileName))
                throw new ArgumentException("File name cannot be empty.",nameof(customFileName));

            lock(_fileLock) {
                _customFileNames[key] = customFileName;
            }

            Log.Log.LogMessage($"Custom file name set: '{key}' → '{customFileName}'");
        }

        private string GetFilePath(string key) {
            lock(_fileLock) {
                if(_customFileNames.TryGetValue(key,out string customName)) {
                    return Path.Combine(_basePath,customName);
                }
            }
            return Path.Combine(_basePath,$"{key}.json");
        }

        public async Task SaveDataAsync<T>(T data,string key) {
            string filePath = GetFilePath(key);

            try {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await Task.Run(() => File.WriteAllText(filePath,json,Encoding.UTF8));
                Log.Log.LogMessage($"Saved data to: {filePath}");
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to save data for key '{key}' to '{filePath}': {ex.Message}");
            }
        }

        public void SaveData<T>(T data,string key) {
            string filePath = GetFilePath(key);

            try {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath,json,Encoding.UTF8);
                Log.Log.LogMessage($"Saved data (sync) to: {filePath}");
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to save data (sync) for key '{key}' to '{filePath}': {ex.Message}");
            }
        }

        public async Task<T> LoadDataAsync<T>(string key) where T : class {
            string filePath = GetFilePath(key);

            if(!File.Exists(filePath)) {
                Log.Log.LogWarning($"Load failed: File not found for key '{key}' at '{filePath}'");
                return null;
            }

            try {
                string json = await Task.Run(() => File.ReadAllText(filePath, Encoding.UTF8));
                var result = JsonSerializer.Deserialize<T>(json);

                if(result == null)
                    Log.Log.LogWarning($"Loaded file for '{key}' but deserialized to null (possibly empty or invalid JSON).");

                Log.Log.LogMessage($"Loaded data from: {filePath}");
                return result;
            } catch(JsonException jsonEx) {
                Log.Log.LogError($"JSON deserialization failed for '{key}' at '{filePath}': {jsonEx.Message}");
                return null;
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to load data for key '{key}' from '{filePath}': {ex.Message}");
                return null;
            }
        }

        public T LoadData<T>(string key) where T : class {
            string filePath = GetFilePath(key);

            if(!File.Exists(filePath)) {
                Log.Log.LogWarning($"Load failed (sync): File not found for key '{key}' at '{filePath}'");
                return null;
            }

            try {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var result = JsonSerializer.Deserialize<T>(json);

                if(result == null)
                    Log.Log.LogWarning($"Loaded file (sync) for '{key}' but deserialized to null.");

                Log.Log.LogMessage($"Loaded data (sync) from: {filePath}");
                return result;
            } catch(JsonException jsonEx) {
                Log.Log.LogError($"JSON deserialization failed (sync) for '{key}': {jsonEx.Message}");
                return null;
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to load data (sync) for key '{key}': {ex.Message}");
                return null;
            }
        }

        public async Task ModifyDataAsync<T>(string key,Action<T> modifier) where T : class, new() {
            T data = await LoadDataAsync<T>(key) ?? new T();
            modifier(data);
            await SaveDataAsync(data,key);
        }

        public async Task AppendToListAsync<T>(T item,string key) where T : class {
            var list = await LoadDataAsync<List<T>>(key) ?? new List<T>();
            list.Add(item);
            await SaveDataAsync(list,key);
            Log.Log.LogMessage($"Appended item to list for key '{key}'");
        }

        public void DeleteData(string key) {
            string filePath = GetFilePath(key);
            if(File.Exists(filePath)) {
                try {
                    File.Delete(filePath);
                    Log.Log.LogMessage($"Deleted file: {filePath}");
                } catch(Exception ex) {
                    Log.Log.LogError($"Failed to delete file for key '{key}': {ex.Message}");
                }
            } else {
                Log.Log.LogWarning($"Delete requested for key '{key}', but file not found: {filePath}");
            }
        }

        public bool Exists(string key) => File.Exists(GetFilePath(key));

        public IEnumerable<string> GetAllKeys() {
            try {
                var files = Directory.GetFiles(_basePath);
                var keys = new HashSet<string>();

                foreach(var file in files) {
                    string fileName = Path.GetFileName(file);
                    string matchingKey = null;

                    lock(_fileLock) {
                        foreach(var kvp in _customFileNames) {
                            if(string.Equals(kvp.Value,fileName,StringComparison.OrdinalIgnoreCase)) {
                                matchingKey = kvp.Key;
                                break;
                            }
                        }
                    }

                    if(matchingKey != null)
                        keys.Add(matchingKey);
                    else if(fileName.EndsWith(".json",StringComparison.OrdinalIgnoreCase))
                        keys.Add(Path.GetFileNameWithoutExtension(fileName));
                }

                return keys;
            } catch(Exception ex) {
                Log.Log.LogError($"Failed to list saved keys in folder '{_basePath}': {ex.Message}");
                return new string[0];
            }
        }

        public void BackupTo(string backupPath) {
            try {
                if(!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                int count = 0;
                foreach(var file in Directory.GetFiles(_basePath)) {
                    string dest = Path.Combine(backupPath, Path.GetFileName(file));
                    File.Copy(file,dest,true);
                    count++;
                }

                Log.Log.LogMessage($"Backup successful: {count} file(s) copied to '{backupPath}'");
            } catch(Exception ex) {
                Log.Log.LogError($"Backup failed to '{backupPath}': {ex.Message}");
            }
        }
    }
}