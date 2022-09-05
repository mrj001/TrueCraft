using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking;
using System.Collections.Generic;
using System.Linq;

namespace TrueCraft.Core.Logic.Blocks
{
    public abstract class FluidBlock : BlockProvider
    {
        // Fluids in Minecraft propegate according to a set of rules as cellular automata.
        // Source blocks start at zero and each block progressively further from the source
        // is one greater than the largest value nearby. When they reach
        // MaximumFluidDepletion, the fluid stops propgetating.

        public FluidBlock(XmlNode node) : base(node)
        {
        }

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new ItemStack[0];
        }

        protected abstract double SecondsBetweenUpdates { get; }
        protected abstract byte MaximumFluidDepletion { get; }
        protected abstract byte FlowingID { get; }
        protected abstract byte StillID { get; }

        protected virtual bool AllowSourceCreation { get { return true; } }

        private static readonly Vector3i[] Neighbors =
            {
                Vector3i.North,
                Vector3i.South,
                Vector3i.East,
                Vector3i.West
            };

        /// <summary>
        /// Represents a block that the currently updating fluid block is able to flow outwards into.
        /// </summary>
        protected struct LiquidFlow
        {
            public LiquidFlow(GlobalVoxelCoordinates targetBlock, byte level)
            {
                TargetBlock = targetBlock;
                Level = level;
            }

            /// <summary>
            /// The block to be filled with fluid.
            /// </summary>
            public GlobalVoxelCoordinates TargetBlock;

            /// <summary>
            /// The fluid level to fill the target block with.
            /// </summary>
            public byte Level;
        }

        private void ScheduleNextEvent(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            IChunk? chunk = dimension.GetChunk(coords);
            if (chunk is null || dimension.GetBlockID(coords) == StillID)
                return;

            server.Scheduler.ScheduleEvent("fluid", chunk,
                TimeSpan.FromSeconds(SecondsBetweenUpdates), (_server) =>
                AutomataUpdate(_server, dimension, coords));
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            if (ID == FlowingID)
                ScheduleNextEvent(user.Server, dimension, descriptor.Coordinates);
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (ID == StillID)
            {
                var outward = DetermineOutwardFlow(dimension, descriptor.Coordinates);
                var inward = DetermineInwardFlow(dimension, descriptor.Coordinates);
                if (outward.Length != 0 || inward != descriptor.Metadata)
                {
                    dimension.SetBlockID(descriptor.Coordinates, FlowingID);
                    ScheduleNextEvent(server, dimension, descriptor.Coordinates);
                }
            }
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            ScheduleNextEvent(server, dimension, coordinates);
        }

        private void AutomataUpdate(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            IChunk? chunk = dimension.GetChunk(coords);
            if (chunk is null || (dimension.GetBlockID(coords) != FlowingID && dimension.GetBlockID(coords) != StillID))
                return;
            server.BlockUpdatesEnabled = false;
            var again = DoAutomata(server, dimension, coords);
            server.BlockUpdatesEnabled = true;
            if (again)
            {
                server.Scheduler.ScheduleEvent("fluid", chunk,
                    TimeSpan.FromSeconds(SecondsBetweenUpdates), (_server) =>
                    AutomataUpdate(_server, dimension, coords));
            }
        }

        public bool DoAutomata(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            var previousLevel = dimension.GetMetadata(coords);

            var inward = DetermineInwardFlow(dimension, coords);
            var outward = DetermineOutwardFlow(dimension, coords);

            if (outward.Length == 1 && outward[0].TargetBlock == coords + Vector3i.Down)
            {
                // Exit early if we have placed a fluid block beneath us (and we aren't a source block)
                FlowOutward(dimension, outward[0], server);
                if (previousLevel != 0)
                    return true;
            }

            // Process inward flow
            if (inward > MaximumFluidDepletion)
            {
                dimension.SetBlockID(coords, 0);
                return true;
            }
            dimension.SetMetadata(coords, inward);
            if (inward == 0 && previousLevel != 0)
            {
                // Exit early if we have become a source block
                return true;
            }

            // Process outward flow
            for (int i = 0; i < outward.Length; i++)
                FlowOutward(dimension, outward[i], server);
            // Set our block to still fluid if we are done spreading.
            if (outward.Length == 0 && inward == previousLevel)
            {
                dimension.SetBlockID(coords, StillID);
                return false;
            }
            return true;
        }

