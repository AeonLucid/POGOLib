using Google.Protobuf;
using POGOProtos.Networking.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POGOLib.Util
{
    public interface IDataCache
    {
        IMessage<T> GetCachedData<T>(string fileName) where T : IMessage<T>, new();
        void SaveDate(string fileName, IMessage msg);
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
    }
}
