using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.Services
{
    public class PreferenceDataStore : IDataStore
    {
        public T Get<T>(string key)
        {
            return Preferences.Default.Get<T>(key,default(T));
        }

        public object Get(string key, Type type)
        {
            var value=Preferences.Default.Get<object>(key,null);
            if (value != null)
            {
                if (type.IsAssignableFrom(value.GetType()))
                {
                    return value;
                }
            }
            return null;
        }

        public void Save(string key, object value)
        {
            Preferences.Default.Set(key, value);
        }

        public void Save<T>(string key, T value)
        {
            Preferences.Default.Set(key, value);
        }
    }
}
