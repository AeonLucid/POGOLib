using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Google.Protobuf;
using POGOLib.Extensions;
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

        private readonly Stopwatch _stopwatch;

        private readonly ByteString _sessionHash;

        private readonly Random _random;

        private readonly string _deviceId;

        internal RpcEncryption(Session session)
        {
            _session = session;
            _stopwatch = Stopwatch.StartNew();
            _random = new Random();

            var sessionHash = new byte[32];
            _random.NextBytes(sessionHash);

            _sessionHash = ByteString.CopyFrom(sessionHash);
            _deviceId = HexUtil.GetRandomHexNumber(32);
        }

        private long TimestampSinceStartMs => _stopwatch.ElapsedMilliseconds;

        private List<LocationFix> BuildLocationFixes(long timestampSinceStart, RequestEnvelope requestEnvelope)
        {
            var locationFixes = new List<LocationFix>();

            if (requestEnvelope.Requests.Count == 0 || requestEnvelope.Requests[0] == null)
                return locationFixes;

            var providerCount = _random.Next(4, 10);
            for (var i = 0; i < providerCount; i++)
            {
                var timestampSnapshot = timestampSinceStart + (150 * (i + 1) + _random.Next(250 * (i + 1) - 150 * (i + 1)));
                if (timestampSnapshot >= timestampSinceStart)
                {
                    if (locationFixes.Count != 0) break;

                    timestampSnapshot = timestampSinceStart - _random.Next(20, 50);

                    if (timestampSnapshot < 0) timestampSnapshot = 0;
                }

                locationFixes.Insert(0, new LocationFix
                {
                    TimestampSnapshot = (ulong) timestampSnapshot,
                    Latitude = LocationUtil.OffsetLatitudeLongitude(_session.Player.Coordinate.Latitude, _random.Next(100) + 10),
                    Longitude = LocationUtil.OffsetLatitudeLongitude(_session.Player.Coordinate.Longitude, _random.Next(100) + 10),
                    HorizontalAccuracy = (float) _random.NextDouble(5.0, 25.0),
                    VerticalAccuracy = (float) _random.NextDouble(5.0, 25.0),
                    Altitude = (float) _random.NextDouble(10.0, 30.0),
                    Provider = "fused",
                    ProviderStatus = 3,
                    LocationType = 1,
//                    Speed = ?,
                    Course = -1,
//                    Floor = 0
                });
            }

            return locationFixes;
        }

        /// <summary>
        ///     Generates the encrypted signature which is required for the <see cref="RequestEnvelope"/>.
        /// </summary>
        /// <returns>The encrypted <see cref="PlatformRequest"/> (Signature).</returns>
        internal PlatformRequest GenerateSignature(RequestEnvelope requestEnvelope)
        {
            var timestampSinceStart = TimestampSinceStartMs;
            var locationFixes = BuildLocationFixes(timestampSinceStart, requestEnvelope);

            // TODO: Figure out why the map request sometimes fails.
            
            _session.Player.Coordinate.HorizontalAccuracy = locationFixes[0].HorizontalAccuracy;
            _session.Player.Coordinate.VerticalAccuracy = locationFixes[0].VerticalAccuracy;
            
            requestEnvelope.Accuracy = _session.Player.Coordinate.HorizontalAccuracy;
            requestEnvelope.MsSinceLastLocationfix = (long)locationFixes[0].TimestampSnapshot;

            var signature = new Signature
            {
                TimestampSinceStart = (ulong)timestampSinceStart,
                Timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds(),
                SensorInfo =
                {
                    new SensorInfo
                    {
                        TimestampSnapshot = (ulong) (timestampSinceStart + _random.Next(100, 250)),
                        LinearAccelerationX = -0.7 + _random.NextDouble() * 1.4,
                        LinearAccelerationY = -0.7 + _random.NextDouble() * 1.4,
                        LinearAccelerationZ = -0.7 + _random.NextDouble() * 1.4,
                        RotationRateX = 0.7 * _random.NextDouble(),
                        RotationRateY = 0.8 * _random.NextDouble(),
                        RotationRateZ = 0.8 * _random.NextDouble(),
                        AttitudePitch = -1.0 + _random.NextDouble() * 2.0,
                        AttitudeRoll = -1.0 + _random.NextDouble() * 2.0,
                        AttitudeYaw = -1.0 + _random.NextDouble() * 2.0,
                        GravityX = -1.0 + _random.NextDouble() * 2.0,
                        GravityY = -1.0 + _random.NextDouble() * 2.0,
                        GravityZ = -1.0 + _random.NextDouble() * 2.0,
                        MagneticFieldAccuracy = -1,
                        Status = 3
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = _deviceId,
                    AndroidBoardName = string.Empty,
                    AndroidBootloader = string.Empty,
                    DeviceBrand = "Apple",
                    DeviceModel = "iPhone",
                    DeviceModelIdentifier = string.Empty,
                    DeviceModelBoot = "iPhone6,1",
                    HardwareManufacturer = "Apple",
                    HardwareModel = "N51AP",
                    FirmwareBrand = "iPhone OS",
                    FirmwareTags = string.Empty,
                    FirmwareType = "9.3.3",
                    FirmwareFingerprint = string.Empty
                },
                LocationFix = { locationFixes },
                ActivityStatus = new ActivityStatus
                {
                    Stationary = true
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
                    EncryptedSignature = ByteString.CopyFrom(PCrypt.Encrypt(signature.ToByteArray(), (uint)timestampSinceStart))
                }.ToByteString()
            };
            
            return encryptedSignature;
        }

    }
}
