using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOLib.Util;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using static POGOProtos.Networking.Envelopes.Signature.Types;

namespace POGOLib.Net
{
    internal class RpcEncryption
    {

        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        private readonly Stopwatch _internalStopwatch;

        private readonly bool _is64Bit;

        internal RpcEncryption(Session session)
        {
            _session = session;
            _internalStopwatch = new Stopwatch();
            _is64Bit = Environment.Is64BitOperatingSystem;
        }

        /// <summary>
        ///     Generates the encrypted signature which is required for the <see cref="RequestEnvelope"/>.
        /// </summary>
        /// <param name="requests">The requests of the <see cref="RequestEnvelope"/>.</param>
        /// <returns>The encrypted <see cref="Unknown6"/> (Signature).</returns>
        internal Unknown6 GenerateSignature(RepeatedField<Request> requests)
        {
            var signature = new Signature
            {
                TimestampSinceStart = (ulong) _internalStopwatch.ElapsedMilliseconds,
                Timestamp = (ulong) TimeUtil.GetCurrentTimestampInMilliseconds(),
                SensorInfo = new SensorInfo()
                {
                    AccelNormalizedZ = Randomize(9.8),
                    AccelNormalizedX = Randomize(0.02),
                    AccelNormalizedY = Randomize(0.3),
                    TimestampSnapshot = (ulong) _internalStopwatch.ElapsedMilliseconds - 230,
                    MagnetometerX = Randomize(012271042913198471),
                    MagnetometerY = Randomize(-0.015570580959320068),
                    MagnetometerZ = Randomize(0.010850906372070313),
                    AngleNormalizedX = Randomize(17.950439453125),
                    AngleNormalizedY = Randomize(-23.36273193359375),
                    AngleNormalizedZ = Randomize(-48.8250732421875),
                    AccelRawX = Randomize(-0.0120010357350111),
                    AccelRawY = Randomize(-0.04214850440621376),
                    AccelRawZ = Randomize(0.94571763277053833),
                    GyroscopeRawX = Randomize(7.62939453125e-005),
                    GyroscopeRawY = Randomize(-0.00054931640625),
                    GyroscopeRawZ = Randomize(0.0024566650390625),
                    AccelerometerAxes = 3
                },
                DeviceInfo = new DeviceInfo()
                {
                    DeviceId = _session.Device.DeviceId,
                    AndroidBoardName = _session.Device.AndroidBoardName,
                    AndroidBootloader = _session.Device.AndroidBootloader,
                    DeviceBrand = _session.Device.DeviceBrand,
                    DeviceModel = _session.Device.DeviceModel,
                    DeviceModelIdentifier = _session.Device.DeviceModelIdentifier,
                    DeviceModelBoot = _session.Device.DeviceModelBoot,
                    HardwareManufacturer = _session.Device.HardwareManufacturer,
                    HardwareModel = _session.Device.HardwareModel,
                    FirmwareBrand = _session.Device.FirmwareBrand,
                    FirmwareTags = _session.Device.FirmwareTags,
                    FirmwareType = _session.Device.FirmwareType,
                    FirmwareFingerprint = _session.Device.FirmwareFingerprint
                },
                LocationFix =
                {
                    new LocationFix
                    {
                        Provider = "network",
                        //Unk4 = 120,
                        Latitude = (float)_session.Player.Coordinate.Latitude,
                        Longitude = (float)_session.Player.Coordinate.Longitude,
                        Altitude = (float)_session.Player.Coordinate.Altitude,
                        TimestampSinceStart = (ulong)_internalStopwatch.ElapsedMilliseconds - 200,
                        Floor = 3,
                        LocationType = 1
                    }
                }
            };

            //Compute 10
            var x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_session.AccessToken.AuthTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(32, firstHash);
            var locationBytes = BitConverter.GetBytes(_session.Player.Coordinate.Latitude).Reverse()
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Longitude).Reverse())
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Altitude).Reverse()).ToArray();
            signature.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 20
            x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            signature.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 24
            x = new System.Data.HashFunction.xxHash(64, 0x1B845238);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_session.AccessToken.AuthTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(64, seed);
            foreach (var req in requests)
                signature.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));

            //static for now
            signature.Unknown22 = ByteString.CopyFrom(new byte[16] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F });

            var encryptedSignature = new Unknown6
            {
                RequestType = 6,
                Unknown2 = new Unknown6.Types.Unknown2
                {
                    EncryptedSignature = ByteString.CopyFrom(Encrypt(signature.ToByteArray()))
                }
            };

            return encryptedSignature;
        }

        private double Randomize(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = (num * (1 - randomFactor));
            var randomMax = (num * (1 + randomFactor));
            var randomizedDelay = new Random().NextDouble() * (randomMax - randomMin) + randomMin; ;
            return randomizedDelay; ;
        }

        private byte[] Encrypt(byte[] bytes)
        {
            var outputLength = 32 + bytes.Length + (256 - (bytes.Length % 256));
            var ptr = Marshal.AllocHGlobal(outputLength);
            var ptrOutput = Marshal.AllocHGlobal(outputLength);
            FillMemory(ptr, (uint)outputLength, 0);
            FillMemory(ptrOutput, (uint)outputLength, 0);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            try
            {
                int outputSize = outputLength;

                // Results in "An attempt was made to load a program with an incorrect format."
                //                if(_is64Bit)
                //                    EncryptNative_x64(ptr, bytes.Length, new byte[32], 32, ptrOutput, out outputSize);
                //                else

                EncryptNative_x86(ptr, bytes.Length, new byte[32], 32, ptrOutput, out outputSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var output = new byte[outputLength];
            Marshal.Copy(ptrOutput, output, 0, outputLength);
            return output;
        }

        [DllImport("Libraries/Encrypt_x86.dll", EntryPoint = "encrypt", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void EncryptNative_x86(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);

        [DllImport("Libraries/Encrypt_x64.dll", EntryPoint = "encrypt", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void EncryptNative_x64(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        private static extern void FillMemory(IntPtr destination, uint length, byte fill);

    }
}
