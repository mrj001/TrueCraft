using System;
using System.Collections.Generic;
using fNbt;
using TrueCraft.Core;
using TrueCraft.Core.Inventory;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Inventory
{
    public class FurnaceWindow : Window<IServerSlot>, IFurnaceWindow<IServerSlot>,
        IServerWindow
    {
        // NOTE: these values must match the order in which the slots
        //    collections are added in the constructors.
        public enum AreaIndices
        {
            Ingredient = 0,
            Fuel = 1,
            Output = 2,
            Main = 3,
            Hotbar = 4
        }

        private readonly IServerServiceLocator _serviceLocator;
        private readonly IDimension _dimension;
        private readonly GlobalVoxelCoordinates _location;

        private const int _outputSlotIndex = 2;
        public FurnaceWindow(IServiceLocator serviceLocator,
            ISlotFactory<IServerSlot> slotFactory, sbyte windowID, IFurnaceSlots furnaceSlots,
            ISlots<IServerSlot> mainInventory,
            ISlots<IServerSlot> hotBar, IDimension dimension, GlobalVoxelCoordinates location) :
            base(serviceLocator.ItemRepository, windowID, Core.Windows.WindowType.Furnace, "Furnace",
                new ISlots<IServerSlot>[] { new ServerSlots(serviceLocator.ItemRepository, new List<IServerSlot> {furnaceSlots.IngredientSlot }),
                    new ServerSlots(serviceLocator.ItemRepository, new List<IServerSlot> { furnaceSlots.FuelSlot }),
                    new ServerSlots(serviceLocator.ItemRepository, new List<IServerSlot> {furnaceSlots.OutputSlot }),
                    mainInventory, hotBar })
        {
            _serviceLocator = (IServerServiceLocator)serviceLocator;
            _dimension = dimension;
            _location = location;

            int slotIndex = 0;
            IngredientSlotIndex = slotIndex;
            slotIndex += Ingredient.Count;
            FuelSlotIndex = slotIndex;
            slotIndex += Fuel.Count;
            OutputSlotIndex = slotIndex;
        }

        public ISlots<IServerSlot> Ingredient => Slots[(int)AreaIndices.Ingredient];

        /// <inheritdoc />
        public int IngredientSlotIndex { get; }

        public ISlots<IServerSlot> Fuel => Slots[(int)AreaIndices.Fuel];

        /// <inheritdoc />
        public int FuelSlotIndex { get; }

        public ISlots<IServerSlot> Output => Slots[(int)AreaIndices.Output];

        /// <inheritdoc />
        public int OutputSlotIndex { get; }

        public override bool IsOutputSlot(int slotIndex)
        {
            return slotIndex == _outputSlotIndex;
        }

        /// <inheritdoc />
        public CloseWindowPacket GetCloseWindowPacket()
        {
            return new CloseWindowPacket(WindowID);
        }

        public List<SetSlotPacket> GetDirtySetSlotPackets()
        {
            int offset = 9;  // TODO hard-coded constant.  This is the offset within the Inventory Window of the Main Inventory.
            List<SetSlotPacket> packets = ((IServerSlots)MainInventory).GetSetSlotPackets(0, (short)offset);
            offset += MainInventory.Count;

            packets.AddRange(((IServerSlots)Hotbar).GetSetSlotPackets(0, (short)offset));

            return packets;
        }

        public OpenWindowPacket GetOpenWindowPacket()
        {
            int len = Count - MainInventory.Count - Hotbar.Count;
            return new OpenWindowPacket(WindowID, Type, Name, (sbyte)len);
        }

        public WindowItemsPacket GetWindowItemsPacket()
        {
            return new WindowItemsPacket(WindowID, AllItems());
        }

        public override void SetSlots(ItemStack[] slotContents)
        {
#if DEBUG
            if (slotContents.Length != Count)
                throw new ApplicationException($"{nameof(slotContents)}.Length has value of {slotContents.Length}, but {Count} was expected.");
#endif
            int index = 0;
            for (int j = 0, jul = Slots.Length; j < jul; j++)
                for (int k = 0, kul = Slots[j].Count; k < kul; k++)
                {
                    Slots[j][k].Item = slotContents[index];
                    Slots[j][k].SetClean();
                    index++;
                }
        }

        public void HandleClick(IRemoteClient client, ClickWindowPacket packet)
        {
            int slotIndex = packet.SlotIndex;
            ItemStack itemStaging = client.ItemStaging;
            bool handled;

            if (packet.RightClick)
            {
                if (packet.Shift)
                    handled = HandleShiftRightClick(slotIndex, ref itemStaging);
                else
                    handled = HandleRightClick(slotIndex, ref itemStaging);
            }
            else
            {
                if (packet.Shift)
                    handled = HandleShiftLeftClick(slotIndex, ref itemStaging);
                else
                    handled = HandleLeftClick(slotIndex, ref itemStaging);
            }

            if (handled)
            {
                client.ItemStaging = itemStaging;

                client.QueuePacket(new TransactionStatusPacket(packet.WindowID, packet.TransactionID, handled));

                FurnaceBlock furnace = (FurnaceBlock)_serviceLocator.BlockRepository.GetBlockProvider((byte)BlockIDs.Furnace);
                furnace.TryStartFurnace(_serviceLocator.Server.Scheduler, _dimension, _location, ItemRepository);
            }
            else
            {
                client.QueuePacket(new TransactionStatusPacket(packet.WindowID, packet.TransactionID, handled));
            }
        }

        protected bool HandleLeftClick(int slotIndex, ref ItemStack itemStaging)
        {
            if (IsOutputSlot(slotIndex))
            {
                // can only remove from output slot.
                ItemStack output = this[slotIndex];

                // It is a No-Op if either the output slot is empty or the output
                // is not compatible with the item in hand.
                // It is assumed that Beta 1.7.3 sends a window click anyway in this case.
                if (output.Empty || !output.CanMerge(itemStaging))
                    return true;

                short itemID = output.ID;
                short metadata = output.Metadata;
                NbtCompound? nbt = output.Nbt;
                int maxStack = ItemRepository.GetItemProvider(itemID)!.MaximumStack;   // output is known to not be Empty
                int numToPickUp = Math.Min(maxStack - itemStaging.Count, output.Count);

                itemStaging = new ItemStack(itemID, (sbyte)(itemStaging.Count + numToPickUp), metadata, nbt);
                this[slotIndex] = output.GetReducedStack(numToPickUp);
                return true;
            }

            // Play-testing of Beta 1.7.3 shows
            //  - Anything can be placed in the Fuel Slot.
            //  - Anything can be placed in the Ingredient Slot
            //  Smelting begins if the item is burnable AND the Ingredient is smeltable.

            ItemStack slotContent = this[slotIndex];

            if (slotContent.Empty || itemStaging.Empty || !slotContent.CanMerge(itemStaging))
            {
                this[slotIndex] = itemStaging;
                itemStaging = slotContent;
                return true;
            }
            else
            {
                int maxStack = ItemRepository.GetItemProvider(itemStaging.ID)!.MaximumStack;   // itemStaging is known to not be Empty
                int numToPlace = Math.Min(maxStack - slotContent.Count, itemStaging.Count);
                this[slotIndex] = new ItemStack(slotContent.ID, (sbyte)(slotContent.Count + numToPlace),
                    slotContent.Metadata, slotContent.Nbt);
                itemStaging = itemStaging.GetReducedStack(numToPlace);
                return true;
            }
        }

        protected bool HandleShiftLeftClick(int slotIndex, ref ItemStack itemStaging)
        {
            // Play-testing Beta1.7.3 shows Shift-Left-Clicking moves items
            // from wherever they are to the Hotbar/Inventory.  Items are never
            // moved to the furnace slots.  They may be moved from the Furnace slots.
            // The content of the mouse cursor is not considered.

            ItemStack srcSlotContent = this[slotIndex];
            int areaIndex = GetAreaIndex(slotIndex);

            // If the source area is anywhere but the Hotbar
            if (areaIndex != (int)AreaIndices.Hotbar)
            {
                if (areaIndex == (int)AreaIndices.Main)
                {
                    // Move as many as possible to the Hotbar.
                    this[slotIndex] = Hotbar.StoreItemStack(srcSlotContent, false);
                }
                else
                {
                    // Move as many as possible to the Hotbar, then any remaining
                    // to the Inventory
                    ItemStack remaining = Hotbar.StoreItemStack(srcSlotContent, true);
                    remaining = MainInventory.StoreItemStack(remaining, true);
                    remaining = Hotbar.StoreItemStack(remaining, false);
                    this[slotIndex] = MainInventory.StoreItemStack(remaining, false);
                }
            }
            else
            {
                // The source area is the Hotbar.  Move as many as possible to
                // the main Inventory.
                this[slotIndex] = MainInventory.StoreItemStack(srcSlotContent, false);
            }

            return true;
        }

        protected bool HandleRightClick(int slotIndex, ref ItemStack itemStaging)
        {
            int maxStack;

            if (IsOutputSlot(slotIndex))
            {
                // can only remove from output slot.
                ItemStack output = this[slotIndex];

                // It is a No-Op if either the output slot is empty or the output
                // is not compatible with the item in hand.
                // It is assumed that Beta 1.7.3 sends a window click anyway in this case.
                if (output.Empty || !output.CanMerge(itemStaging))
                    return true;

                maxStack = ItemRepository.GetItemProvider(output.ID)!.MaximumStack;   // output is known to not be Empty
                if (itemStaging.Empty)
                {
                    sbyte amt = (sbyte)(output.Count / 2 + output.Count % 2);
                    itemStaging = new ItemStack(output.ID, amt, output.Metadata);
                    this[slotIndex] = output.GetReducedStack(amt);
                    return true;
                }

                if (itemStaging.Count < maxStack)
                {
                    // Play-testing of Beta1.7.3 shows that when the mouse cursor
                    // has a compatible item in it, all of the output stack is
                    // picked up, not half of it
                    sbyte amt = (sbyte)(output.Count + itemStaging.Count > maxStack ? maxStack - itemStaging.Count : output.Count);
                    itemStaging = new ItemStack(output.ID, (sbyte)(amt + itemStaging.Count), output.Metadata);
                    this[slotIndex] = output.GetReducedStack(amt);
                    return true;
                }

                return true;
            }

            ItemStack stack = this[slotIndex];
            if (itemStaging.Empty)
            {
                // If the stack is empty, there's nothing to do.
                if (stack.Empty)
                    return true;

                // An empty hand picks up half
                sbyte amt = (sbyte)(stack.Count / 2 + stack.Count % 2);
                itemStaging = new ItemStack(stack.ID, amt, stack.Metadata);
                this[slotIndex] = stack.GetReducedStack(amt);
                return true;
            }

            // If the stack is empty or compatible
            if (itemStaging.CanMerge(stack))
            {
                if (stack.Empty)
                {
                    this[slotIndex] = new ItemStack(itemStaging.ID, 1, itemStaging.Metadata);
                    itemStaging = itemStaging.GetReducedStack(1);
                    return true;
                }

                // Place one item.
                maxStack = ItemRepository.GetItemProvider(stack.ID)!.MaximumStack;   // stack is known to not be Empty
                if (stack.Count < maxStack)
                {
                    this[slotIndex] = new ItemStack(itemStaging.ID, (sbyte)(stack.Count + 1), itemStaging.Metadata);
                    itemStaging = itemStaging.GetReducedStack(1);
                    return true;
                }
            }

            // The stack and the staging item are incompatible
            this[slotIndex] = itemStaging;
            itemStaging = stack;
            return true;
        }

        protected bool HandleShiftRightClick(int slotIndex, ref ItemStack itemStaging)
        {
            return HandleShiftLeftClick(slotIndex, ref itemStaging);
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}
