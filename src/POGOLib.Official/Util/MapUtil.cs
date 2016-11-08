using System.Collections.Generic;
using Google.Common.Geometry;

namespace POGOLib.Official.Util
{
    internal static class MapUtil
    {
        public static ulong[] GetCellIdsForLatLong(double latitude, double longitude)
        {
            var latLong = S2LatLng.FromDegrees(latitude, longitude);
            var cell = S2CellId.FromLatLng(latLong);
            var cellId = cell.ParentForLevel(15);
            var cells = cellId.GetEdgeNeighbors();
            var cellIds = new List<ulong>
            {
                cellId.Id
            };

            foreach (var cellEdge1 in cells)
            {
                if (!cellIds.Contains(cellEdge1.Id)) cellIds.Add(cellEdge1.Id);

                foreach (var cellEdge2 in cellEdge1.GetEdgeNeighbors())
                {
                    if (!cellIds.Contains(cellEdge2.Id)) cellIds.Add(cellEdge2.Id);
                }
            }

            return cellIds.ToArray();
        }
    }
}