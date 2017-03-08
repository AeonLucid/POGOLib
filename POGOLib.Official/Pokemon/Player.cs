using System;
using System.Linq;
using System.Device.Location;
using POGOProtos.Data;
using POGOProtos.Data.Player;

namespace POGOLib.Official.Pokemon
{
    public class Player
    {
        internal Player(GeoCoordinate coordinate)
        {
            Coordinate = coordinate;
            Inventory = new Inventory();
            Inventory.Update += InventoryOnUpdate;
        }

        internal GeoCoordinate Coordinate  { get; private set; }

        public double Latitude
        {
            get { return Coordinate.Latitude; }
            set { Coordinate.Latitude = value; }
        }
        public double Longitude
        {
            get { return Coordinate.Longitude; }
            set { Coordinate.Longitude = value; }
        }
        public double Altitude
        {
            get { return Coordinate.Altitude; }
            set { Coordinate.Altitude = value; }
        }

        public Inventory Inventory { get; }

        public PlayerStats Stats { get; private set; }
		
		public PlayerData Data { get; set; }

        public void SetCoordinates(double latitude, double longitude, double altitude = 10d)
        {
            Coordinate = new GeoCoordinate(latitude, longitude, altitude);
        }
 
        public void SetCoordinates(GeoCoordinate coordinate)
        {
            Coordinate = coordinate;
        }

        public double DistanceTo(double latitude, double longitude)
        {
            return Coordinate.GetDistanceTo(new GeoCoordinate(latitude, longitude));
        }

        private void InventoryOnUpdate(object sender, EventArgs eventArgs)
        {
            var stats =
                Inventory.InventoryItems.FirstOrDefault(i => i?.InventoryItemData?.PlayerStats != null)?
                    .InventoryItemData?.PlayerStats;
            if (stats != null)
            {
                Stats = stats;
            }
        }
    }
}