using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.Services
{
    internal class FileDataStore:IDataStore
    {
        Dictionary<string, string> keyJsons;
        string path;
        public FileDataStore(string path = null)
        {
            this.path =path??Path.Combine(FileSystem.AppDataDirectory,"data.db");
            try
            {
                var json=File.ReadAllText(this.path);
                keyJsons=JsonSerializer.Deserialize<Dictionary<string, string>>(json)??new Dictionary<string, string>();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                keyJsons = new Dictionary<string, string>();
            }
        }
        object saveLock = new object();
        public void Save()
        {
            try
            {
                lock (saveLock)
                {
                    if(!File.Exists(this.path))
                    {
                        using var stream= File.Create(this.path);
                    }
                    File.WriteAllText(path,JsonSerializer.Serialize(keyJsons));
                }
            }
            catch { }
        }
        public T Get<T>(string key)
        {
            try
            {
                if(keyJsons.TryGetValue(key,out var value))
                {
                    return JsonSerializer.Deserialize<T>(value.ToString());
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return default;
        }

        public object Get(string key, Type type)
        {
            if (keyJsons.TryGetValue(key, out var value))
            {
                return JsonSerializer.Deserialize(value.ToString(), type);
            }
            return null;
        }

        public void Save(string key, object value)
        {
            keyJsons[key] = JsonSerializer.Serialize(value);
            Save();
        }

        public void Save<T>(string key, T value)
        {
            keyJsons[key]=JsonSerializer.Serialize(value);
            Save();
        }
    }
}
