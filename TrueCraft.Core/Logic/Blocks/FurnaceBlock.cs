using System;
using System.Runtime.CompilerServices;
using System.Xml;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;
using TrueCraft.Core.Windows;
using System.Collections.Generic;
using fNbt;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Inventory;
using System.ComponentModel;

namespace TrueCraft.Core.Logic.Blocks
{
    public class FurnaceBlock : BlockProvider
    {
        protected class FurnaceState : IFurnaceSlots
        {
            private readonly NbtCompound _furnaceState;

            private const string BurnTimeRemainingTag = "BurnTime";

            private const string BurnTimeTotalTag = "BurnTotal";

            private const string CookTimeTag = "CookTime";

            private const int IngredientIndex = 0;

            private const int FuelIndex = 1;

            private const int OutputIndex = 2;

            /// <summary>
            /// Constructs a new default FurnaceState.
            /// </summary>
            public FurnaceState()
            {
                _furnaceState = new NbtCompound(new NbtTag[]
                {
                    new NbtShort("BurnTime", 0),
                    new NbtShort("BurnTotal", 0),
                    new NbtShort("CookTime", -1),
                    new NbtList("Items", new[]
                    {
                        ItemStack.EmptyStack.ToNbt(),
                        ItemStack.EmptyStack.ToNbt(),
                        ItemStack.EmptyStack.ToNbt()
                    }, NbtTagType.Compound)
                });
                IngredientSlot = new IngredientSlotImpl(IngredientIndex, this);
                FuelSlot = new FuelSlotImpl(FuelIndex, this);
                OutputSlot = new OutputSlotImpl(OutputIndex, this);
            }

            /// <summary>
            /// Constructs a new instance of FurnaceState from a Tile Entity.
            /// </summary>
            /// <param name="tileEntity">The Tile Entity which stores the furnace state on disk.</param>
            public FurnaceState(NbtCompound tileEntity)
            {
                _furnaceState = tileEntity;
                IngredientSlot = new IngredientSlotImpl(IngredientIndex, this);
                FuelSlot = new FuelSlotImpl(FuelIndex, this);
                OutputSlot = new OutputSlotImpl(OutputIndex, this);
            }

            private short InternalGet(string tag)
            {
                lock (_furnaceState)
                {
                    return (short)(_furnaceState.Get<NbtShort>(tag)?.Value ?? 0);
                }
            }

            private void InternalSet(string tag, short value)
            {
                lock (_furnaceState)
                {
                    NbtShort burnTime = _furnaceState.Get<NbtShort>(tag);
                    if (burnTime == null)
                    {
                        burnTime = new NbtShort(tag);
                        _furnaceState.Add(burnTime);
                    }
                    burnTime.Value = value;
                }
            }

            /// <summary>
            /// Gets or sets the current number of ticks remaining for the
            /// Furnace to be lit by the most recently consumed fuel item.
            /// </summary>
            public short BurnTimeRemaining
            {
                get => InternalGet(BurnTimeRemainingTag);
                set => InternalSet(BurnTimeRemainingTag, value);
            }

            /// <summary>
            /// Gets or sets the total number of ticks of burning provided by
            /// the most recently consumed fuel item.
            /// </summary>
            public short BurnTimeTotal
            {
                get => InternalGet(BurnTimeTotalTag);
                set => InternalSet(BurnTimeTotalTag, value);
            }

            /// <summary>
            /// Gets or sets the number of ticks spent cooking the current Ingredient.
            /// </summary>
            public short CookTime
            {
                get => InternalGet(CookTimeTag);
                set => InternalSet(CookTimeTag, value);
            }

            private ItemStack InternalSlotGet(int slotIndex)
            {
                lock(_furnaceState)
                {
                    NbtList items = _furnaceState.Get<NbtList>("Items");
                    return ItemStack.FromNbt(items.Get<NbtCompound>(slotIndex));
                }
            }

            private void InternalSlotSet(int slotIndex, ItemStack stack)
            {
                lock(_furnaceState)
                {
                    NbtList items = _furnaceState.Get<NbtList>("Items");
                    items[slotIndex] = stack.ToNbt();
                }
            }

