namespace POGOLib.Util.Devices
{
    public class Device
    {

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
        internal string UserAgent { get; set; } = "Dalvik/2.1.0 (Linux; U; Android 5.0; Nexus 5 Build/LPX13D)"; // Device "nexus5".

    }
}
