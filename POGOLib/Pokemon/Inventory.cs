using System;
using Google.Protobuf.Collections;
using POGOProtos.Inventory;

namespace POGOLib.Pokemon
{
    /// <summary>
    /// A wrapper class for <see cref="Inventory"/> with helper methods.
    /// </summary>
    public class Inventory
    {       
        /// <summary>
        /// The <see cref="Player"/> of the <see cref="Inventory"/>.
        /// </summary>
        private readonly Player _player;

        /// <summary>
        /// Holds the last received <see cref="RepeatedField{InventoryItem}"/> from PokémonGo.
        /// </summary>
        private RepeatedField<InventoryItem> _inventoryItems;

        internal Inventory(Player player)
        {
            _player = player;
        }

        internal long LastInventoryTimestampMs;

        /// <summary>
        /// Gets the last received <see cref="RepeatedField{InventoryItem}"/> from PokémonGo.<br/>
        /// Only use this if you know what you are doing.
        /// </summary>
        public RepeatedField<InventoryItem> InventoryItems
        {
            get { return _inventoryItems; }
            internal set {
                _inventoryItems = value;
                _player.Inventory.OnUpdate();
            }
        }

        /// <summary>
        /// Base maximum amount of items a player can hold (can be extended)
        /// </summary>
        public int BaseBagItems { get; internal set; }

        /// <summary>
        /// Base maximum amount of pokemon a player can hold (can be extended)
        /// </summary>
        public int BasePokemon { get; internal set; }

        internal void OnUpdate()
        {
            Update?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> Update;

    }
}
