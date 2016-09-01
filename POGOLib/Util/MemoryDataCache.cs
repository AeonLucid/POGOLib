using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace POGOLib.Util
{
    class MemoryDataCache : IDataCache
    {
        Dictionary<string, IMessage> _cachedData = new Dictionary<string, IMessage>();

        public void SaveData(string fileName, IMessage msg)
        {
            _cachedData[fileName] = msg;
        }

        public Task<IMessage<T>> GetCachedData<T>(string fileName) where T : IMessage<T>, new()
        {
			return Task.Run(() =>
			{
				IMessage msg = null;
				_cachedData.TryGetValue(fileName, out msg);
				return (IMessage<T>)msg;
			});
        }
    }
}
