using Google.Protobuf;
using POGOLib.Util;
using System;
using System.IO;

namespace FileDataCacheLib
{
    public class FileDataCache : IDataCache
    {
        public string AssetDigestFile => Path.Combine(Environment.CurrentDirectory, "templates.asset-digests.dat");

        public string ItemTemplatesFile => Path.Combine(Environment.CurrentDirectory, "templates.items.dat");

        public IMessage<T> GetCachedData<T>(string fileName) where T: IMessage<T>, new()
        {
            var path = Path.Combine(Environment.CurrentDirectory, fileName);
            if (File.Exists(path))
            {
                return new MessageParser<T>(() => new T()).ParseFrom(File.ReadAllBytes(path));
            }
            return null;
        }

        public void SaveDate(string fileName, IMessage msg)
        {
            var path = Path.Combine(Environment.CurrentDirectory, fileName);
            File.WriteAllBytes(path, msg.ToByteArray());
        }
    }
}
