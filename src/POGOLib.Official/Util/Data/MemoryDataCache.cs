using System.Collections.Generic;
using Google.Protobuf;

namespace POGOLib.Official.Util.Data
{
    internal class MemoryDataCache : IDataCache
    {
        Dictionary<string, IMessage> _cachedData = new Dictionary<string, IMessage>();

        public void SaveData(string fileName, IMessage msg)
        {
            _cachedData[fileName] = msg;
        }

        public IMessage<T> GetCachedData<T>(string fileName) where T : IMessage<T>, new()
        {
            IMessage msg = null;
            _cachedData.TryGetValue(fileName, out msg);
            return (IMessage<T>)msg;
        }
    }
}
