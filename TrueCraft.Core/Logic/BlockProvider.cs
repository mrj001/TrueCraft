using System;
using System.Collections.Generic;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Server;
using TrueCraft.Core.Logic.Blocks;
using System.Linq;
using fNbt;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Physics;
using System.Xml;
using static System.Formats.Asn1.AsnWriter;

namespace TrueCraft.Core.Logic
{
    /// <summary>
    /// Provides common implementations of block logic.
    /// </summary>
    public class BlockProvider : IItemProvider, IBlockProvider
    {
        private static List<short> _metadata;

        private readonly byte _id;
        private readonly double _blastResistance;
        private readonly double _hardness;
        private readonly byte _luminance;
        private readonly bool _opaque;
        private readonly bool _renderOpaque;
        private readonly bool _flammable;
        private readonly byte _lightOpacity;

        // TODO: This is a function of metadata.
        private readonly SoundEffectClass _soundEffect;

        private readonly ToolMaterial _toolMaterials;
        private readonly ToolType _tools;

        // TODO: this is a function of metadata.
        private readonly List<BoundingBox>? _collisionBoxes;

        // TODO: this is a function of metadata.
        private readonly BoundingBox? _collisionBox;

        // TODO: this is a function of metadata.
        private readonly BoundingBox _interactiveBoundingBox;

        // TODO: this is a function of metadata
        private readonly Tuple<int, int> _textureMap;

        private const string IdNodeName = "id";
        private const string BlastResistanceNodeName = "blastresistance";
        private const string HardnessNodeName = "hardness";
        private const string LuminanceNodeName = "luminance";
        private const string OpaqueNodeName = "opaque";
        private const string RenderOpaqueNodeName = "renderopaque";
        private const string LightOpacityNodeName = "lightopacity";
        private const string FlammableNodeName = "flammable";
        private const string BlockMetadataNodeName = "blockmetadata";
        private const string MetadataNodeName = "metadata";
        private const string ValueNodeName = "value";
        private const string DisplayNameNodeName = "displayname";
        private const string SoundEffectNodeName = "soundeffect";
        private const string DropsNodeName = "drops";
        private const string BoundingBoxNodeName = "boundingbox";
        private const string InteractiveBoundingBoxNodeName = "interactiveboundingbox";

        static BlockProvider()
        {
            _metadata = new List<short>(1);
            _metadata.Add(0);
        }

#pragma warning disable CS8618
        // This constructor supports unit testing.  Do not use outside of
        // unit testing.
        protected BlockProvider()
        {

        }
#pragma warning restore CS8618