            public ItemStack Ingredient
            {
                get => InternalSlotGet(IngredientIndex);
                set => InternalSlotSet(IngredientIndex, value);
            }

            public void DecrementIngredient()
            {
                InternalDecrement(IngredientIndex);
            }

            public ItemStack Fuel
            {
                get => InternalSlotGet(FuelIndex);
                set => InternalSlotSet(FuelIndex, value);
            }

            private void InternalDecrement(int slotIndex)
            {
                lock (_furnaceState)
                {
                    NbtList items = _furnaceState.Get<NbtList>("Items");
                    NbtCompound fuel = (NbtCompound)items[slotIndex];
                    NbtByte cnt = (NbtByte)fuel["Count"];
                    cnt.Value -= 1;
                }
            }

            public void DecrementFuel()
            {
                InternalDecrement(FuelIndex);
            }

            public ItemStack Output
            {
                get => InternalSlotGet(OutputIndex);
                set => InternalSlotSet(OutputIndex, value);
            }

            public void Save(IDimension dimension, GlobalVoxelCoordinates coordinates)
            {
                ((IDimensionServer)dimension).SetTileEntity(coordinates, _furnaceState);
            }

            #region IFurnaceSlots
            // TODO It is not possible to robustly implement the INotifyPropertyChanged
            //    as the NbtTag class does not implement it.  So we have no way to
            //    detect if the underlying storage is changed through other means.
            private abstract class SlotImpl : IServerSlot
            {
                private bool _dirty = false;

                protected FurnaceState _parent;

                protected SlotImpl(int slotIndex, FurnaceState parent)
                {
                    Index = slotIndex;
                    _parent = parent;
                }

                protected virtual void OnPropertyChanged([CallerMemberName]string name = "")
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }

                public bool Dirty
                {
                    get => _dirty;
                    protected set
                    {
                        if (_dirty == value) return;
                        _dirty = value;
                        OnPropertyChanged();
                    }
                }

                public void SetClean()
                {
                    Dirty = false;
                }

                public int Index { get; }

                public abstract ItemStack Item { get; set; }

                public event PropertyChangedEventHandler? PropertyChanged;

                public int CanAccept(ItemStack other)
                {
                    if (other.Empty) return 0;

                    ItemStack item = this.Item;

                    if (item.Empty) return other.Count;

                    if (item.CanMerge(other))
                    {
                        IItemProvider provider = ItemRepository.Get().GetItemProvider(item.ID)!;
                        int maxStack = provider.MaximumStack;
                        return Math.Min(maxStack - item.Count, other.Count);
                    }

                    return 0;
                }

                public SetSlotPacket GetSetSlotPacket(sbyte windowID)
                {
                    Dirty = false;
                    ItemStack item = Item;
                    return new SetSlotPacket(windowID, (short)Index, item.ID, item.Count, item.Metadata);
                }
            }

            private class IngredientSlotImpl : SlotImpl
            {

                public IngredientSlotImpl(int slotIndex, FurnaceState parent) : base(slotIndex, parent)
                {
                }

                public override ItemStack Item
                {
                    get => _parent.Ingredient;
                    set
                    {
                        _parent.Ingredient = value;
                        OnPropertyChanged();
                        Dirty = true;
                    }
                }
            }

            public IServerSlot IngredientSlot { get; }

            private class FuelSlotImpl : SlotImpl
            {
                public FuelSlotImpl(int slotIndex, FurnaceState parent) : base(slotIndex, parent)
                {

                }

                public override ItemStack Item
                {
                    get => _parent.Fuel;
                    set
                    {
                        _parent.Fuel = value;
                        OnPropertyChanged();
                        Dirty = true;
                    }
                }
            }

            public IServerSlot FuelSlot { get; }

            public IServerSlot OutputSlot { get; }

            private class OutputSlotImpl : SlotImpl
            {
                public OutputSlotImpl(int slotIndex, FurnaceState parent) : base (slotIndex, parent)
                {

                }

                public override ItemStack Item
                {
                    get => _parent.Output;
                    set
                    {
                        _parent.Output = value;
                        OnPropertyChanged();
                        Dirty = true;
                    }
                }
            }
            #endregion
        }

