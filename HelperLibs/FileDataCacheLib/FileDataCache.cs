using Google.Protobuf;
using POGOLib.Util;
using System;
using System.IO;

namespace FileDataCacheLib
{
    public class FileDataCache : IDataCache
    {
        public IMessage<T> GetCachedData<T>(string fileName) where T: IMessage<T>, new()
        {
            var path = Path.Combine(Environment.CurrentDirectory, fileName);
            if (!File.Exists(path))
            {
                return null;
            }
            return this.ParseMessageFromBytes<T>(File.ReadAllBytes(path));
        }

        public void SaveData(string fileName, IMessage msg)
        {
            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, fileName), msg.ToByteArray());
        }
    }
}
