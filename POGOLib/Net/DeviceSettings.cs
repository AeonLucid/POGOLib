using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POGOLib.Net
{
    public class DeviceSettings
    {
        public double DefaultLatitude { get; set; }
        public double DefaultLongitude { get; set; }
        public double DefaultAltitude { get; set; }
        public string DeviceId { get; set; }
        public string AndroidBoardName { get; set; }
        public string AndroidBootloader { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceModelIdentifier { get; set; }
        public string DeviceModelBoot { get; set; }
        public string HardwareManufacturer { get; set; }
        public string HardwareModel { get; set; }
        public string FirmwareBrand { get; set; }
        public string FirmwareTags { get; set; }
        public string FirmwareType { get; set; }
        public string FirmwareFingerprint { get; set; }

        public static DeviceSettings FromPresets(string presetName = "lg-optimus-g")
        {
            if (DeviceInfoHelper.DeviceInfoSets.ContainsKey(presetName))
            {
                var deviceSettings = new DeviceSettings();
                deviceSettings.AndroidBoardName = DeviceInfoHelper.DeviceInfoSets[presetName]["AndroidBoardName"];
                deviceSettings.AndroidBootloader = DeviceInfoHelper.DeviceInfoSets[presetName]["AndroidBootloader"];
                deviceSettings.DeviceBrand = DeviceInfoHelper.DeviceInfoSets[presetName]["DeviceBrand"];
                deviceSettings.DeviceId = DeviceInfoHelper.DeviceInfoSets[presetName]["DeviceId"];
                deviceSettings.DeviceModel = DeviceInfoHelper.DeviceInfoSets[presetName]["DeviceModel"];
                deviceSettings.DeviceModelBoot = DeviceInfoHelper.DeviceInfoSets[presetName]["DeviceModelBoot"];
                deviceSettings.DeviceModelIdentifier = DeviceInfoHelper.DeviceInfoSets[presetName]["DeviceModelIdentifier"];
                deviceSettings.FirmwareBrand = DeviceInfoHelper.DeviceInfoSets[presetName]["FirmwareBrand"];
                deviceSettings.FirmwareFingerprint = DeviceInfoHelper.DeviceInfoSets[presetName]["FirmwareFingerprint"];
                deviceSettings.FirmwareTags = DeviceInfoHelper.DeviceInfoSets[presetName]["FirmwareTags"];
                deviceSettings.FirmwareType = DeviceInfoHelper.DeviceInfoSets[presetName]["FirmwareType"];
                deviceSettings.HardwareManufacturer = DeviceInfoHelper.DeviceInfoSets[presetName]["HardwareManufacturer"];
                deviceSettings.HardwareModel = DeviceInfoHelper.DeviceInfoSets[presetName]["HardwareModel"];
                return deviceSettings;
            }
            else
            {
                throw new ArgumentException("Invalid device info provided");
            }
        }
    }
}