        protected class FurnaceEventSubject : IEventSubject
        {
            public event EventHandler? Disposed;

            public void Dispose()
            {
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// A class to track the pair of the FurnaceWindow and its associated IRemoteClient instance
        /// </summary>
        protected class FurnaceWindowUser
        {
            public FurnaceWindowUser(IFurnaceWindow<IServerSlot> window, IRemoteClient user)
            {
                this.Window = window;
                this.User = user;
            }

            public IFurnaceWindow<IServerSlot> Window { get; }
            public IRemoteClient User { get; }
        }

        public FurnaceBlock(XmlNode node) : base(node)
        {

        }

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            // TODO: should Furnaces also drop their contents?
            return new[] { new ItemStack((short)BlockIDs.Furnace) };
        }

        // TODO: An instance of GlobalVoxelCoordinates is not sufficient.  If a
        //       furnace is placed in another dimension at the same coordinates,
        //       this will fail.
        protected static Dictionary<GlobalVoxelCoordinates, FurnaceEventSubject> _scheduledFurnaces = new Dictionary<GlobalVoxelCoordinates, FurnaceEventSubject>();
        protected static Dictionary<GlobalVoxelCoordinates, List<FurnaceWindowUser>> _trackedFurnaceWindows = new Dictionary<GlobalVoxelCoordinates, List<FurnaceWindowUser>>();
        protected static Dictionary<GlobalVoxelCoordinates, FurnaceState> _trackedFurnaceStates = new Dictionary<GlobalVoxelCoordinates, FurnaceState>();

        private FurnaceState GetState(IDimension dimension, GlobalVoxelCoordinates coords)
        {
            ServerOnly.Assert();

            if (_trackedFurnaceStates.ContainsKey(coords))
                return _trackedFurnaceStates[coords];

            FurnaceState rv;
            NbtCompound? tileEntity = ((IDimensionServer)dimension).GetTileEntity(coords);
            if (tileEntity is null)
            {
                rv = new FurnaceState();
                rv.Save(dimension, coords);
            }
            else
            {
                rv = new FurnaceState(tileEntity);
            }

            _trackedFurnaceStates.Add(coords, rv);
            return rv;
        }

        /// <summary>
        /// Sends out Progress packets to all Clients with a Furnace Window open
        /// at the specified coordinates.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="state"></param>
        private void UpdateWindows(GlobalVoxelCoordinates coords, FurnaceState state)
        {
            ServerOnly.Assert();

            if (_trackedFurnaceWindows.ContainsKey(coords))
            {
                Handling = true;
                foreach (var window in _trackedFurnaceWindows[coords])
                    UpdateWindow(window.Window, window.User, state);
                Handling = false;
            }
        }

        /// <summary>
        /// Sends Progress packets for a single open Furnace Window.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="user"></param>
        /// <param name="state"></param>
        private void UpdateWindow(IFurnaceWindow<IServerSlot> window, IRemoteClient user, FurnaceState state)
        {
            user.QueuePacket(new UpdateProgressPacket(
                        window.WindowID, UpdateProgressPacket.ProgressTarget.ItemCompletion, state.CookTime));
            double burnProgress = state.BurnTimeRemaining / (double)state.BurnTimeTotal;
            short burn = (short)(burnProgress * 250);
            user.QueuePacket(new UpdateProgressPacket(
                window.WindowID, UpdateProgressPacket.ProgressTarget.AvailableHeat, burn));
        }

        private void SetState(IDimension dimension, GlobalVoxelCoordinates coords, FurnaceState state)
        {
            state.Save(dimension, coords);
            UpdateWindows(coords, state);
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            ServerOnly.Assert();

            FurnaceState state = GetState(dimension, coords);
            if (state.BurnTimeRemaining > 0)
                ScheduleFurnace(server.Scheduler, dimension, coords, ItemRepository.Get());
        }

        public override void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            // TODO clean up running Furnace

