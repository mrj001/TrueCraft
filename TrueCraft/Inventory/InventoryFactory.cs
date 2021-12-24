using System;
using TrueCraft.Core.Inventory;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Inventory
{
    public class InventoryFactory : IInventoryFactory<IServerSlot>
    {
        public InventoryFactory()
        {
        }

        public IWindow<IServerSlot> NewInventoryWindow(IItemRepository itemRepository,
            ICraftingRepository craftingRepository, ISlotFactory<IServerSlot> slotFactory,
            ISlots<IServerSlot> mainInventory, ISlots<IServerSlot> hotBar)
        {
            throw new NotImplementedException();
        }

        public ICraftingBenchWindow<IServerSlot> NewCraftingBenchWindow(IItemRepository itemRepository,
            ICraftingRepository craftingRepository, ISlotFactory<IServerSlot> slotFactory,
            sbyte windowID, ISlots<IServerSlot> mainInventory, ISlots<IServerSlot> hotBar,
            string name, int width, int height)
        {
            return new CraftingBenchWindow(itemRepository, craftingRepository,
                slotFactory, windowID, mainInventory, hotBar, name, width, height);
        }

        public IChestWindow<IServerSlot> NewChestWindow(IItemRepository itemRepository,
            ISlotFactory<IServerSlot> slotFactory, sbyte windowID, ISlots<IServerSlot> mainInventory,
            ISlots<IServerSlot> hotBar, IWorld world, GlobalVoxelCoordinates location,
            GlobalVoxelCoordinates otherHalf)
        {
            return new ChestWindow(itemRepository, slotFactory, windowID,
                mainInventory, hotBar, world, location, otherHalf);
        }

        public IFurnaceWindow<IServerSlot> NewFurnaceWindow(IItemRepository itemRepository,
            ISlotFactory<IServerSlot> slotFactory, sbyte windowID, ISlots<IServerSlot> mainInventory,
            ISlots<IServerSlot> hotBar, IWorld world, GlobalVoxelCoordinates location)
        {
            return new FurnaceWindow(itemRepository, slotFactory, windowID,
                mainInventory, hotBar, world, location);
        }
    }
}