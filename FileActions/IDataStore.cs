using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions
{
    public interface IDataStore
    {
        void Save(string key,object value);
        void Save<T>(string key,T value);
        T Get<T>(string key);
        object Get(string key, Type type);
    }
}
