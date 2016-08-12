using System;
using System.Threading;
using POGOLib.Logging;
using POGOLib.Net;
using System.Threading.Tasks;

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
        private CancellationTokenSource _heartbeatCancellation;

        private Task _heartbeatTask;

        internal HeartbeatDispatcher(Session session)
        {
            _session = session;
        }

        /// <summary>
        ///     Checks every second if we need to update.
        /// </summary>
        private async Task CheckDispatch()
        {
            while (!_heartbeatCancellation.IsCancellationRequested)
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
                            Logger.Debug($"Refreshing MapObjects, reason: 'secondsSinceLast({secondsSinceLast}) >= maxSeconds({maxSeconds})'.");
                            canRefresh = true;
                        }
                        else if (metersMoved >= minDistance)
                        {
                            Logger.Debug($"Refreshing MapObjects, reason: 'metersMoved({metersMoved}) >= minDistance({minDistance})'.");
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
                    await Dispatch();
                }
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), _heartbeatCancellation.Token);
                }
                // cancelled
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        internal void StartDispatcher()
        {
            if (_heartbeatTask != null)
            {
                throw new Exception("Heartbeat task already running");
            }
            _heartbeatCancellation = new CancellationTokenSource();
            _heartbeatTask = CheckDispatch();
        }

        internal void StopDispatcher()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatTask = null;
        }

        private async Task Dispatch()
        {
            await _session.RpcClient.RefreshMapObjects();
        }
    }
}