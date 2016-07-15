namespace POGOLib
{
    internal static class Configuration
    {
        public const string ApiUrl = "https://pgorelease.nianticlabs.com/plfe/rpc";
        public const string ApiUserAgent = "Dalvik/2.1.0 (Linux; U; Android 6.0.1; ONEPLUS A3003 Build/MMB29M)";

        public const string LoginUrl = "https://sso.pokemon.com/sso/login?service=https%3A%2F%2Fsso.pokemon.com%2Fsso%2Foauth2.0%2FcallbackAuthorize";
        public const string LoginUserAgent = "Niantic App";
        public const string LoginOauthUrl = "https://sso.pokemon.com/sso/oauth2.0/accessToken";
    }
}
