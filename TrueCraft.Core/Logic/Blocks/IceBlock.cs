using System;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class IceBlock : BlockProvider
    {
        public static readonly byte BlockID = 0x4F;
        
        public override byte ID { get { return 0x4F; } }
        
        public override double BlastResistance { get { return 2.5; } }

        public override double Hardness { get { return 0.5; } }

        public override byte Luminance { get { return 0; } }

        public override bool Opaque { get { return false; } }

        public override byte LightOpacity { get { return 2; } }
        
        public override string GetDisplayName(short metadata)
        {
            return "Ice";
        }

        public override SoundEffectClass SoundEffect
        {
            get
            {
                return SoundEffectClass.Glass;
            }
        }

        public override Tuple<int, int> GetTextureMap(byte metadata)
        {
            return new Tuple<int, int>(3, 4);
        }

        public override void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetBlockID(descriptor.Coordinates, WaterBlock.BlockID);
            dimension.BlockRepository.GetBlockProvider(WaterBlock.BlockID).BlockPlaced(descriptor, face, dimension, user);
        }
    }
}