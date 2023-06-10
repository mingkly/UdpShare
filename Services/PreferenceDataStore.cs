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
    public class PreferenceDataStore : IDataStore
    {
        public T Get<T>(string key)
        {
            try
            {
                var value = Preferences.Get(key, null, "MyShare");
                if (value != null)
                {
                    return JsonSerializer.Deserialize<T>(value.ToString());
                }
            }
            catch
            {
                Debug.WriteLine(key);
            }

            return default;
        }

        public object Get(string key, Type type)
        {
            var value=Preferences.Get(key,null, "MyShare");
            if (value != null)
            {
                return JsonSerializer.Deserialize(value.ToString(), type);
            }
            return null;
        }

        public void Save(string key, object value)
        {
            Preferences.Set(key, JsonSerializer.Serialize(value),"MyShare");
        }

        public void Save<T>(string key, T value)
        {
            Preferences.Set(key,JsonSerializer.Serialize(value),"MyShare");
        }
    }
}
