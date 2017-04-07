using System;

namespace POGOLib.Official.Net.Captcha
{
    public class CaptchaEventArgs : EventArgs
    {
        public CaptchaEventArgs(string captchaUrl)
        {
            CaptchaUrl = captchaUrl;
        }

        public string CaptchaUrl { get; }
    }
}
