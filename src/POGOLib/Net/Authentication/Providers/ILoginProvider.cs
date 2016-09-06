using System.Threading.Tasks;
using POGOLib.Net.Authentication.Data;

namespace POGOLib.Net.Authentication.Providers
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