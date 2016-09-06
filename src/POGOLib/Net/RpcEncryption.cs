using System;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
using PokemonGoEncryptSharp;
using POGOLib.Util;
using POGOLib.Util.Encryption;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Platform;
using POGOProtos.Networking.Platform.Requests;
using static POGOProtos.Networking.Envelopes.Signature.Types;
using static POGOProtos.Networking.Envelopes.RequestEnvelope.Types;

namespace POGOLib.Net
{
    internal class RpcEncryption
    {

        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        private readonly Stopwatch _internalStopwatch;

        private readonly Random _random;

        private readonly ByteString _sessionHash;

        internal RpcEncryption(Session session)
        {
            _session = session;
            _internalStopwatch = new Stopwatch();
            _random = new Random();
            var sessionHash = new byte[16];
            _random.NextBytes(sessionHash);
            _sessionHash = ByteString.CopyFrom(sessionHash);
        }

        /// <summary>
        ///     Generates the encrypted signature which is required for the <see cref="RequestEnvelope"/>.
        /// </summary>
        /// <param name="requests">The requests of the <see cref="RequestEnvelope"/>.</param>
        /// <returns>The encrypted <see cref="Unknown6"/> (Signature).</returns>
        internal PlatformRequest GenerateSignature(RequestEnvelope requestEnvelope)
        {
            var signature = new Signature
            {
                TimestampSinceStart = (ulong)_internalStopwatch.ElapsedMilliseconds,
                Timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds(),
                SensorInfo = new SensorInfo()
                {
                    TimestampSnapshot = (ulong)_internalStopwatch.ElapsedMilliseconds - 230,
                    LinearAccelerationX = Randomize(012271042913198471),
                    LinearAccelerationY = Randomize(-0.015570580959320068),
                    LinearAccelerationZ = Randomize(0.010850906372070313),
                    MagneticFieldX = Randomize(17.950439453125),
                    MagneticFieldY = Randomize(-23.36273193359375),
                    MagneticFieldZ = Randomize(-48.8250732421875),
                    RotationVectorX = Randomize(-0.0120010357350111),
                    RotationVectorY = Randomize(-0.04214850440621376),
                    RotationVectorZ = Randomize(0.94571763277053833),
                    GyroscopeRawX = Randomize(7.62939453125e-005),
                    GyroscopeRawY = Randomize(-0.00054931640625),
                    GyroscopeRawZ = Randomize(0.0024566650390625),
                    GravityX = Randomize(0.02),
                    GravityY = Randomize(0.3),
                    GravityZ = Randomize(9.8),
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
                        Latitude = (float)_session.Player.Coordinate.Latitude,
                        Longitude = (float)_session.Player.Coordinate.Longitude,
                        Altitude = (float)_session.Player.Coordinate.Altitude,
                        HorizontalAccuracy = (float)_session.Player.Coordinate.HorizontalAccuracy,
                        VerticalAccuracy = (float)_session.Player.Coordinate.VerticalAccuracy,
                        Speed = (float)_session.Player.Coordinate.Speed,
                        Course = (float)_session.Player.Coordinate.Course,
                        TimestampSnapshot = (ulong)_internalStopwatch.ElapsedMilliseconds - 200, // TODO: Verify this
                        Floor = 3,
                        LocationType = 1
                    }
                }
            };


            const uint seed035 = 0x61656632;
            const long unknown25_035 = 7363665268261373700;
            // Compute 10
            var serializedTicket = requestEnvelope.AuthTicket != null ? requestEnvelope.AuthTicket.ToByteArray() : requestEnvelope.AuthInfo.ToByteArray();
            var firstHash = CalculateHash32(serializedTicket, seed035);
            var locationBytes = BitConverter.GetBytes(_session.Player.Coordinate.Latitude).Reverse()
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Longitude).Reverse())
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.HorizontalAccuracy).Reverse()).ToArray();
            signature.LocationHash1 = CalculateHash32(locationBytes, firstHash);
            // Compute 20
            signature.LocationHash2 = CalculateHash32(locationBytes, seed035);
            // Compute 24
            var seed = xxHash64.CalculateHash(serializedTicket, serializedTicket.Length, seed035);
            foreach (var req in requestEnvelope.Requests)
            {
                var reqBytes = req.ToByteArray();
                signature.RequestHash.Add(xxHash64.CalculateHash(reqBytes, reqBytes.Length, seed));
            }

            //static for now
            //signature.SessionHash = ByteString.CopyFrom(0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F);
            signature.SessionHash = _sessionHash;
            signature.Unknown25 = unknown25_035;
            var iv = new byte[32];
            _random.NextBytes(iv);

            var encryptedSignature = new PlatformRequest()
            {
                Type = PlatformRequestType.SendEncryptedSignature,
                RequestMessage = new SendEncryptedSignatureRequest
                {
                    EncryptedSignature = ByteString.CopyFrom(PogoEncryptUtil.Encrypt(signature.ToByteArray(), iv))
                }.ToByteString()
            };

            return encryptedSignature;
        }

        private uint CalculateHash32(byte[] bytes, uint seed)
        {
            var xxHash = new xxHash32();
            xxHash.Init(seed);
            xxHash.Update(bytes, bytes.Length);
            return xxHash.Digest();
        }

        private double Randomize(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = (num * (1 - randomFactor));
            var randomMax = (num * (1 + randomFactor));
            var randomizedDelay = new Random().NextDouble() * (randomMax - randomMin) + randomMin; ;
            return randomizedDelay; ;
        }

    }
}
