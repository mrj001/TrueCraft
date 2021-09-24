﻿using System;
using TrueCraft.API;
using TrueCraft.API.Logic;
using TrueCraft.API.Server;
using TrueCraft.API.Windows;
using TrueCraft.API.World;
using TrueCraft.Client.Handlers;
using TrueCraft.Client.Modules;
using TrueCraft.Core.Windows;

namespace TrueCraft.Client.Windows
{
    public class FurnaceWindowContentClient : WindowContentClient, IFurnaceWindowContent
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainInventory"></param>
        /// <param name="hotBar"></param>
        /// <param name="scheduler"></param>
        /// <param name="coordinates"></param>
        /// <param name="itemRepository"></param>
        public FurnaceWindowContentClient(ISlots mainInventory, ISlots hotBar,
            IItemRepository itemRepository) :
            base(FurnaceWindowConstants.Areas(mainInventory, hotBar),
                itemRepository)
        {
        }

        // Indices of the area within the Furnace
        // Note that these are the same values as the slot
        // indices only because the areas contain a single slot.
        // They are conceptually different.
        //private const int IngredientAreaIndex = 0;
        //private const int FuelAreaIndex = 1;
        //private const int OutputAreaIndex = 2;
        //private const int MainAreaIndex = 3;
        //private const int HotbarAreaIndex = 4;

        // Slot Indices within the overall Furnace
        //public const short IngredientIndex = 0;
        //public const short FuelIndex = 1;
        private const short OutputIndex = 2;
        //public const short MainIndex = 3;
        //public const short HotbarIndex = 30;    // TODO: implicitly hard-codes the size of the main inventory

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

        public ISlots Fuel
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Fuel]; }
        }

        public ISlots Output
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Output]; }
        }

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
            // TODO: this function should not be needed client-side.
            throw new NotImplementedException();
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

        protected override ActionConfirmation HandleLeftClick(int slotIndex, IHeldItem heldItem)
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override ActionConfirmation HandleShiftLeftClick(int slotIndex, IHeldItem heldItem)
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override ActionConfirmation HandleRightClick(int slotIndex, IHeldItem heldItem)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}