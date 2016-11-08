using Google.Protobuf;
using POGOProtos.Networking.Responses;

namespace POGOLib.Official.Util.Data
{
    public interface IDataCache
    {
        IMessage<T> GetCachedData<T>(string fileName) where T : IMessage<T>, new();
        void SaveData(string fileName, IMessage msg);
    }

    public static class DataCacheExtensions
    {
        public static string AssetDigestFile => "templates.asset-digests.dat";
        public static string ItemTemplatesFile => "templates.items.dat";

        public static GetAssetDigestResponse GetCachedAssetDigest(this IDataCache dataCache)
        {
            return dataCache.GetCachedData<GetAssetDigestResponse>(AssetDigestFile) as GetAssetDigestResponse;
        }

        public static DownloadItemTemplatesResponse GetCachedItemTemplates(this IDataCache dataCache)
        {
            return dataCache.GetCachedData<DownloadItemTemplatesResponse>(ItemTemplatesFile) as DownloadItemTemplatesResponse;
        }

        public static IMessage<T> ParseMessageFromBytes<T>(this IDataCache dataCache, byte[] data) where T : IMessage<T>, new()
        {
            return new MessageParser<T>(() => new T()).ParseFrom(data);
        }
    }
}
