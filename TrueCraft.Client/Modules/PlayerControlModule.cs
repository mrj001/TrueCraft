using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TrueCraft.Client.Input;
using Matrix = Microsoft.Xna.Framework.Matrix;
using TVector3 = TrueCraft.Core.Vector3;
using XVector3 = Microsoft.Xna.Framework.Vector3;
using XMathHelper = Microsoft.Xna.Framework.MathHelper;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.World;
using TrueCraft.Core;

namespace TrueCraft.Client.Modules
{
    public class PlayerControlModule : InputModule
    {
        private readonly IServiceLocator _serviceLocator;

        private readonly TrueCraftGame _game;
        private DateTime _nextAnimation;
        private XVector3 _delta;
        private bool _capture;
        private bool _digging;
        private GamePadState _gamePadState;
        private bool _ignoreNextUpdate;

        public PlayerControlModule(IServiceLocator serviceLocator, TrueCraftGame game)
        {
            _serviceLocator = serviceLocator;

            _game = game;
            _capture = true;
            _digging = false;
            _game.StartDigging = DateTime.MinValue;
            _game.EndDigging = DateTime.MaxValue;
            _game.TargetBlock = null;
            _nextAnimation = DateTime.MaxValue;
            _gamePadState = GamePad.GetState(PlayerIndex.One);
        }

        public bool IgnoreNextUpdate
        {
            get => _ignoreNextUpdate;
            set => _ignoreNextUpdate = value;
        }

        public override bool KeyDown(GameTime gameTime, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                // Exit game
                case Keys.Escape:
                    Process.GetCurrentProcess().Kill();
                    return true;

                // Take a screenshot.
                case Keys.F2:
                    _game.TakeScreenshot();
                    return true;

                // Move to the left.
                case Keys.A:
                case Keys.Left:
                    _delta += XVector3.Left;
                    return true;

                // Move to the right.
                case Keys.D:
                case Keys.Right:
                    _delta += XVector3.Right;
                    return true;

                // Move forwards.
                case Keys.W:
                case Keys.Up:
                    _delta += XVector3.Forward;
                    return true;

                // Move backwards.
                case Keys.S:
                case Keys.Down:
                    _delta += XVector3.Backward;
                    return true;

                case Keys.I:
                    _game.Client.Position = _game.Client.Position.Floor();
                    return true;

                case Keys.Tab:
                    _capture = !_capture;
                    return true;

                case Keys.E:
                    _game.Client.CurrentWindow = _game.Client.InventoryWindow;
                    return true;

                case Keys.Space:
                    if (_game.Client.Physics.IsGrounded(_game.Client))
                        _game.Client.Velocity += TrueCraft.Core.Vector3.Up * GameConstants.JumpVelocity;
                    return true;

                case Keys.D1:
                case Keys.NumPad1:
                    _game.Client.HotbarSelection = 0;
                    return true;

                case Keys.D2:
                case Keys.NumPad2:
                    _game.Client.HotbarSelection = 1;
                    return true;

                case Keys.D3:
                case Keys.NumPad3:
                    _game.Client.HotbarSelection = 2;
                    return true;

                case Keys.D4:
                case Keys.NumPad4:
                    _game.Client.HotbarSelection = 3;
                    return true;

                case Keys.D5:
                case Keys.NumPad5:
                    _game.Client.HotbarSelection = 4;
                    return true;

                case Keys.D6:
                case Keys.NumPad6:
                    _game.Client.HotbarSelection = 5;
                    return true;

                case Keys.D7:
                case Keys.NumPad7:
                    _game.Client.HotbarSelection = 6;
                    return true;

                case Keys.D8:
                case Keys.NumPad8:
                    _game.Client.HotbarSelection = 7;
                    return true;

                case Keys.D9:
                case Keys.NumPad9:
                    _game.Client.HotbarSelection = 8;
                    return true;
            }
            return false;
        }

