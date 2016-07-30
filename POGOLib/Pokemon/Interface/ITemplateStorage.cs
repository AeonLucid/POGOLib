using System.Threading.Tasks;

namespace POGOLib.Pokemon
{
    public interface ITemplateStorage
    {
        Task<byte[]> LoadTemplateAsync(string template);

        Task SaveTemplateAsync(string template, byte[] byteArray);

        bool TemplateExists(string template);
    }
}

