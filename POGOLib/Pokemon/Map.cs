using System;
using Google.Protobuf.Collections;
using POGOProtos.Map;

namespace POGOLib.Pokemon
{
    /// <summary>
    ///     A wrapper class for <see cref="RepeatedField{T}" />.
    /// </summary>
    public class Map
    {
        // The last received map cells.
        private RepeatedField<MapCell> _cells;

        internal Map()
        {
        }

        /// <summary>
        ///     Gets the last received <see cref="RepeatedField{MapCell}" /> from PokémonGo.<br />
        ///     Only use this if you know what you are doing.
        /// </summary>
        public RepeatedField<MapCell> Cells
        {
            get { return _cells; }
            internal set
            {
                _cells = value;
                Update?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> Update;
    }
}