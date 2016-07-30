using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using POGOLib.Pokemon;

namespace Demo
{
    public class FileTemplateStorage : ITemplateStorage
    {
        public Task<byte[]> LoadTemplateAsync(string template)
        {
            var file = GetFilePath(template);
            return Task.Run(() => File.ReadAllBytes(file));
        }

        public Task SaveTemplateAsync(string template, byte[] byteArray)
        {
            var file = GetFilePath(template);
            var directory = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            return Task.Run(() => File.WriteAllBytes(file, byteArray));

        }

        public bool TemplateExists(string template)
        {
            var file = GetFilePath(template);
            return File.Exists(file);
        }

        private string GetFilePath(string template)
        {
            return Path.Combine(Environment.CurrentDirectory,
                    ConfigurationManager.AppSettings["POGOLib.Templates.Directory"] ?? string.Empty,
                                $"templates.{template}.dat");
        }
    }
}

