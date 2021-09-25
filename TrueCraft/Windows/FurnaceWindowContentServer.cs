using System;
using System.Linq;
using TrueCraft.API;
using TrueCraft.API.Logic;
using TrueCraft.API.Server;
using TrueCraft.API.Windows;
using TrueCraft.API.World;
using TrueCraft.Core.Windows;

namespace TrueCraft.Windows
{
    public class FurnaceWindowContentServer : WindowContentServer, IFurnaceWindowContent
    {
        public IEventScheduler EventScheduler { get; set; }
        public GlobalVoxelCoordinates Coordinates { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainInventory"></param>
        /// <param name="hotBar"></param>
        /// <param name="scheduler"></param>
        /// <param name="coordinates"></param>
        /// <param name="itemRepository"></param>
        public FurnaceWindowContentServer(ISlots mainInventory, ISlots hotBar,
            IEventScheduler scheduler, GlobalVoxelCoordinates coordinates,
            IItemRepository itemRepository) :
            base(FurnaceWindowConstants.Areas(mainInventory, hotBar),
                itemRepository)
        {
            EventScheduler = scheduler;
            Coordinates = coordinates;

            IngredientIndex = 0;
            FuelIndex = IngredientIndex + Ingredient.Count;
            OutputIndex = FuelIndex + Fuel.Count;
        }

        public override string Name
        {
            get
            {
                return "Furnace";
            }
        }

        public override WindowType Type
        {
            get
            {
                return WindowType.Furnace;
            }
        }

        public override bool IsOutputSlot(int slotIndex)
        {
            return slotIndex == OutputIndex;
        }

        public ISlots Ingredient 
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Ingredient]; }
        }

        /// <inheritdoc />
        public int IngredientIndex { get; }

        public ISlots Fuel
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Fuel]; }
        }

        /// <inheritdoc />
        public int FuelIndex { get; }

        public ISlots Output
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Output]; }
        }

        /// <inheritdoc />
        public int OutputIndex { get; }

        public override ISlots MainInventory
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Main]; }
        }

        public override ISlots Hotbar
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Hotbar]; }
        }

        public override bool IsPlayerInventorySlot(int slotIndex)
        {
            // NOTE: hard-coded assumption that Fuel, Ingredient, and Output
            // are all a single slot.  Seems like a pretty robust assumption.
            return slotIndex >= 3;
        }

        public override ItemStack[] GetSlots()
        {
            ItemStack[] rv = new ItemStack[this.Length];
            rv[0] = Ingredient[0];
            rv[1] = Fuel[0];
            rv[2] = Output[0];

            int index = 3;
            for (int j = 0, jul= MainInventory.Count; j < jul; j ++, index ++)
                rv[index] = MainInventory[j];

            for (int j = 0, jul = Hotbar.Count; j < jul; j++, index++)
                rv[index] = Hotbar[j];

            return rv;
        }

        protected override ISlots GetLinkedArea(int index, ItemStack slot)
        {
            if (index < (int)FurnaceWindowConstants.AreaIndices.Main)
                return MainInventory;
            return Hotbar;
        }

        /// <inheritdoc />
        public override ItemStack MoveItemStack(int index)
        {
            FurnaceWindowConstants.AreaIndices sourceAreaIndex = (FurnaceWindowConstants.AreaIndices)GetAreaIndex(index);
            ItemStack remaining = this[index];

            switch(sourceAreaIndex)
            {
                case FurnaceWindowConstants.AreaIndices.Ingredient:
                case FurnaceWindowConstants.AreaIndices.Fuel:
                case FurnaceWindowConstants.AreaIndices.Output:
                    remaining = MoveToInventory(remaining);
                    break;

                case FurnaceWindowConstants.AreaIndices.Main:
                case FurnaceWindowConstants.AreaIndices.Hotbar:
                    remaining = MoveToFurnace(remaining);
                    break;

                default:
                    throw new ApplicationException();
            }

            return remaining;
        }

        private ItemStack MoveToInventory(ItemStack source)
        {
            ItemStack remaining = MainInventory.StoreItemStack(source, true);

            if (!remaining.Empty)
                remaining = Hotbar.StoreItemStack(remaining, false);

            if (!remaining.Empty)
                remaining = MainInventory.StoreItemStack(remaining, false);

            return remaining;
        }

        private ItemStack MoveToFurnace(ItemStack source)
        {
            ItemStack remaining = source;
            IItemProvider provider = ItemRepository.GetItemProvider(source.ID);

            if (provider is IBurnableItem)
            {
                remaining = Fuel.StoreItemStack(remaining, false);
                if (remaining.Empty)
                    return ItemStack.EmptyStack;
            }

            if (provider is ISmeltableItem)
            {
                remaining = Ingredient.StoreItemStack(remaining, false);
                if (remaining.Empty)
                    return ItemStack.EmptyStack;
            }

            return remaining;
        }

        public override ItemStack StoreItemStack(ItemStack slot, bool topUpOnly)
        {
            throw new NotImplementedException();
        }

        protected override bool HandleLeftClick(int slotIndex, ref ItemStack itemStaging)
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override bool HandleShiftLeftClick(int slotIndex, ref ItemStack itemStaging)
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override bool HandleRightClick(int slotIndex, ref ItemStack itemStaging)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}