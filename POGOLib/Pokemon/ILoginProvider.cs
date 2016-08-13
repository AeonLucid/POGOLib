using POGOLib.Net.Authentication.Data;
using System.Threading.Tasks;

namespace POGOLib.Pokemon
{
    public interface ILoginProvider
    {
        /// <summary>
        /// Only used for informational purposes. Could be an email, username, etc
        /// </summary>
        string UserID { get; }
        string ProviderID { get; }
        Task<AccessToken> GetAccessToken();
    }
}