using System;

namespace POGOLib.Util.Devices
{
    public class Device
    {
        private string _userAgent;
        private const string DefaultUserAgent = "Dalvik/2.1.0 (Linux; U; Android 5.0; Nexus 5 Build/LPX13D)";
        private const string UserAgentFormat = "Dalvik/{0} (Linux; U; Android {1}; {2} Build/{3})";

        internal string AndroidBoardName { get; set; }
        internal string AndroidBootloader { get; set; }
        internal string DeviceBrand { get; set; }
        internal string DeviceId { get; set; }
        internal string DeviceModel { get; set; }
        internal string DeviceModelBoot { get; set; }
        internal string DeviceModelIdentifier { get; set; }
        internal string FirmwareBrand { get; set; }
        internal string FirmwareFingerprint { get; set; }
        internal string FirmwareTags { get; set; }
        internal string FirmwareType { get; set; }
        internal string HardwareManufacturer { get; set; }
        internal string HardwareModel { get; set; }

        internal string UserAgent
        {
            get
            {
                if (_userAgent == null)
                {
                    if (TryParseUserAgent(out _userAgent))
                    {
                        return _userAgent;
                    }
                    _userAgent = DefaultUserAgent;
                }
                return _userAgent;
            }
            set { _userAgent = value; }
        }

        private bool TryParseUserAgent(out string userAgent)
        {
            userAgent = null;
            if (string.IsNullOrWhiteSpace(FirmwareFingerprint))
            {
                return false;
            }
            var fingerPrintParts = FirmwareFingerprint.Split('/');
            if (fingerPrintParts.Length < 4)
            {
                return false;
            }
            if (!fingerPrintParts[2].Contains(":"))
            {
                return false;
            }
            try
            {
                string jvmVersion;
                var androidVersion = fingerPrintParts[2].Split(':')[1];
                if (androidVersion.StartsWith("2.1"))
                {
                    jvmVersion = "1.1.0";
                }
                else if (androidVersion.StartsWith("2.2"))
                {
                    jvmVersion = "1.2.0";
                }
                else if (androidVersion.StartsWith("2.3"))
                {
                    jvmVersion = "1.4.0";
                }
                else if (androidVersion.StartsWith("4"))
                {
                    jvmVersion = "1.6.0";
                }
                else
                {
                    jvmVersion = "2.1.0";
                }

                var build = fingerPrintParts[3];
                userAgent = string.Format(UserAgentFormat, jvmVersion, androidVersion, DeviceModel, build);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
