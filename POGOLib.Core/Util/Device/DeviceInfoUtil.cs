using System;
using POGOLib.Official.Extensions;
using static POGOProtos.Networking.Envelopes.Signature.Types;

namespace POGOLib.Official.Util.Device
{
    public class DeviceInfoUtil
    {
        private static readonly Random Random = new Random();

        private static readonly string[][] Devices =
        {
            // maybe not support 
            /*new[] {"iPad3,1", "iPad", "J1AP"},
            new[] {"iPad3,2", "iPad", "J2AP"},
            new[] {"iPad3,3", "iPad", "J2AAP"},
            new[] {"iPad3,4", "iPad", "P101AP"},
            new[] {"iPad3,5", "iPad", "P102AP"},
            new[] {"iPad3,6", "iPad", "P103AP"},
            new[] {"iPad4,1", "iPad", "J71AP"},
            new[] {"iPad4,2", "iPad", "J72AP"},
            new[] {"iPad4,3", "iPad", "J73AP"},
            new[] {"iPad4,4", "iPad", "J85AP"},
            new[] {"iPad4,5", "iPad", "J86AP"},
            new[] {"iPad4,6", "iPad", "J87AP"},
            new[] {"iPad4,7", "iPad", "J85mAP"},
            new[] {"iPad4,8", "iPad", "J86mAP"},
            new[] {"iPad4,9", "iPad", "J87mAP"},*/
            new[] {"iPad5,1", "iPad", "J96AP"},
            new[] {"iPad5,2", "iPad", "J97AP"},
            new[] {"iPad5,3", "iPad", "J81AP"},
            new[] {"iPad5,4", "iPad", "J82AP"},
            new[] {"iPad6,7", "iPad", "J98aAP"},
            new[] {"iPad6,8", "iPad", "J99aAP"},
            new[] {"iPhone5,1", "iPhone", "N41AP"},
            new[] {"iPhone5,2", "iPhone", "N42AP"},
            new[] {"iPhone5,3", "iPhone", "N48AP"},
            new[] {"iPhone5,4", "iPhone", "N49AP"},
            new[] {"iPhone6,1", "iPhone", "N51AP"},
            new[] {"iPhone6,2", "iPhone", "N53AP"},
            new[] {"iPhone7,1", "iPhone", "N56AP"},
            new[] {"iPhone7,2", "iPhone", "N61AP"},
            new[] {"iPhone8,1", "iPhone", "N71AP"},
            new[] {"iPhone8,2", "iPhone", "MKTM2"}, //iphone 6s plus
            new[] {"iPhone9,3", "iPhone", "MN9T2"}
        };

        private static readonly string[] OsVersions = {
            /*
             * not more supported
            "8.1.1",
            "8.1.2",
            "8.1.3",
            "8.2",
            "8.3",
            "8.4",
            "8.4.1",*/
            "9.0",
            "9.0.1",
            "9.0.2",
            "9.1",
            "9.2",
            "9.2.1",
            "9.3",
//            "9.3.1",
            "9.3.2",
//            "9.3.3",
//            "9.3.4"
            "10.3.3",
            "11.1.0",
            "11.2.0"
        };

        private static readonly string[] OsUserAgentParts = {
            /*
             * not more supportd
            "CFNetwork/711.1.16 Darwin/14.0.0", // 8.1.1
            "CFNetwork/711.1.16 Darwin/14.0.0", // 8.1.2
            "CFNetwork/711.1.16	Darwin/14.0.0", // 8.1.3
            "CFNetwork/711.2.23 Darwin/14.0.0", // 8.2
            "CFNetwork/711.3.18 Darwin/14.0.0", // 8.3
            "CFNetwork/711.4.6 Darwin/14.4.0",  // 8.4
            "CFNetwork/711.4.6 Darwin/14.4.0",  // 8.4.1*/
            "CFNetwork/758.0.2 Darwin/15.0.0",  // 9.0
            "CFNetwork/758.0.2 Darwin/15.0.0",  // 9.0.1
            "CFNetwork/758.0.2 Darwin/15.0.0",  // 9.0.2
            "CFNetwork/758.1.6 Darwin/15.0.0",  // 9.1
            "CFNetwork/758.2.8 Darwin/15.0.0",  // 9.2
            "CFNetwork/758.2.8 Darwin/15.0.0",  // 9.2.1
            "CFNetwork/758.3.15 Darwin/15.4.0", // 9.3
//            "9.3.1", // 9.3.1
            "CFNetwork/758.4.3 Darwin/15.5.0", // 9.3.2
//            "9.3.3", // 9.3.3
//            "9.3.4"  // 9.3.4
            "CFNetwork/807.2.14 Darwin/16.3.0", // 10.3.3
            "CFNetwork/889.3 Darwin/17.2.0", // 11.1.0
            "CFNetwork/893.10 Darwin/17.3.0", // 11.2.0
        };

        public static DeviceWrapper GetRandomDevice()
        {
            var device = Devices[Random.Next(Devices.Length)];

            var osId = Random.Next(OsVersions.Length);
            var firmwareType = OsVersions[osId];
            var firmwareUserAgentPart = OsUserAgentParts[osId];

            return new DeviceWrapper
            {
                UserAgent = $"pokemongo/1 {firmwareUserAgentPart}",
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = Random.NextHexNumber(32).ToLower(),
                    DeviceBrand = "Apple",
                    DeviceModelBoot = device[0],
                    DeviceModel = device[1],
                    HardwareModel = device[2],
                    HardwareManufacturer = "Apple",
                    FirmwareBrand = "iPhone OS",
                    FirmwareType = firmwareType,
                }
            };
        }
    }
}
