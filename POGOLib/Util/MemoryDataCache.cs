using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using POGOProtos.Networking.Responses;

namespace POGOLib.Util
{
    class MemoryDataCache : IDataCache
    {
        Dictionary<string, IMessage> _cachedData = new Dictionary<string, IMessage>();

        public void SaveData(string fileName, IMessage msg)
        {
            _cachedData[fileName] = msg;
        }

        public async Task<IMessage<T>> GetCachedData<T>(string fileName) where T : IMessage<T>, new()
        {
            IMessage msg = null;
            _cachedData.TryGetValue(fileName, out msg);
            return (IMessage<T>)msg;
        }
    }
}
