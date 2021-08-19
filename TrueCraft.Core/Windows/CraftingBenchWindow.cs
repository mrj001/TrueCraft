﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrueCraft.API.Windows;
using TrueCraft.API.Logic;
using TrueCraft.API;

namespace TrueCraft.Core.Windows
{
    public class CraftingBenchWindow : Window
    {
        public CraftingBenchWindow(ICraftingRepository craftingRepository, InventoryWindow inventory)
        {
            WindowAreas = new[]
            {
                new CraftingWindowArea(craftingRepository, CraftingOutputIndex, 3, 3),
                new Slots(MainIndex, 27, 9, 3), // Main inventory
                new Slots(HotbarIndex, 9, 9, 1) // Hotbar
            };
            if (inventory != null)
            {
                inventory.MainInventory.CopyTo(MainInventory);
                inventory.Hotbar.CopyTo(Hotbar);
            }
            foreach (var area in WindowAreas)
                area.WindowChange += (s, e) => OnWindowChange(new WindowChangeEventArgs(
                        (s as Slots).StartIndex + e.SlotIndex, e.Value));
            Copying = false;
            if (inventory != null)
            {
                inventory.WindowChange += (sender, e) =>
                {
                    if (Copying)
                        return;
                    if ((e.SlotIndex >= InventoryWindow.MainIndex && e.SlotIndex < InventoryWindow.MainIndex + inventory.MainInventory.Count)
                    || (e.SlotIndex >= InventoryWindow.HotbarIndex && e.SlotIndex < InventoryWindow.HotbarIndex + inventory.Hotbar.Count))
                    {
                        inventory.MainInventory.CopyTo(MainInventory);
                        inventory.Hotbar.CopyTo(Hotbar);
                    }
                };
            }
        }

        #region Variables

        private bool Copying { get; set; }

        public const short HotbarIndex = 37;
        public const short CraftingGridIndex = 1;
        public const short CraftingOutputIndex = 0;
        public const short MainIndex = 10;

        public override string Name
        {
            get
            {
                return "Workbench";
            }
        }

        public override WindowType Type
        {
            get
            {
                return WindowType.CraftingBench;
            }
        }

        public override short[] ReadOnlySlots
        {
            get
            {
                return new[] { CraftingOutputIndex };
            }
        }

        public override ISlots[] WindowAreas { get; }

        #region Properties

        public ISlots CraftingGrid 
        {
            get { return WindowAreas[0]; }
        }

        public ISlots MainInventory
        {
            get { return WindowAreas[1]; }
        }

        public ISlots Hotbar
        {
            get { return WindowAreas[2]; }
        }

        #endregion

        #endregion

        public override ItemStack[] GetSlots()
        {
            var relevantAreas = new[] { CraftingGrid };
            int length = relevantAreas.Sum(area => area.Count);
            var slots = new ItemStack[length];
            foreach (var windowArea in relevantAreas)
                Array.Copy(windowArea.Items, 0, slots, windowArea.StartIndex, windowArea.Count);
            return slots;
        }

        public override void CopyToInventory(IWindow inventoryWindow)
        {
            var window = (InventoryWindow)inventoryWindow;
            Copying = true;
            MainInventory.CopyTo(window.MainInventory);
            Hotbar.CopyTo(window.Hotbar);
            Copying = false;
        }

        protected override ISlots GetLinkedArea(int index, ItemStack slot)
        {
            if (index < MainIndex)
                return MainInventory;
            return Hotbar;
        }

        public override bool PickUpStack(ItemStack slot)
        {
            var area = MainInventory;
            foreach (var item in Hotbar.Items)
            {
                if (item.Empty || (slot.ID == item.ID && slot.Metadata == item.Metadata))
                    //&& item.Count + slot.Count < Item.GetMaximumStackSize(new ItemDescriptor(item.Id, item.Metadata)))) // TODO
                {
                    area = Hotbar;
                    break;
                }
            }
            int index = area.MoveOrMergeItem(-1, slot, null);
            return index != -1;
        }
    }
}
