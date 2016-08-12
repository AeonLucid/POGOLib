using POGOLib.Net.Authentication.Data;
using System.Threading.Tasks;

namespace POGOLib.Pokemon
{
    public interface ILoginProvider
    {
        string ProviderID { get; }
        Task<AccessToken> GetAccessToken(string username, string password);
    }
}