            IDimensionServer dimensionServer = (IDimensionServer)dimension;
            NbtCompound? entity = dimensionServer.GetTileEntity(descriptor.Coordinates);
            if (entity is not null)
            {
                foreach (var item in (NbtList)entity["Items"])
                {
                    IEntityManager manager = ((IDimensionServer)dimension).EntityManager;
                    var slot = ItemStack.FromNbt((NbtCompound)item);
                    manager.SpawnEntity(new ItemEntity(dimension, manager,
                        new Vector3(descriptor.Coordinates.X + 0.5, descriptor.Coordinates.Y + 0.5, descriptor.Coordinates.Z + 0.5), slot));
                }
                dimensionServer.SetTileEntity(descriptor.Coordinates, null);
            }
            base.BlockMined(descriptor, face, dimension, user);
        }

        public override bool BlockRightClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            FurnaceState state = GetState(dimension, descriptor.Coordinates);
            IInventoryFactory<IServerSlot> factory = new InventoryFactory<IServerSlot>();
            IFurnaceWindow<IServerSlot> window = factory.NewFurnaceWindow(serviceLocator,
                SlotFactory<IServerSlot>.Get(), WindowIDs.GetWindowID(),
                state, user.Inventory, user.Hotbar,
                dimension, descriptor.Coordinates);

            user.OpenWindow(window);
            if (!_trackedFurnaceWindows.ContainsKey(descriptor.Coordinates))
                _trackedFurnaceWindows[descriptor.Coordinates] = new List<FurnaceWindowUser>();
            _trackedFurnaceWindows[descriptor.Coordinates].Add(new FurnaceWindowUser(window, user));
            window.WindowClosed += (sender, e) => _trackedFurnaceWindows.Remove(descriptor.Coordinates);

            UpdateWindow(window, user, state);

