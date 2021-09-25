using System;
using TrueCraft.API.Logic;
using TrueCraft.API.Networking;

namespace TrueCraft.API.Windows
{
    /// <summary>
    /// An enum to specify types of child windows.
    /// </summary>
    /// <remarks>These values are the ones used in the Type field of the OpenWindowPacket.</remarks>
    public enum WindowType : sbyte
    {
        Inventory = -1,
        Chest = 0,
        CraftingBench = 1,
        Furnace = 2,
        Dispenser = 3
    }

    public interface IWindowContent : IDisposable, IEventSubject
    {
        IRemoteClient Client { get; set; }

        sbyte ID { get; set; }
        string Name { get; }

        /// <summary>
        /// Gets the type of the Window for which this stores the content.
        /// </summary>
        WindowType Type { get; }

        int Length { get; }
        int Length2 { get; }
        ItemStack this[int index] { get; set; }

        /// <summary>
        /// Gets whether or not the given Slot Index is an output Slot.
        /// Nothing can be placed in an Output Slot; only removed.
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        bool IsOutputSlot(int slotIndex);

        /// <summary>
        /// Gets a reference to the collection of slots that contaings the
        /// Player's main inventory.
        /// </summary>
        ISlots MainInventory { get; }

        /// <summary>
        /// Gets the Slot Index within the overall Window Content where the
        /// Main Inventory starts.
        /// </summary>
        int MainInventoryIndex { get => throw new NotImplementedException(); }

        /// <summary>
        /// Gets the slots of the Player's Hotbar.
        /// </summary>
        ISlots Hotbar { get; }

        /// <summary>
        /// Gets the Slot Index within the overall Window Content where the
        /// Hotbar starts.
        /// </summary>
        int HotbarIndex { get => throw new NotImplementedException(); }

        /// <summary>
        /// Gets whether or not the given index represents a slot within the Main Inventory
        /// or the Hotbar.
        /// </summary>
        /// <param name="slotIndex">The Slot Index to check.</param>
        /// <returns>True if the given Slot Index is part of either the Main Inventory
        /// or the Hotbar.</returns>
        bool IsPlayerInventorySlot(int slotIndex);

        /// <summary>
        /// Gets an array of all slots in this window. Suitable for sending to clients over the network.
        /// </summary>
        ItemStack[] GetSlots();

        void SetSlots(ItemStack[] slots);

        /// <summary>
        /// Moves as many as possible of the specified ItemStack to another area of the Window Content.
        /// </summary>
        /// <param name="index">The index within the overall Window Content from
        /// which we are to move the ItemStack</param>
        /// <returns>
        /// An ItemStack containing any leftover items that would not fit.
        /// If all given Items fit in these Slots, then ItemStack.EmptyStack will
        /// be returned.
        /// </returns>
        ItemStack MoveItemStack(int index);
    }
}
