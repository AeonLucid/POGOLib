using System;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
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

        private readonly ByteString _sessionHash;

        private readonly Random _random;

        internal RpcEncryption(Session session)
        {
            _session = session;
            _internalStopwatch = Stopwatch.StartNew();
            _random = new Random();

            var sessionHash = new byte[32];
            _random.NextBytes(sessionHash);

            _sessionHash = ByteString.CopyFrom(sessionHash);
        }

        /// <summary>
        ///     Generates the encrypted signature which is required for the <see cref="RequestEnvelope"/>.
        /// </summary>
        /// <returns>The encrypted <see cref="PlatformRequest"/> (Signature).</returns>
        internal PlatformRequest GenerateSignature(RequestEnvelope requestEnvelope)
        {
            // TODO: Figure out why the map request sometimes fails, probably has to do with the Randomize() method.

            _session.Player.Coordinate.HorizontalAccuracy = 10.0; // TODO: TEMP FIX, figure out why only this returns map data.

            var signature = new Signature
            {
                TimestampSinceStart = (ulong) _internalStopwatch.ElapsedMilliseconds,
                Timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds(),
                SensorInfo = new SensorInfo
                {
                    TimestampSnapshot = (ulong) (_internalStopwatch.ElapsedMilliseconds - _random.Next(100, 250)),
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
                DeviceInfo = new DeviceInfo
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
                        TimestampSnapshot = (ulong) (_internalStopwatch.ElapsedMilliseconds - _random.Next(100, 250)), // TODO: Verify this
                        Floor = 0,
                        LocationType = 1
                    }
                }
            };

            var serializedTicket = requestEnvelope.AuthTicket != null ? requestEnvelope.AuthTicket.ToByteArray() : requestEnvelope.AuthInfo.ToByteArray();
            var locationBytes = BitConverter.GetBytes(_session.Player.Coordinate.Latitude).Reverse()
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Longitude).Reverse())
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.HorizontalAccuracy).Reverse()).ToArray();

            signature.LocationHash1 = NiaHash.Hash32Salt(locationBytes, NiaHash.Hash32(serializedTicket));
            signature.LocationHash2 = NiaHash.Hash32(locationBytes);

            foreach (var req in requestEnvelope.Requests)
            {
                signature.RequestHash.Add(NiaHash.Hash64Salt64(req.ToByteArray(), NiaHash.Hash64(serializedTicket)));
            }

            signature.SessionHash = _sessionHash;
            signature.Unknown25 = -8408506833887075802;

            var encryptedSignature = new PlatformRequest
            {
                Type = PlatformRequestType.SendEncryptedSignature,
                RequestMessage = new SendEncryptedSignatureRequest
                {
                    EncryptedSignature = ByteString.CopyFrom(PCrypt.Encrypt(signature.ToByteArray(), (uint) _internalStopwatch.ElapsedMilliseconds))
                }.ToByteString()
            };
            
            return encryptedSignature;
        }

        private static double Randomize(double num)
        {
            const float randomFactor = 0.3f;
            var randomMin = num * (1 - randomFactor);
            var randomMax = num * (1 + randomFactor);
            var randomizedDelay = new Random().NextDouble() * (randomMax - randomMin) + randomMin; 
            return randomizedDelay;
        }

    }
}
