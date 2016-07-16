using System;
using System.Collections.Generic;
using Google.Common.Geometry;
using log4net;
using POGOLib.Net;
using POGOLib.Util;

namespace POGOLib
{
    /// <summary>
    /// Used for testing stuff.
    /// </summary>
    public class Test
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Test));

        public static void DoTest()
        {
            Log.Info("Hello.");

            var latitude = 52.369437;
            var longitude = 4.896031;

            var cellIds = MapUtil.GetCellIdsForLatLong(latitude, longitude);

            Log.Info($"Cells: {cellIds.Length}");
            
            foreach (var @ulong in cellIds)
            {
                Console.WriteLine(@ulong);
            }
        }

    }
}