        public BlockProvider(XmlNode node)
        {
            XmlNode? idNode = node.FirstChild;
            if (idNode is null || idNode.LocalName != IdNodeName)
                throw new ArgumentException($"Missing {IdNodeName} Node.");
            _id = byte.Parse(idNode.InnerText);

            XmlNode? blastResistanceNode = idNode.NextSibling;
            if (blastResistanceNode is null || blastResistanceNode.LocalName != BlastResistanceNodeName)
                throw new ArgumentException($"Missing {BlastResistanceNodeName} Node.");
            _blastResistance = double.Parse(blastResistanceNode.InnerText);

            XmlNode? hardnessNode = blastResistanceNode.NextSibling;
            if (hardnessNode is null || hardnessNode.LocalName != HardnessNodeName)
                throw new ArgumentException($"Missing {HardnessNodeName} Node.");
            _hardness = double.Parse(hardnessNode.InnerText);

            XmlNode? luminanceNode = hardnessNode.NextSibling;
            if (luminanceNode is null || luminanceNode.LocalName != LuminanceNodeName)
                throw new ArgumentException($"Missing {LuminanceNodeName} Node.");
            _luminance = byte.Parse(luminanceNode.InnerText);

            XmlNode? opaqueNode = luminanceNode.NextSibling;
            if (opaqueNode is null || opaqueNode.LocalName != OpaqueNodeName)
                throw new ArgumentException($"Missing {OpaqueNodeName} Node.");
            _opaque = bool.Parse(opaqueNode.InnerText);

            XmlNode? renderOpaqueNode = opaqueNode.NextSibling;
            if (renderOpaqueNode is null || renderOpaqueNode.LocalName != RenderOpaqueNodeName)
                throw new ArgumentException($"Missing {RenderOpaqueNodeName} Node.");
            _renderOpaque = bool.Parse(renderOpaqueNode.InnerText);

            XmlNode? lightOpacityNode = renderOpaqueNode.NextSibling;
            if (lightOpacityNode is null || lightOpacityNode.LocalName != LightOpacityNodeName)
                throw new ArgumentException($"Missing {LightOpacityNodeName} Node.");
            _lightOpacity = byte.Parse(lightOpacityNode.InnerText);

            XmlNode? flammableNode = lightOpacityNode.NextSibling;
            if (flammableNode is null || flammableNode.LocalName != FlammableNodeName)
                throw new ArgumentException($"Missing {FlammableNodeName} Node.");
            _flammable = bool.Parse(flammableNode.InnerText);

            XmlNode? metadataNode = flammableNode.NextSibling;
            if (metadataNode is null || metadataNode.LocalName != BlockMetadataNodeName)
                throw new ArgumentException($"Missing {BlockMetadataNodeName} Node.");

            XmlNode? megadatumNode = metadataNode.FirstChild;
            if (megadatumNode is null || megadatumNode.LocalName != MetadataNodeName)
                throw new ArgumentException($"Missing {MetadataNodeName} Node.");

            // TODO Parse multiple Metadata Nodes
            XmlNode? valueNode = megadatumNode.FirstChild;
            if (valueNode is null || valueNode.LocalName != ValueNodeName)
                throw new ArgumentException($"Missing {ValueNodeName} Node.");
            // TODO: this value will be the key used to lookup values that are a
            //       function of metadata.

            XmlNode? displayNameNode = valueNode.NextSibling;
            if (displayNameNode is null || displayNameNode.LocalName != DisplayNameNodeName)
                throw new ArgumentException($"Missing {DisplayNameNodeName} Node.");

            XmlNode? soundEffectNode = displayNameNode.NextSibling;
            if (soundEffectNode is null || soundEffectNode.LocalName != SoundEffectNodeName)
                throw new ArgumentException($"Missing {SoundEffectNodeName} Node.");
            _soundEffect = ParseSoundEffect(soundEffectNode.InnerText);

            XmlNode? dropsNode = soundEffectNode.NextSibling;
            if (dropsNode is null || dropsNode.LocalName != DropsNodeName)
                throw new ArgumentException($"Missing {DropsNodeName} Node.");
            // TODO Parse Drops

            XmlNode? interactiveBoundingBoxNode = dropsNode.NextSibling;
            if (interactiveBoundingBoxNode is null || interactiveBoundingBoxNode.LocalName != InteractiveBoundingBoxNodeName)
                throw new ArgumentException($"Missing {InteractiveBoundingBoxNodeName} Node.");

            XmlNode? boundingBoxNode = interactiveBoundingBoxNode.NextSibling;
            XmlNode? modelNode;
            if (boundingBoxNode is not null && boundingBoxNode.LocalName == BoundingBoxNodeName)
            {
                _collisionBoxes = new List<BoundingBox>();
                foreach(XmlNode box in boundingBoxNode.ChildNodes)
                    _collisionBoxes.Add(new BoundingBox(box));
                _collisionBox = BoundingBox.Union(_collisionBoxes);
                modelNode = boundingBoxNode.NextSibling;
            }
            else
            {
                _collisionBoxes = null;
                _collisionBox = null;
                modelNode = boundingBoxNode;
            }

            // Parse Model
            // TODO

            // TODO: parse texture node.
            _textureMap = new Tuple<int, int>(0, 0);
        }