            return false;
        }

        private bool Handling = false;

        /// <summary>
        /// Tries to change the Furnace from the Off state to the Lit state.
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="dimension"></param>
        /// <param name="coords"></param>
        /// <param name="itemRepository"></param>
        public void TryStartFurnace(IEventScheduler scheduler, IDimension dimension,
            GlobalVoxelCoordinates coords, IItemRepository itemRepository)
        {
            // If the furnace is already scheduled, it is already lit.
            if (_scheduledFurnaces.ContainsKey(coords))
                return;

            FurnaceState state = GetState(dimension, coords);
            ItemStack inputStack = state.Ingredient;
            ItemStack fuelStack = state.Fuel;
            ItemStack outputStack = state.Output;

            ISmeltableItem? input = itemRepository.GetItemProvider(inputStack.ID) as ISmeltableItem;
            IBurnableItem? fuel = itemRepository.GetItemProvider(fuelStack.ID) as IBurnableItem;

            if (!CanStartBurningFuel(itemRepository, fuel, input, outputStack))
                return;

            ScheduleFurnace(scheduler, dimension, coords, itemRepository);

            dimension.SetBlockID(coords, (byte)BlockIDs.LitFurnace);

            state.BurnTimeTotal = (short)(fuel!.BurnTime.TotalSeconds * 20);  // TODO hard-coded ticks per second
            state.BurnTimeRemaining = state.BurnTimeTotal;
            state.CookTime = 0;
            state.DecrementFuel();
            fuelStack = state.Fuel;
            foreach (FurnaceWindowUser fwu in _trackedFurnaceWindows[coords])
                fwu.User.QueuePacket(new SetSlotPacket(fwu.Window.WindowID, (short)fwu.Window.FuelSlotIndex, fuelStack.ID, fuelStack.Count, fuelStack.Metadata));

            UpdateWindows(coords, state);
        }

        /// <summary>
        /// Determines whether or not the Furnace will start burning an item of Fuel.
        /// </summary>
        /// <param name="itemRepository"></param>
        /// <param name="fuel"></param>
        /// <param name="input"></param>
        /// <param name="outputStack"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// To start burning a fuel item, we need:
        /// <list type="number">
        /// <item>Fuel</item>
        /// <item>a smeltable ingredient, AND</item>
        /// <item>room for the output</item>
        /// </list>
        /// </para></remarks>
        private bool CanStartBurningFuel(IItemRepository itemRepository,
            IBurnableItem? fuel, ISmeltableItem? input, ItemStack outputStack)
        {
            if (fuel is null || input is null)
                return false;

            if (!outputStack.CanMerge(input.SmeltingOutput))
                return false;

            IItemProvider? provider = itemRepository.GetItemProvider(input.SmeltingOutput.ID);
            if (provider is null || outputStack.Count == provider.MaximumStack)
                return false;

            return true;
        }

        private void ScheduleFurnace(IEventScheduler scheduler,
            IDimension dimension, GlobalVoxelCoordinates coords, IItemRepository itemRepository)
        {
            FurnaceEventSubject subject = new FurnaceEventSubject();
            _scheduledFurnaces[coords] = subject;
            // TODO hard-coded TimeSpan that must be coordinated with hard-coded values in other places.
            scheduler.ScheduleEvent("smelting", subject, TimeSpan.FromSeconds(1),
                server => UpdateFurnace(server.Scheduler, dimension, coords, itemRepository));
        }

        private void UpdateFurnace(IEventScheduler scheduler, IDimension dimension, GlobalVoxelCoordinates coords, IItemRepository itemRepository)
        {
            // This furnace is no longer scheduled, so remove it.
            if (!_scheduledFurnaces.ContainsKey(coords))
                return;
            _scheduledFurnaces.Remove(coords);

            FurnaceState state = GetState(dimension, coords);
            ItemStack inputStack = state.Ingredient;
            ItemStack fuelStack = state.Fuel;
            ItemStack outputStack = state.Output;

            ISmeltableItem? input = itemRepository.GetItemProvider(inputStack.ID) as ISmeltableItem;
            IBurnableItem? fuel = itemRepository.GetItemProvider(fuelStack.ID) as IBurnableItem;

            if (input is not null)
            {
                // Increase CookTime
                state.CookTime += 20;   // TODO hard-coded value that must be coordinated with TimeSpan in ScheduleFurnace
                if (state.CookTime >= 200)   // TODO Hard-coded constant
                {
                    state.DecrementIngredient();
                    inputStack = state.Ingredient;

                    IItemProvider outputItemProvider = itemRepository.GetItemProvider(input.SmeltingOutput.ID)!;
                    int maxStack = outputItemProvider.MaximumStack;
                    if (outputStack.Empty)
                        outputStack = input.SmeltingOutput;
                    else
                        outputStack.Count += (sbyte)Math.Min(maxStack - outputStack.Count, input.SmeltingOutput.Count);
                    state.Output = outputStack;

                    foreach(FurnaceWindowUser fwu in _trackedFurnaceWindows[coords])
                    {
                        IFurnaceWindow<IServerSlot> window = fwu.Window;
                        IRemoteClient user = fwu.User;
                        user.QueuePacket(new SetSlotPacket(window.WindowID, (short)window.IngredientSlotIndex, inputStack.ID, inputStack.Count, inputStack.Metadata));
                        user.QueuePacket(new SetSlotPacket(window.WindowID, (short)window.OutputSlotIndex, outputStack.ID, outputStack.Count, outputStack.Metadata));
                    }

                    state.CookTime = 0;
                }
            }

            // Reduce BurnTimeRemaining
            state.BurnTimeRemaining -= 20;   // TODO hard-coded value that must be coordinated with TimeSpan in ScheduleFurnace
            if (state.BurnTimeRemaining <= 0)
            {
                if (CanStartBurningFuel(itemRepository, fuel, input, outputStack))
                {
                    state.BurnTimeTotal = (short)(fuel!.BurnTime.TotalSeconds * 20);  // TODO hard-coded ticks per second
                    state.BurnTimeRemaining = state.BurnTimeTotal;
                    state.DecrementFuel();
                    fuelStack = state.Fuel;
                    foreach (FurnaceWindowUser fwu in _trackedFurnaceWindows[coords])
                        fwu.User.QueuePacket(new SetSlotPacket(fwu.Window.WindowID, (short)fwu.Window.FuelSlotIndex, fuelStack.ID, fuelStack.Count, fuelStack.Metadata));
                }
                else
                {
                    state.BurnTimeRemaining = 0;
                    state.BurnTimeTotal = 0;
                    state.CookTime = 0;
                }
            }

            if (state.BurnTimeRemaining > 0)
                ScheduleFurnace(scheduler, dimension, coords, itemRepository);

            UpdateWindows(coords, state);
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
        }
    }
}
