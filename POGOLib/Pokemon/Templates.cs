using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOProtos.Data;
using POGOProtos.Networking.Responses;

namespace POGOLib.Pokemon
{
    public class Templates
    {
        private GetAssetDigestResponse _assetDigestResponse;
        private DownloadItemTemplatesResponse _itemTemplatesResponse;

        public Templates()
        {
            _assetDigestResponse = LoadAssetDigest();
            _itemTemplatesResponse = LoadItemTemplates();
        }

        public RepeatedField<AssetDigestEntry> AssetDigests => _assetDigestResponse?.Digest;

        public RepeatedField<DownloadItemTemplatesResponse.Types.ItemTemplate> ItemTemplates
            => _itemTemplatesResponse?.ItemTemplates;

        public string AssetDigestFile
            =>
                Path.Combine(Environment.CurrentDirectory,
                    ConfigurationManager.AppSettings["POGOLib.Templates.Directory"] ?? string.Empty,
                    "templates.asset-digests.dat");

        public string ItemTemplatesFile
            =>
                Path.Combine(Environment.CurrentDirectory,
                    ConfigurationManager.AppSettings["POGOLib.Templates.Directory"] ?? string.Empty,
                    "templates.items.dat");

        public void SetAssetDigests(GetAssetDigestResponse assetDigestResponse)
        {
            if (_assetDigestResponse == null || assetDigestResponse.TimestampMs > _assetDigestResponse.TimestampMs)
            {
                _assetDigestResponse = assetDigestResponse;
                SaveTemplate(AssetDigestFile, _assetDigestResponse.ToByteString().ToByteArray());
            }
        }

        public void SetItemTemplates(DownloadItemTemplatesResponse itemTemplatesResponse)
        {
            if (_itemTemplatesResponse == null || itemTemplatesResponse.TimestampMs > _itemTemplatesResponse.TimestampMs)
            {
                _itemTemplatesResponse = itemTemplatesResponse;
                SaveTemplate(ItemTemplatesFile, _itemTemplatesResponse.ToByteString().ToByteArray());
            }
        }

        private GetAssetDigestResponse LoadAssetDigest()
        {
            if (File.Exists(AssetDigestFile))
            {
                var bytes = File.ReadAllBytes(AssetDigestFile);
                if (bytes.Any())
                {
                    return GetAssetDigestResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

        private DownloadItemTemplatesResponse LoadItemTemplates()
        {
            if (File.Exists(ItemTemplatesFile))
            {
                var bytes = File.ReadAllBytes(ItemTemplatesFile);
                if (bytes.Any())
                {
                    return DownloadItemTemplatesResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

        private void SaveTemplate(string file, byte[] data)
        {
            var directory = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            File.WriteAllBytes(file, data);
        }
    }
}