        private static SoundEffectClass ParseSoundEffect(string effect)
        {
            switch(effect)
            {
                case "None":
                    return SoundEffectClass.None;

                case "Cloth":
                    return SoundEffectClass.Cloth;

                case "Grass":
                    return SoundEffectClass.Grass;

                case "Gravel":
                    return SoundEffectClass.Gravel;

                case "Sand":
                    return SoundEffectClass.Sand;

                case "Snow":
                    return SoundEffectClass.Snow;

                case "Stone":
                    return SoundEffectClass.Stone;

                case "Wood":
                    return SoundEffectClass.Wood;

                case "Glass":
                    return SoundEffectClass.Glass;

                default:
                    throw new ArgumentException("Unable to parse '{effect}' into a SoundEffectClass.");
            }
        }

        public virtual void BlockLeftClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            var coords = descriptor.Coordinates + MathHelper.BlockFaceToCoordinates(face);
            if (dimension.IsValidPosition(coords) && dimension.GetBlockID(coords) == (byte)BlockIDs.Fire)
                dimension.SetBlockID(coords, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="face"></param>
        /// <param name="dimension"></param>
        /// <param name="user"></param>
        /// <returns>True if the right-click has been handled; false otherwise.</returns>
        public virtual bool BlockRightClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            return true;
        }

        public virtual void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            // This space intentionally left blank
        }

