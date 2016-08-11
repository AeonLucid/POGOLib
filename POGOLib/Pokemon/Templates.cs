using System;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOProtos.Data;
using POGOProtos.Networking.Responses;
using PCLStorage;

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

        public string AssetDigestFile => Path.Combine(FileSystem.Current.LocalStorage.Path, "templates.asset-digests.dat");

        public string ItemTemplatesFile => Path.Combine(FileSystem.Current.LocalStorage.Path, "templates.items.dat");

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
            if (FileSystem.Current.LocalStorage.CheckExistsAsync(AssetDigestFile).Result == ExistenceCheckResult.FileExists)
            {
                var file = FileSystem.Current.LocalStorage.GetFileAsync(AssetDigestFile).Result.OpenAsync(FileAccess.Read).Result;
                var bytes = new BinaryReader(file).ReadBytes((int)file.Length);
                if (bytes.Any())
                {
                    return GetAssetDigestResponse.Parser.ParseFrom(bytes);
                }
            }
            return null;
        }

        private DownloadItemTemplatesResponse LoadItemTemplates()
        {
            if (FileSystem.Current.LocalStorage.CheckExistsAsync(ItemTemplatesFile).Result == ExistenceCheckResult.FileExists)
            {
                var file = FileSystem.Current.LocalStorage.GetFileAsync(ItemTemplatesFile).Result.OpenAsync(FileAccess.Read).Result;
                var bytes = new BinaryReader(file).ReadBytes((int)file.Length);
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
                if (FileSystem.Current.LocalStorage.CheckExistsAsync(directory).Result == ExistenceCheckResult.NotFound)
                {
                    FileSystem.Current.LocalStorage.CreateFolderAsync(directory, CreationCollisionOption.OpenIfExists).Wait();
                }
            }
            var fileOpen = FileSystem.Current.LocalStorage.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting).Result;
            fileOpen.OpenAsync(FileAccess.ReadAndWrite).Result.Write(data, 0, data.Length);
        }
    }
}