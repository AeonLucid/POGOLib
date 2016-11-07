using System;
using System.Threading;
using POGOLib.Logging;
using POGOLib.Net;

namespace POGOLib.Pokemon
{
    internal class HeartbeatDispatcher
    {
        
        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        /// <summary>
        ///     Determines whether we can keep heartbeating.
        /// </summary>
        private bool _keepHeartbeating = true;

        internal HeartbeatDispatcher(Session session)
        {
            _session = session;
        }

        /// <summary>
        ///     Checks every second if we need to update.
        /// </summary>
        private void CheckDispatch()
        {
            while (_keepHeartbeating)
            {
                var canRefresh = false;
                if (_session.GlobalSettings != null)
                {
                    var minSeconds = _session.GlobalSettings.MapSettings.GetMapObjectsMinRefreshSeconds;
                    var maxSeconds = _session.GlobalSettings.MapSettings.GetMapObjectsMaxRefreshSeconds;
                    var minDistance = _session.GlobalSettings.MapSettings.GetMapObjectsMinDistanceMeters;
                    var lastGeoCoordinate = _session.RpcClient.LastGeoCoordinateMapObjectsRequest;
                    var secondsSinceLast = DateTime.UtcNow.Subtract(_session.RpcClient.LastRpcRequest).Seconds;

                    if (lastGeoCoordinate.IsUnknown)
                    {
                        Logger.Debug("Refreshing MapObjects, reason: 'lastGeoCoordinate.IsUnknown'.");
                        canRefresh = true;
                    }
                    else if (secondsSinceLast >= minSeconds)
                    {
                        var metersMoved = _session.Player.Coordinate.GetDistanceTo(lastGeoCoordinate);
                        if (secondsSinceLast >= maxSeconds)
                        {
							Logger.Debug("Refreshing MapObjects, reason: 'secondsSinceLast({0}) >= maxSeconds({1})'.", secondsSinceLast.ToString(), maxSeconds.ToString());
                            canRefresh = true;
                        }
                        else if (metersMoved >= minDistance)
                        {
							Logger.Debug("Refreshing MapObjects, reason: 'metersMoved({0}) >= minDistance({1})'.", metersMoved.ToString(), minDistance.ToString());
                            canRefresh = true;
                        }
                    }
                }
                else
                {
                    canRefresh = true;
                }
                if (canRefresh)
                {
                    Dispatch();
                }
                Thread.Sleep(1000);
            }
        }

        internal void StartDispatcher()
        {
            _keepHeartbeating = true;
            new Thread(CheckDispatch)
            {
                IsBackground = true
            }.Start();
        }

        internal void StopDispatcher()
        {
            _keepHeartbeating = false;
        }

        private void Dispatch()
        {
            _session.RpcClient.RefreshMapObjects();
        }
    }
}