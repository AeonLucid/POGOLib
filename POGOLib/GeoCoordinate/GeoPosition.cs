using System;

namespace GeoCoordinatePortable
{
    /// <summary>
    /// Contains location data of a type specified by the type parameter of the <see cref="GeoPosition{T}"/> class
    /// </summary>
    /// <typeparam name="T">The type of the location data.</typeparam>
    public class GeoPosition<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoPosition{T}"/> class.
        /// </summary>
        public GeoPosition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoPosition{T}"/> class
        /// with a timestamp and position.
        /// </summary>
        /// <param name="timestamp">The time the location data was obtained.</param>
        /// <param name="location">The location data to use to initialize the <see cref="GeoPosition{T}"/> object.</param>
        public GeoPosition(DateTimeOffset timestamp, T location)
        {
            Timestamp = timestamp;
            Location = location;
        }

        /// <summary>
        /// Gets or sets the location data for the <see cref="GeoPosition{T}"/> object.
        /// </summary>
        /// <value>
        /// An object of type T that contains the location data for the <see cref="GeoPosition{T}"/> object.
        /// </value>
        public T Location { get; set; }

        /// <summary>
        /// Gets or sets the time when the location data was obtained.
        /// </summary>
        /// <value>
        /// A <see cref="DateTimeOffset"/> that contains the time the location data was created.
        /// </value>
        public DateTimeOffset Timestamp { get; set; }
    }
}