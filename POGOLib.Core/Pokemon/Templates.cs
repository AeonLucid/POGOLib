namespace POGOLib.Official.Pokemon
{
    /*
    public class Templates
    {
        private GetAssetDigestResponse _assetDigestResponse;
        private DownloadItemTemplatesResponse _itemTemplatesResponse;

        public Templates(IDataCache templateDataCache)
        {
            _templateDataCache = templateDataCache;
            _assetDigestResponse = LoadAssetDigest();
            _itemTemplatesResponse = LoadItemTemplates();
        }

        public RepeatedField<AssetDigestEntry> AssetDigests => _assetDigestResponse?.Digest;

        public RepeatedField<DownloadItemTemplatesResponse.Types.ItemTemplate> ItemTemplates
            => _itemTemplatesResponse?.ItemTemplates;

        public string AssetDigestFile => "templates.asset-digests.dat";

        public string ItemTemplatesFile => "templates.items.dat";

        public void SetAssetDigests(GetAssetDigestResponse assetDigestResponse)
        {
            if (_assetDigestResponse == null || assetDigestResponse.TimestampMs > _assetDigestResponse.TimestampMs)
            {
                _assetDigestResponse = assetDigestResponse;
                _templateDataCache.SaveTemplateDate(AssetDigestFile, _assetDigestResponse.ToByteString().ToByteArray());
            }
        }

        public void SetItemTemplates(DownloadItemTemplatesResponse itemTemplatesResponse)
        {
            if (_itemTemplatesResponse == null || itemTemplatesResponse.TimestampMs > _itemTemplatesResponse.TimestampMs)
            {
                _itemTemplatesResponse = itemTemplatesResponse;
                _templateDataCache.SaveTemplateDate(ItemTemplatesFile, _itemTemplatesResponse.ToByteString().ToByteArray());
            }
        }

        private GetAssetDigestResponse LoadAssetDigest()
        {
            if (_templateDataCache.HasTemplateData(AssetDigestFile))
            {
                var bytes = _templateDataCache.GetTemplateDataCache(AssetDigestFile);
                if (bytes?.Any() ?? false)
                {
                    return GetAssetDigestResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

        private DownloadItemTemplatesResponse LoadItemTemplates()
        {
            if (_templateDataCache.HasTemplateData(ItemTemplatesFile))
            {
                var bytes = _templateDataCache.GetTemplateDataCache(ItemTemplatesFile);
                if (bytes?.Any() ?? false)
                {
                    return DownloadItemTemplatesResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

    }*/
}