        public virtual void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            GenerateDropEntity(descriptor, dimension, user.Server, user.SelectedItem);
            dimension.SetBlockID(descriptor.Coordinates, 0);
        }

        public virtual void GenerateDropEntity(BlockDescriptor descriptor, IDimension dimension, IMultiplayerServer server, ItemStack item)
        {
            ServerOnly.Assert();

            IEntityManager entityManager = ((IDimensionServer)dimension).EntityManager;
            ItemStack[] items = new ItemStack[0];
            ToolType type = ToolType.None;
            ToolMaterial material = ToolMaterial.None;
            IItemProvider? held = dimension.ItemRepository.GetItemProvider(item.ID);

            if (held is ToolItem)
            {
                ToolItem tool = (ToolItem)held;
                material = tool.Material;
                type = tool.ToolType;
            }

            if ((EffectiveTools & type) > 0)
            {
                if ((EffectiveToolMaterials & material) > 0)
                    items = GetDrop(descriptor, item);
            }

            foreach (var i in items)
            {
                if (i.Empty) continue;
                IEntity entity = new ItemEntity(dimension, entityManager,
                    (Vector3)descriptor.Coordinates + new Vector3(0.5), i);
                entityManager.SpawnEntity(entity);
            }
        }

        public virtual bool IsSupported(IDimension dimension, BlockDescriptor descriptor)
        {
            ServerOnly.Assert();

            var support = GetSupportDirection(descriptor);
            if (support != Vector3i.Zero)
            {
                var supportingBlock = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(descriptor.Coordinates + support));
                if (!supportingBlock.Opaque)
                    return false;
            }
            return true;
        }

        public virtual void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            ServerOnly.Assert();

            if (!IsSupported(dimension, descriptor))
            {
                GenerateDropEntity(descriptor, dimension, server, ItemStack.EmptyStack);
                dimension.SetBlockID(descriptor.Coordinates, 0);
            }
        }

        // TODO: Fix this method signature.  passing in a BlockDescriptor opens the
        //   possibility it may have a different ID than this BlockProvider.  What is
        //   the meaning of, say, passing in a Torch BlockDescriptor, if this is a StoneBlock?
        //   The only part of the BlockDescriptor to be used appears to be the metadata.
        //   Also: item means the item held by the player breaking the block.
        protected virtual ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new[] { new ItemStack(descriptor.ID, 1, descriptor.Metadata) };
        }

        public virtual void ItemUsedOnEntity(ItemStack item, IEntity usedOn, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            // This space intentionally left blank
        }

        public virtual void ItemUsedOnNothing(ItemStack item, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            // This space intentionally left blank
        }

        public static readonly byte[] Overwritable =
        {
            (byte)BlockIDs.Air,
            (byte)BlockIDs.Water,
            (byte)BlockIDs.WaterStationary,
            (byte)BlockIDs.Lava,
            (byte)BlockIDs.LavaStationary,
            (byte)BlockIDs.Snow
        };

        public virtual void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            var old = dimension.GetBlockData(coordinates);
            if (!Overwritable.Any(b => b == old.ID))
            {
                coordinates += MathHelper.BlockFaceToCoordinates(face);
                old = dimension.GetBlockData(coordinates);
                if (!Overwritable.Any(b => b == old.ID))
                    return;
            }

            // Test for entities that block placing the block
            BoundingBox? box = GetCollisionBox((byte)item.Metadata);
            if (box.HasValue)
            {
                IEntityManager em = ((IDimensionServer)dimension).EntityManager;
                IList<IEntity> entities = em.EntitiesInRange((Vector3)coordinates, 3);
                box = box.Value.OffsetBy((Vector3)coordinates);
                foreach (IEntity entity in entities)
                    if (entity is not null &&
                        !(typeof(ItemEntity).IsAssignableFrom(entity.GetType())) &&
                        entity.BoundingBox.Intersects(box.Value))
                        return;
            }

            // Place block
            dimension.SetBlockID(coordinates, ID);
            dimension.SetMetadata(coordinates, (byte)item.Metadata);

            BlockPlaced(dimension.GetBlockData(coordinates), face, dimension, user);

            // TODO: How could the block we just placed be unsupported?
            if (!IsSupported(dimension, dimension.GetBlockData(coordinates)))
                dimension.SetBlockData(coordinates, old);
            else
            {
                item.Count--;
                user.Inventory[user.SelectedSlot].Item = item;
            }
        }

        /// <inheritdoc />
        public virtual void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            ServerOnly.Assert();

            // This space intentionally left blank
        }

        public virtual void TileEntityLoadedForClient(BlockDescriptor descriptor, IDimension dimension, NbtCompound entity, IRemoteClient client)
        {
            ServerOnly.Assert();

            // This space intentionally left blank
        }

        short IItemProvider.ID
        {
            get
            {
                return ID;
            }
        }

        /// <summary>
        /// The ID of the block.
        /// </summary>
        public virtual byte ID { get => _id; }

        public virtual Tuple<int, int>? GetIconTexture(byte metadata)
        {
            return null; // Blocks are rendered in 3D
        }

        /// <inheritdoc />
        public virtual IEnumerable<short> VisibleMetadata => _metadata;

        public virtual Vector3i GetSupportDirection(BlockDescriptor descriptor)
        {
            return Vector3i.Zero;
        }

        public virtual SoundEffectClass SoundEffect { get => _soundEffect; }

        /// <summary>
        /// The maximum amount that can be in a single stack of this block.
        /// </summary>
        public virtual sbyte MaximumStack { get { return 64; } }

        /// <summary>
        /// How resist the block is to explosions.
        /// </summary>
        public virtual double BlastResistance { get => _blastResistance; }

        /// <summary>
        /// How resist the block is to player mining/digging.
        /// </summary>
        public virtual double Hardness { get => _hardness; }

        /// <summary>
        /// The light level emitted by the block. 0 - 15
        /// </summary>
        public virtual byte Luminance { get => _luminance; }

        /// <summary>
        /// Whether or not the block is opaque
        /// </summary>
        public virtual bool Opaque { get => _opaque; }

        /// <summary>
        /// Whether or not the block is rendered opaque
        /// </summary>
        public virtual bool RenderOpaque { get => _renderOpaque; }

        public virtual bool Flammable { get => _flammable; }

        /// <summary>
        /// The amount removed from the light level as it passes through this block.
        /// 255 - Let no light pass through(this may change)
        /// Notes:
        /// - This isn't needed for opaque blocks
        /// - This is needed since some "partial" transparent blocks remove more than 1 level from light passing through such as Ice.
        /// </summary>
        public virtual byte LightOpacity { get => _lightOpacity; }

        /// <inheritdoc />
        public virtual string GetDisplayName(short metadata)
        {
            // TODO: fix
            return string.Empty;
        }

        public virtual ToolMaterial EffectiveToolMaterials { get => _toolMaterials; }

        public virtual ToolType EffectiveTools { get => _tools; }

        public virtual Tuple<int, int>? GetTextureMap(byte metadata)
        {
            // TODO: must be a function of metadata
            return _textureMap;
        }

        /// <inheritdoc />
        public virtual IEnumerable<BoundingBox>? GetCollisionBoxes(byte metadata)
        {
            return _collisionBoxes;
        }

        /// <inheritdoc />
        public virtual BoundingBox? GetCollisionBox(byte metadata)
        {
            return _collisionBox;
        }

        public virtual BoundingBox? InteractiveBoundingBox { get => _interactiveBoundingBox; }

        /// <summary>
        /// Gets the time required to mine the given block with the given item.
        /// </summary>
        /// <returns>The harvest time in milliseconds.</returns>
        /// <param name="serviceLocator">The Service Locator.</param>
        /// <param name="blockId">Block identifier.</param>
        /// <param name="itemId">Item identifier of the item currently held by the Player.</param>
        /// <param name="damage">Damage sustained by the item.</param>
        public static int GetHarvestTime(IServiceLocator serviceLocator, byte blockId, short itemId, out short damage)
        {
            // Reference:
            // http://minecraft.gamepedia.com/index.php?title=Breaking&oldid=138286

            damage = 0;

            IBlockProvider block = serviceLocator.BlockRepository.GetBlockProvider(blockId);
            IItemProvider? item = serviceLocator.ItemRepository.GetItemProvider(itemId);

            double hardness = block.Hardness;
            if (hardness == -1)
                return -1;

            double time = hardness * 1.5;

            var tool = ToolType.None;
            var material = ToolMaterial.None;

            if (item is ToolItem)
            {
                ToolItem toolItem = (ToolItem)item;
                tool = toolItem.ToolType;
                material = toolItem.Material;

                if ((block.EffectiveTools & tool) == 0 || (block.EffectiveToolMaterials & material) == 0)
                {
                    time *= 3.33; // Add time for ineffective tools
                }
                if (material != ToolMaterial.None)
                {
                    switch (material)
                    {
                        case ToolMaterial.Wood:
                            time /= 2;
                            break;
                        case ToolMaterial.Stone:
                            time /= 4;
                            break;
                        case ToolMaterial.Iron:
                            time /= 6;
                            break;
                        case ToolMaterial.Diamond:
                            time /= 8;
                            break;
                        case ToolMaterial.Gold:
                            time /= 12;
                            break;
                    }
                }
                damage = 1;
                if (tool == ToolType.Shovel || tool == ToolType.Axe || tool == ToolType.Pickaxe)
                {
                    damage = (short)(hardness != 0 ? 1 : 0);
                }
                else if (tool == ToolType.Sword)
                {
                    damage = (short)(hardness != 0 ? 2 : 0);
                    time /= 1.5;
                    if (blockId == (byte)BlockIDs.Cobweb)
                        time /= 1.5;
                }
                else if (tool == ToolType.Hoe)
                    damage = 0; // What? This doesn't seem right
                else if (item is ShearsItem)
                {
                    if (blockId == (byte)BlockIDs.Wool)
                        time /= 5;
                    else if (block is LeavesBlock || blockId == (byte)BlockIDs.Cobweb)
                        time /= 15;
                    if (block is LeavesBlock || blockId == (byte)BlockIDs.Cobweb || block is TallGrassBlock)
                        damage = 1;
                    else
                        damage = 0;
                }
            }
            return (int)(time * 1000);
        }
    }
}