        private void FlowOutward(IDimension dimension, LiquidFlow target, IMultiplayerServer server)
        {
            IChunk? chunk = dimension.GetChunk(target.TargetBlock);
            if (chunk is null)
                return;

            // For each block we can flow into, generate an item entity if appropriate
            var provider = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(target.TargetBlock));
            provider.GenerateDropEntity(new BlockDescriptor { Coordinates = target.TargetBlock, ID = provider.ID }, dimension, server, ItemStack.EmptyStack);
            // And overwrite the block with a new fluid block.
            dimension.SetBlockID(target.TargetBlock, FlowingID);
            dimension.SetMetadata(target.TargetBlock, target.Level);
            server.Scheduler.ScheduleEvent("fluid", chunk,
                TimeSpan.FromSeconds(SecondsBetweenUpdates),
                s => AutomataUpdate(s, dimension, target.TargetBlock));
            if (FlowingID == (byte)BlockIDs.Lava)
            {
                (dimension.BlockRepository.GetBlockProvider((byte)BlockIDs.Fire) as FireBlock)?.ScheduleUpdate(
                    server, dimension, dimension.GetBlockData(target.TargetBlock));
            }
        }

        /// <summary>
        /// Examines neighboring blocks and determines the new fluid level that this block should adopt.
        /// </summary>
        protected byte DetermineInwardFlow(IDimension dimension, GlobalVoxelCoordinates coords)
        {
            var currentLevel = dimension.GetMetadata(coords);
            var up = dimension.GetBlockID(coords + Vector3i.Up);
            if (up == FlowingID || up == StillID) // Check for fluid above us
                return currentLevel;
            else
            {
                if (currentLevel != 0)
                {
                    byte highestNeighboringFluid = 15;
                    int neighboringSourceBlocks = 0;
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        var nId = dimension.GetBlockID(coords + Neighbors[i]);
                        if (nId == FlowingID || nId == StillID)
                        {
                            var neighborLevel = dimension.GetMetadata(coords + Neighbors[i]);
                            if (neighborLevel < highestNeighboringFluid)
                                highestNeighboringFluid = neighborLevel;
                            if (neighborLevel == 0)
                                neighboringSourceBlocks++;
                        }
                    }
                    if (neighboringSourceBlocks >= 2 && AllowSourceCreation)
                        currentLevel = 0;
                    if (highestNeighboringFluid > 0)
                        currentLevel = (byte)(highestNeighboringFluid + 1);
                }
            }
            return currentLevel;
        }

        /// <summary>
        /// Produces a list of outward flow targets that this block may flow towards.
        /// </summary>
        protected LiquidFlow[] DetermineOutwardFlow(IDimension dimension, GlobalVoxelCoordinates coords)
        {
            // The maximum distance we will search for lower ground to flow towards
            const int dropCheckDistance = 5;

            var outwardFlow = new List<LiquidFlow>(5);

            var currentLevel = dimension.GetMetadata(coords);
            var blockBelow = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(coords + Vector3i.Down));
            if (blockBelow.Hardness == 0 && blockBelow.ID != FlowingID && blockBelow.ID != StillID)
            {
                outwardFlow.Add(new LiquidFlow(coords + Vector3i.Down, 1));
                if (currentLevel != 0)
                    return outwardFlow.ToArray();
            }

            if (currentLevel < MaximumFluidDepletion)
            {
                // This code is responsible for seeking out candidates for flowing towards.
                // Fluid in Minecraft will flow in the direction of the nearest drop-off where
                // there is at least one block removed on the Y axis.
                // It will flow towards several equally strong candidates at once.

                var candidateFlowPoints = new List<Vector3i>(4);
                Vector3i furthestPossibleCandidate = new Vector3i(dropCheckDistance + 1, -1, dropCheckDistance + 1);

                var nearestCandidate = furthestPossibleCandidate;
                for (int x = -dropCheckDistance; x < dropCheckDistance; x++)
                {
                    for (int z = -dropCheckDistance; z < dropCheckDistance; z++)
                    {
                        if (Math.Abs(z) + Math.Abs(x) > dropCheckDistance)
                            continue;
                        Vector3i check = new Vector3i(x, -1, z);
                        var c = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(check + coords));
                        if (c.Hardness == 0)
                        {
                            if (!LineOfSight(dimension, check + coords, coords))
                                continue;
                            if (coords.DistanceTo(check + coords) == coords.DistanceTo(nearestCandidate + coords))
                                candidateFlowPoints.Add(check);
                            if (coords.DistanceTo(check + coords) < coords.DistanceTo(nearestCandidate + coords))
                            {
                                candidateFlowPoints.Clear();
                                nearestCandidate = check;
                            }
                        }
                    }
                }
                if (nearestCandidate == furthestPossibleCandidate)
                {
                    candidateFlowPoints.Add(new Vector3i(-dropCheckDistance - 1, -1, dropCheckDistance + 1));
                    candidateFlowPoints.Add(new Vector3i(dropCheckDistance + 1, -1, -dropCheckDistance - 1));
                    candidateFlowPoints.Add(new Vector3i(-dropCheckDistance - 1, -1, -dropCheckDistance - 1));
                }
                candidateFlowPoints.Add(nearestCandidate);

                // For each candidate, determine if we are actually capable of flowing towards it.
                // We are able to flow through blocks with a hardness of zero, but no others. We are
                // not able to flow through established fluid blocks.
                for (int i = 0; i < candidateFlowPoints.Count; i++)
                {
                    var location = candidateFlowPoints[i];
                    location = location.Clamp(1);

                    var xCoordinateCheck = new GlobalVoxelCoordinates(coords.X + location.X, coords.Y, coords.Z);
                    var zCoordinateCheck = new GlobalVoxelCoordinates(coords.X, coords.Y, coords.Z + location.Z);

                    var xID = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(xCoordinateCheck));
                    var zID = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(zCoordinateCheck));

                    if (xID.Hardness == 0 && xID.ID != FlowingID && xID.ID != StillID)
                    {
                        if (outwardFlow.All(f => f.TargetBlock != xCoordinateCheck))
                            outwardFlow.Add(new LiquidFlow(xCoordinateCheck, (byte)(currentLevel + 1)));
                    }

                    if (zID.Hardness == 0 && zID.ID != FlowingID && zID.ID != StillID)
                    {
                        if (outwardFlow.All(f => f.TargetBlock != zCoordinateCheck))
                            outwardFlow.Add(new LiquidFlow(zCoordinateCheck, (byte)(currentLevel + 1)));
                    }
                }

                // Occasionally, there are scenarios where the nearest candidate hole is not acceptable, but
                // there is space immediately next to the block. We should fill that space.
                if (outwardFlow.Count == 0 && blockBelow.ID != FlowingID && blockBelow.ID != StillID)
                {
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        var b = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(coords + Neighbors[i]));
                        if (b.Hardness == 0 && b.ID != StillID && b.ID != FlowingID)
                            outwardFlow.Add(new LiquidFlow(Neighbors[i] + coords, (byte)(currentLevel + 1)));
                    }
                }
            }
            return outwardFlow.ToArray();
        }

        /// <summary>
        /// Returns true if the given candidate coordinate has a line-of-sight to the given target coordinate.
        /// </summary>
        private bool LineOfSight(IDimension dimension, GlobalVoxelCoordinates candidate, GlobalVoxelCoordinates target)
        {
            // TODO: What does LineOfSight have to do with fluid flow?
            //       You can have an uphill line of sight, but fluid won't flow there.
            // TODO: why is the candidate elevated by one block?
            candidate = new GlobalVoxelCoordinates(candidate.X, candidate.Y + 1, candidate.Z);
            Vector3i direction = (target - candidate).Clamp(1);

            // TODO: In what sense is a non-zero hardness block ANYWHERE in the
            //       rectangle with candidate and target at its opposite corners
            //       blocking the line of sight?
            do
            {
                int z = candidate.Z;
                do
                {
                    var p = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(candidate));
                    // TODO: what does Hardness have to do with Line of Sight?  Not opaque?
                    //       Are these blocks that break when water flows in?
                    if (p.Hardness != 0)
                        return false;
                    candidate = new GlobalVoxelCoordinates(candidate.X, candidate.Y, candidate.Z + direction.Z);
                } while (target.Z != candidate.Z);
                candidate = new GlobalVoxelCoordinates(candidate.X + direction.X, candidate.Y, z);
            } while (target.X != candidate.X);
            return true;
        }
    }
}