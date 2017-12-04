using System;

namespace POGOLib.Official
{
    public static class Constants
    {
        // API stuff

        public const string ApiUrl = "https://pgorelease.nianticlabs.com/plfe/rpc";
        public const string ApiUserAgent = "Niantic App";

        public const string VersionUrl = "https://pgorelease.nianticlabs.com/plfe/version";

        // Login stuff

        public const string LoginUrl = "https://sso.pokemon.com/sso/login?service=https%3A%2F%2Fsso.pokemon.com%2Fsso%2Foauth2.0%2FcallbackAuthorize";
        public const string LoginUserAgent = "pokemongo/1 CFNetwork/893.10 Darwin/17.3.0"; // iOS 11.2
        public const string LoginOauthUrl = "https://sso.pokemon.com/sso/oauth2.0/accessToken";

        public const string GoogleAuthService = "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com";
        public const string GoogleAuthApp = "com.nianticlabs.pokemongo";
        public const string GoogleAuthClientSig = "321187995bc7cdc2b5fc91b11a96e2baa8602c62";
        public const string Accept = "*/*";
        public const string LoginHostValue = "sso.pokemon.com";
        public const string Connection = "keep-alive";
        public const string AcceptLanguage = "en-US";
        public const string AcceptEncoding = "gzip-deflate";
        public const string LoginManufactor = "X-Unity-Version";
        public const string LoginManufactorVersion = "2017.1.2f1"; //"5.5.1f1";//"5.6.1f1";
        public static TimeSpan TimeOut = new TimeSpan(0, 10, 0);

    }
}