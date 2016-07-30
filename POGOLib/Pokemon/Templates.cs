using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOProtos.Data;
using POGOProtos.Networking.Responses;
using Splat;

namespace POGOLib.Pokemon
{
    public class Templates
    {
        private const string assetDigestKey = "asset-digests";
        private const string itemTemplatesKey = "items";

        private ITemplateStorage _templateStorage;

        private GetAssetDigestResponse _assetDigestResponse;
        private DownloadItemTemplatesResponse _itemTemplatesResponse;

        public Templates()
        {
            _templateStorage = Locator.Current.GetService<ITemplateStorage>();
        }

        public async Task LoadExisting()
        {
            _assetDigestResponse = await LoadAssetDigest();
            _itemTemplatesResponse = await LoadItemTemplates();
        }

        public RepeatedField<AssetDigestEntry> AssetDigests => _assetDigestResponse?.Digest;

        public RepeatedField<DownloadItemTemplatesResponse.Types.ItemTemplate> ItemTemplates
            => _itemTemplatesResponse?.ItemTemplates;


        public async void SetAssetDigests(GetAssetDigestResponse assetDigestResponse)
        {
            if (_assetDigestResponse == null || assetDigestResponse.TimestampMs > _assetDigestResponse.TimestampMs)
            {
                _assetDigestResponse = assetDigestResponse;
                await _templateStorage.SaveTemplateAsync(assetDigestKey, _assetDigestResponse.ToByteString().ToByteArray());
            }
        }

        public async void SetItemTemplates(DownloadItemTemplatesResponse itemTemplatesResponse)
        {
            if (_itemTemplatesResponse == null || itemTemplatesResponse.TimestampMs > _itemTemplatesResponse.TimestampMs)
            {
                _itemTemplatesResponse = itemTemplatesResponse;
                await _templateStorage.SaveTemplateAsync(itemTemplatesKey, _itemTemplatesResponse.ToByteString().ToByteArray());
            }
        }

        private async Task<GetAssetDigestResponse> LoadAssetDigest()
        {
            if (_templateStorage.TemplateExists(assetDigestKey))
            {
                var bytes = await _templateStorage.LoadTemplateAsync(assetDigestKey);
                if (bytes.Any())
                {
                    return GetAssetDigestResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

        private async Task<DownloadItemTemplatesResponse> LoadItemTemplates()
        {
            if (_templateStorage.TemplateExists(itemTemplatesKey))
            {
                var bytes = await _templateStorage.LoadTemplateAsync(itemTemplatesKey);
                if (bytes.Any())
                {
                    return DownloadItemTemplatesResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }
    }
}