        public override bool KeyUp(GameTime gameTime, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                // Stop moving to the left.
                case Keys.A:
                case Keys.Left:
                    _delta -= XVector3.Left;
                    return true;

                // Stop moving to the right.
                case Keys.D:
                case Keys.Right:
                    _delta -= XVector3.Right;
                    return true;

                // Stop moving forwards.
                case Keys.W:
                case Keys.Up:
                    _delta -= XVector3.Forward;
                    return true;

                // Stop moving backwards.
                case Keys.S:
                case Keys.Down:
                    _delta -= XVector3.Backward;
                    return true;
            }
            return false;
        }

        public override bool GamePadButtonDown(GameTime gameTime, GamePadButtonEventArgs e)
        {
            var selected = _game.Client.HotbarSelection;
            switch (e.Button)
            {
                case Buttons.LeftShoulder:
                    selected--;
                    if (selected < 0)
                        selected = 8;
                    if (selected > 8)
                        selected = 0;
                    _game.Client.HotbarSelection = selected;
                    break;
                case Buttons.RightShoulder:
                    selected++;
                    if (selected < 0)
                        selected = 8;
                    if (selected > 8)
                        selected = 0;
                    _game.Client.HotbarSelection = selected;
                    break;
                case Buttons.A:
                    if (Math.Floor(_game.Client.Position.Y) == _game.Client.Position.Y)
                        _game.Client.Velocity += TVector3.Up * 0.3;
                    break;
            }
            return false;
        }

        public override bool MouseScroll(GameTime gameTime, MouseScrollEventArgs e)
        {
            var selected = _game.Client.HotbarSelection;
            selected += e.DeltaValue > 0 ? -1 : 1;
            if (selected < 0)
                selected = 8;
            if (selected > 8)
                selected = 0;
            _game.Client.HotbarSelection = selected;
            return true;
        }
        
        public override bool MouseMove(GameTime gameTime, MouseMoveEventArgs e)
        {
            if (!_capture)
                return false;
            if (_ignoreNextUpdate)
            {
                _ignoreNextUpdate = false;
                return true;
            }
            var centerX = _game.GraphicsDevice.Viewport.Width / 2;
            var centerY = _game.GraphicsDevice.Viewport.Height / 2;

            if (e.X < 10 || e.X > _game.GraphicsDevice.Viewport.Width - 10 ||
                e.Y < 10 || e.Y > _game.GraphicsDevice.Viewport.Height - 10)
            {
                Mouse.SetPosition(centerX, centerY);
                _ignoreNextUpdate = true;
                return true;
            }

            var look = new Vector2((-e.DeltaX), (-e.DeltaY))
                * (float)(gameTime.ElapsedGameTime.TotalSeconds * 30);

            if (TrueCraft.Core.UserSettings.Local.InvertedMouse)
                look.Y = -look.Y;
            _game.Client.Yaw -= look.X;
            _game.Client.Pitch -= look.Y;
            _game.Client.Yaw %= 360;
            _game.Client.Pitch = XMathHelper.Clamp(_game.Client.Pitch, -89.9f, 89.9f);

            return true;
        }

        public override bool MouseButtonDown(GameTime gameTime, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    _digging = true;
                    return true;
                case MouseButton.Right:
                    if (_game.HighlightedBlock is null)
                        return true;

                    ItemStack heldItem = _game.Client.Hotbar[_game.Client.HotbarSelection].Item;
                    _game.Client.QueuePacket(new PlayerBlockPlacementPacket(
                        _game.HighlightedBlock.X, (sbyte)_game.HighlightedBlock.Y, _game.HighlightedBlock.Z,
                        _game.HighlightedBlockFace, heldItem));
                    return true;
            }
            return false;
        }

        private void BeginDigging(GlobalVoxelCoordinates target)
        {
            // TODO: Adjust digging time to compensate for latency
            var block = _game.Client.Dimension.GetBlockID(target);
            _game.TargetBlock = target;
            _game.StartDigging = DateTime.UtcNow;
            short damage;
            _game.EndDigging = _game.StartDigging.AddMilliseconds(
                BlockProvider.GetHarvestTime(_serviceLocator, block,
                    _game.Client.Hotbar[_game.Client.HotbarSelection].Item.ID, out damage));
            _game.Client.QueuePacket(new PlayerDiggingPacket(
                PlayerDiggingPacket.Action.StartDigging,
                _game.TargetBlock.X, (sbyte)_game.TargetBlock.Y, _game.TargetBlock.Z,
                _game.HighlightedBlockFace));
            _nextAnimation = DateTime.UtcNow.AddSeconds(0.25);
        }

        public override bool MouseButtonUp(GameTime gameTime, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    _digging = false;
                    return true;
            }
            return false;
        }

        private void PlayFootstep()
        {
            GlobalVoxelCoordinates coords = (GlobalVoxelCoordinates)_game.Client.BoundingBox.Min.Floor();
            var target = _game.Client.Dimension.GetBlockID(coords);
            if (target == (byte)BlockIDs.Air)
                target = _game.Client.Dimension.GetBlockID(coords + Vector3i.Down);
            IBlockProvider? provider = _serviceLocator.BlockRepository.GetBlockProvider(target);
            if (provider is null || provider.SoundEffect == SoundEffectClass.None)
                return;
            var effect = string.Format("footstep.{0}", Enum.GetName(typeof(SoundEffectClass), provider.SoundEffect)?.ToLower());
            _game.Audio.PlayPack(effect, 0.5f);
        }

        public override void Update(GameTime gameTime)
        {
            XVector3 delta = _delta;

            var gamePad = GamePad.GetState(PlayerIndex.One); // TODO: Can this stuff be done effectively in the GamePadHandler?
            if (gamePad.IsConnected && gamePad.ThumbSticks.Left.Length() != 0)
                delta = new XVector3(gamePad.ThumbSticks.Left.X, 0, gamePad.ThumbSticks.Left.Y);

            var digging = _digging;

            if (gamePad.IsConnected && gamePad.Triggers.Right > 0.5f)
                digging = true;
            if (gamePad.IsConnected && _game.HighlightedBlock is not null && gamePad.Triggers.Left > 0.5f && _gamePadState.Triggers.Left < 0.5f)
            {
                ItemStack heldItem = _game.Client.Hotbar[_game.Client.HotbarSelection].Item;
                _game.Client.QueuePacket(new PlayerBlockPlacementPacket(
                    _game.HighlightedBlock.X, (sbyte)_game.HighlightedBlock.Y, _game.HighlightedBlock.Z,
                    _game.HighlightedBlockFace, heldItem));
            }
            if (gamePad.IsConnected && gamePad.ThumbSticks.Right.Length() != 0)
            {
                var look = -(gamePad.ThumbSticks.Right * 8) * (float)(gameTime.ElapsedGameTime.TotalSeconds * 30);

                _game.Client.Yaw -= look.X;
                _game.Client.Pitch -= look.Y;
                _game.Client.Yaw %= 360;
                _game.Client.Pitch = XMathHelper.Clamp(_game.Client.Pitch, -89.9f, 89.9f);
            }

            if (digging)
            {
                if (_game.StartDigging == DateTime.MinValue) // Would like to start digging a block
                {
                    var target = _game.HighlightedBlock;
                    if (!object.ReferenceEquals(target, null))
                        BeginDigging(target);
                }
                else // Currently digging a block
                {
                    GlobalVoxelCoordinates? target = _game.HighlightedBlock;
                    if (object.ReferenceEquals(target, null)) // Cancel
                    {
                        _game.StartDigging = DateTime.MinValue;
                        _game.EndDigging = DateTime.MaxValue;
                        _game.TargetBlock = null;
                    }
                    else if (target != _game.TargetBlock) // Change target
                        BeginDigging(target);
                }
            }
            else
            {
                _game.StartDigging = DateTime.MinValue;
                _game.EndDigging = DateTime.MaxValue;
                _game.TargetBlock = null;
            }

            if (delta != XVector3.Zero)
            {
                XVector3 newVelocity = XVector3.Transform(-delta,
                                 Matrix.CreateRotationY(XMathHelper.ToRadians(-(_game.Client.Yaw - 180) + 180)));

                newVelocity.X *= (float)GameConstants.WalkingSpeed;
                newVelocity.Z *= (float)GameConstants.WalkingSpeed;

                double seconds = gameTime.ElapsedGameTime.TotalSeconds;
                double bobbing = _game.Bobbing;
                _game.Bobbing += seconds * Math.Max(Math.Abs(newVelocity.X), Math.Abs(newVelocity.Z));

                _game.Client.Velocity = new TVector3(newVelocity.X, _game.Client.Velocity.Y, newVelocity.Z);

                if ((int)bobbing % 2 == 0 && (int)_game.Bobbing % 2 != 0)
                    PlayFootstep();
            }
            else
                _game.Client.Velocity *= new TVector3(0, 1, 0);
            if (_game.EndDigging != DateTime.MaxValue)
            {
                if (_nextAnimation < DateTime.UtcNow)
                {
                    _nextAnimation = DateTime.UtcNow.AddSeconds(0.25);
                    _game.Client.QueuePacket(new AnimationPacket(_game.Client.EntityID,
                        AnimationPacket.PlayerAnimation.SwingArm));
                }
                if (DateTime.UtcNow > _game.EndDigging && _game.HighlightedBlock == _game.TargetBlock)
                {
                    if (_game.TargetBlock is not null)
                        _game.Client.QueuePacket(new PlayerDiggingPacket(
                            PlayerDiggingPacket.Action.StopDigging,
                            _game.TargetBlock.X, (sbyte)_game.TargetBlock.Y, _game.TargetBlock.Z,
                            _game.HighlightedBlockFace));
                    _game.EndDigging = DateTime.MaxValue;
                }
            }

            _gamePadState = gamePad;
        }
    }
}
