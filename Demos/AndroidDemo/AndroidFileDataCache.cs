using Google.Protobuf;
using POGOLib.Util;
using Java.IO;
using Android.Content;
using System.Threading.Tasks;

namespace AndroidDemo
{
    public class AndroidFileDataCache : IDataCache
    {
        Context _ctx;

        public AndroidFileDataCache(Context ctx)
        {
            _ctx = ctx;
        }

        public Task<IMessage<T>> GetCachedData<T>(string fileName) where T : IMessage<T>, new()
        {
			return Task.Run(() =>
			{
				File file = new File(_ctx.FilesDir, fileName);
				if (!file.Exists())
				{
					return null;
				}
				var bytes = new byte[file.Length()];
				using (var stream = new FileInputStream(file))
				{
					stream.Read(bytes);
				}
				return this.ParseMessageFromBytes<T>(bytes);
			});
        }

        public void SaveData(string fileName, IMessage msg)
        {
            using (var file = new File(_ctx.FilesDir, fileName))
            using (var stream = new FileOutputStream(file))
            {
                stream.Write(msg.ToByteArray());
            }
        }
    }

}