using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrueCraft.API.Windows;
using TrueCraft.API;
using TrueCraft.API.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.API.Logic;

namespace TrueCraft.Core.Windows
{
    public abstract class WindowContent : IWindowContent, IDisposable, IEventSubject
    {
        protected WindowContent(ISlots[] slotAreas, IItemRepository itemRepository)
        {
            SlotAreas = slotAreas;
            this.ItemRepository = itemRepository;
        }

        protected ISlots[] SlotAreas { get; }

        public IItemRepository ItemRepository { get; }

        public event EventHandler<WindowChangeEventArgs> WindowChange;

        public bool IsDisposed { get; private set; }

        public IRemoteClient Client { get; set; }
                
        public sbyte ID { get; set; }
        public abstract string Name { get; }

        public abstract WindowType Type { get; }

        /// <summary>
        /// When shift-clicking items between areas, this method is used
        /// to determine which area links to which.
        /// </summary>
        /// <param name="index">The index of the area the item is coming from</param>
        /// <param name="slot">The item being moved</param>
        /// <returns>The area to place the item into</returns>
        protected abstract ISlots GetLinkedArea(int index, ItemStack slot);

        /// <summary>
        /// Gets the window area to handle this index and adjust index accordingly
        /// </summary>
        /// <param name="index">Input: the slot index within the overall window content.
        /// Output:  the slot index within the "Area".</param>
        /// <returns>The ISlots which contains the input index.</returns>
        protected ISlots GetArea(ref int index)
        {
            int startIndex = 0;
            foreach (var area in SlotAreas)
            {
                if (startIndex <= index && startIndex + area.Count > index)
                {
                    index = index - startIndex;
                    return area;
                }
                startIndex += area.Count;
            }
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Gets the index of the appropriate area from the WindowAreas array.
        /// </summary>
        /// <param name="index">The index of the slot within the overall window content.</param>
        /// <returns>The index of the "window area" within the window.</returns>
        protected int GetAreaIndex(int index)
        {
            int startIndex = 0;
            for (int i = 0; i < SlotAreas.Length; i++)
            {
                var area = SlotAreas[i];
                if (index >= startIndex && index < startIndex + area.Count)
                    return i;
                startIndex += area.Count;
            }
            throw new IndexOutOfRangeException();
        }

        public virtual int Length
        {
            get 
            {
                return SlotAreas.Sum(a => a.Count);
            }
        }

        public virtual int Length2 { get { return Length; } }

        public virtual ItemStack[] GetSlots()
        {
            int length = SlotAreas.Sum(area => area.Count);
            var slots = new ItemStack[length];
            int startIndex = 0;
            foreach (var windowArea in SlotAreas)
            {
                Array.Copy(windowArea.Items, 0, slots, startIndex, windowArea.Count);
                startIndex += windowArea.Count;
            }
            return slots;
        }

        public virtual void SetSlots(ItemStack[] slots)
        {
            int startIndex = 0;
            foreach (var windowArea in SlotAreas)
            {
                if (startIndex < slots.Length && startIndex + windowArea.Count <= slots.Length)
                    Array.Copy(slots, startIndex, windowArea.Items, 0, windowArea.Count);
                startIndex += windowArea.Count;
            }
        }

        public virtual ItemStack this[int index]
        {
            get
            {
                int startIndex = 0;
                foreach (var area in SlotAreas)
                {
                    if (index >= startIndex && index < startIndex + area.Count)
                        return area[index - startIndex];
                    startIndex += area.Count;
                }
                throw new IndexOutOfRangeException($"{nameof(index)} = {index} is outside the valid range of [0,{SlotAreas.Sum((a) => a.Count )})");
            }
            set
            {
                int startIndex = 0;
                foreach (var area in SlotAreas)
                {
                    if (index >= startIndex && index < startIndex + area.Count)
                    {
                        var eventArgs = new WindowChangeEventArgs(index, value);
                        OnWindowChange(eventArgs);
                        if (!eventArgs.Handled)
                            area[index - startIndex] = value;
                        return;
                    }
                    startIndex += area.Count;
                }
                throw new IndexOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public abstract ItemStack StoreItemStack(ItemStack slot, bool topUpOnly);

        protected internal virtual void OnWindowChange(WindowChangeEventArgs e)
        {
            if (WindowChange != null)
                WindowChange(this, e);
        }

        public event EventHandler Disposed;

        public virtual void Dispose()
        {
            for (int i = 0; i < SlotAreas.Length; i++)
            {
                SlotAreas[i].Dispose();
            }
            WindowChange = null;
            if (Disposed != null)
                Disposed(this, null);
            Client = null;
            IsDisposed = true;
        }

        public virtual short[] ReadOnlySlots
        {
            get { return new short[0]; }
        }

        public static void HandleClickPacket(ClickWindowPacket packet, IWindowContent window, ref ItemStack itemStaging)
        {
            if (packet.SlotIndex >= window.Length || packet.SlotIndex < 0)
                return;
            var existing = window[packet.SlotIndex];
            if (window.ReadOnlySlots.Contains(packet.SlotIndex))
            {
                if (itemStaging.ID == existing.ID || itemStaging.Empty)
                {
                    if (itemStaging.Empty)
                        itemStaging = existing;
                    else
                        itemStaging.Count += existing.Count;
                    window[packet.SlotIndex] = ItemStack.EmptyStack;
                }
                return;
            }
            if (itemStaging.Empty) // Picking up something
            {
                if (packet.Shift)
                {
                    window.MoveItemStack(packet.SlotIndex);
                }
                else
                {
                    if (packet.RightClick)
                    {
                        sbyte mod = (sbyte)(existing.Count % 2);
                        existing.Count /= 2;
                        itemStaging = existing;
                        itemStaging.Count += mod;
                        window[packet.SlotIndex] = existing;
                    }
                    else
                    {
                        itemStaging = window[packet.SlotIndex];
                        window[packet.SlotIndex] = ItemStack.EmptyStack;
                    }
                }
            }
            else // Setting something down
            {
                if (existing.Empty) // Replace empty slot
                {
                    if (packet.RightClick)
                    {
                        var newItem = (ItemStack)itemStaging.Clone();
                        newItem.Count = 1;
                        itemStaging.Count--;
                        window[packet.SlotIndex] = newItem;
                    }
                    else
                    {
                        window[packet.SlotIndex] = itemStaging;
                        itemStaging = ItemStack.EmptyStack;
                    }
                }
                else
                {
                    if (existing.CanMerge(itemStaging)) // Merge items
                    {
                        // TODO: Consider the maximum stack size
                        if (packet.RightClick)
                        {
                            existing.Count++;
                            itemStaging.Count--;
                            window[packet.SlotIndex] = existing;
                        }
                        else
                        {
                            existing.Count += itemStaging.Count;
                            window[packet.SlotIndex] = existing;
                            itemStaging = ItemStack.EmptyStack;
                        }
                    }
                    else // Swap items
                    {
                        window[packet.SlotIndex] = itemStaging;
                        itemStaging = existing;
                    }
                }
            }
        }

        /// <inheritdoc />
        public abstract ItemStack MoveItemStack(int index);
    }
}