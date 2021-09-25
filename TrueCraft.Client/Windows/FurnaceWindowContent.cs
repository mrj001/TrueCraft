using System;
using fNbt;
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
            IngredientIndex = 0;
            FuelIndex = IngredientIndex + Ingredient.Count;
            OutputIndex = FuelIndex + Fuel.Count;
            MainInventoryIndex = OutputIndex + Output.Count;
            HotbarIndex = MainInventoryIndex + MainInventory.Count;
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

        /// <inheritdoc />
        public int MainInventoryIndex { get; }

        public override ISlots Hotbar
        {
            get { return SlotAreas[(int)FurnaceWindowConstants.AreaIndices.Hotbar]; }
        }

        /// <inheritdoc />
        public int HotbarIndex { get; }

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
            if (IsOutputSlot(slotIndex))
            {
                // can only remove from output slot.
                ItemStack output = this[slotIndex];

                // It is a No-Op if either the output slot is empty or the output
                // is not compatible with the item in hand.
                // It is assumed that Beta 1.7.3 sends a window click anyway in this case.
                // However, the client can be compatible if we don't bother the
                // server about such things.
                if (output.Empty || !output.CanMerge(heldItem.HeldItem))
                    return null;

                return ActionConfirmation.GetActionConfirmation(() =>
                {
                    short itemID = output.ID;
                    short metadata = output.Metadata;
                    NbtCompound nbt = output.Nbt;
                    int maxStack = ItemRepository.GetItemProvider(itemID).MaximumStack;
                    int numToPickUp = Math.Min(maxStack - heldItem.HeldItem.Count, output.Count);

                    heldItem.HeldItem = new ItemStack(itemID, (sbyte)(heldItem.HeldItem.Count + numToPickUp), metadata, nbt);
                    this[slotIndex] = output.GetReducedStack(numToPickUp);
                });
            }

            // Play-testing of Beta 1.7.3 shows
            //  - Anything can be placed in the Fuel Slot.
            //  - Anything can be placed in the Ingredient Slot
            ItemStack slotContent = this[slotIndex];

            if (slotContent.Empty || heldItem.HeldItem.Empty || !slotContent.CanMerge(heldItem.HeldItem))
            {
                return ActionConfirmation.GetActionConfirmation(() =>
                {
                    this[slotIndex] = heldItem.HeldItem;
                    heldItem.HeldItem = slotContent;
                });
            }
            else
            {
                return ActionConfirmation.GetActionConfirmation(() =>
                {
                    int maxStack = ItemRepository.GetItemProvider(heldItem.HeldItem.ID).MaximumStack;
                    int numToPlace = Math.Min(maxStack - slotContent.Count, heldItem.HeldItem.Count);
                    this[slotIndex] = new ItemStack(slotContent.ID, (sbyte)(slotContent.Count + numToPlace),
                        slotContent.Metadata, slotContent.Nbt);
                    heldItem.HeldItem = heldItem.HeldItem.GetReducedStack(numToPlace);
                });
            }
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