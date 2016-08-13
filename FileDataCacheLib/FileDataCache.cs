using Google.Protobuf;
using POGOLib.Util;
using System.IO;
using System.Threading.Tasks;
using PCLStorage;

namespace FileDataCacheLib
{
	public class FileDataCache : IDataCache
	{
		public async Task<IMessage<T>> GetCachedData<T>(string fileName) where T : IMessage<T>, new()
		{
			var path = Path.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, fileName);
			if (await PCLStorage.FileSystem.Current.LocalStorage.CheckExistsAsync(path) == PCLStorage.ExistenceCheckResult.FileExists)
			{
				return new MessageParser<T>(() => new T()).ParseFrom(await (await FileSystem.Current.LocalStorage.GetFileAsync(path)).OpenAsync(PCLStorage.FileAccess.Read));
			}
			return null;
		}

		public async void SaveData(string fileName, IMessage msg)
		{
			var path = Path.Combine(FileSystem.Current.LocalStorage.Path, fileName);
			var file = await FileSystem.Current.LocalStorage.CreateFileAsync(path, CreationCollisionOption.ReplaceExisting);
			var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite);
			msg.WriteTo(stream);
		}
	}
}
