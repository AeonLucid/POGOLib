using System;
using System.Threading;
using log4net;
using POGOLib.Net;

namespace POGOLib.Pokemon
{
    internal class HeartbeatDispatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HeartbeatDispatcher));

        /// <summary>
        /// The authenticated <see cref="Session"/>.
        /// </summary>
        private readonly Session _session;

        /// <summary>
        /// Determines whether we can keep heartbeating.
        /// </summary>
        private bool _keepHeartbeating = true;
        
        internal HeartbeatDispatcher(Session session)
        {
            _session = session;
        }

        /// <summary>
        /// Checks every second if we need to update.
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
                        if (Configuration.Debug)
                            Log.Debug("Refreshing MapObjects, reason: 'lastGeoCoordinate.IsUnknown'.");

                        canRefresh = true;
                    }
                    else if (secondsSinceLast >= minSeconds)
                    {
                        var metersMoved = _session.Player.Coordinate.GetDistanceTo(lastGeoCoordinate);

                        if (secondsSinceLast >= maxSeconds)
                        {
                            if (Configuration.Debug)
                                Log.Debug("Refreshing MapObjects, reason: 'secondsSinceLast >= maxSeconds'.");

                            canRefresh = true;
                        }
                        else if (metersMoved >= minDistance)
                        {
                            if (Configuration.Debug)
                                Log.Debug("Refreshing MapObjects, reason: 'metersMoved >= maxDistance'.");

                            canRefresh = true;
                        }
                    }
                }
                else
                {
                    canRefresh = true;
                }

                if (canRefresh) Dispatch();

                Thread.Sleep(1000);
            }
        }

        internal void StartDispatcher()
        {
            new Thread(CheckDispatch)
            {
                IsBackground = true
            }.Start();
        }

        private void Dispatch()
        {
            _session.RpcClient.RefreshMapObjects();
        }

    }
}
