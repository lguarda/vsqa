using Vintagestory.API.Client;
using Vintagestory.API.Common;
using TestHarnessMod.Core;
using System;
using System.Reflection;

namespace TestHarnessMod
{
    public class TestHarnessClientModSystem : ModSystem
    {

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        private ICoreClientAPI capi;
        private KeyStateSimulator keySimulator;
        private IClientNetworkChannel channel;

        public override void StartClientSide(ICoreClientAPI capi)
        {
            this.capi = capi;
            Logger.capi = capi;
            keySimulator = new KeyStateSimulator(capi);
            channel = capi.Network.RegisterChannel("testharness")
                .RegisterMessageType<AckMessage>()
                .RegisterMessageType<SetLookMessage>()
                .SetMessageHandler<SetLookMessage>(OnSetLook)
                .RegisterMessageType<KeyAction>()
                .SetMessageHandler<KeyAction>(OnKeyAction);

        }
        private void AckIt(IAckable msg) {
            if (msg.RequestId != 0) {
                channel.SendPacket(new AckMessage { RequestId = msg.RequestId });
            }
        }
        private void OnSetLook(SetLookMessage msg)
        {
            var entity = capi.World.Player.Entity;
            // I think this one works it bugged buffore
            // becaise set look after without delay reset this
            // TODO TRY IT OUT OR REMOVE THIS COMMENT
            //capi.Input.MousePitch = msg.Pitch;
            capi.Input.MouseYaw = msg.Yaw;
            entity.Pos.Pitch = msg.Pitch;

            var elapsed = capi.World.ElapsedMilliseconds;
            AckIt(msg);

        }

        private void OnKeyAction(KeyAction msg)
        {
            if (msg.ReleaseAll) {
                Logger.clog($"Release all key press");
                keySimulator.ReleaseAllKeys();
            }
            else {
                Logger.clog($"Fake key:{msg.KeyCode} up:{msg.KeyUp}", true);
                keySimulator.SetFakeKeyState((GlKeys)msg.KeyCode, msg.KeyUp);
            }
        }

        // Don't work at all
        private void MouseButton(
            int x,
            int y,
            EnumMouseButton button,
            bool up)
        {
            // Left = 0,
            // Middle = 1,
            // Right = 2,
            // Button4 = 3,
            // Button5 = 4,
            // Button6 = 5,
            // Button7 = 6,
            // Button8 = 7,
            // /// <summary>
            // /// Used to signal to event handlers, but not actually a button: activated when the wheel is scrolled.
            // /// </summary>
            // Wheel = 13,
            // None = 255

            var mouseEvent = new MouseEvent(x, y, button, 0);
            string fn = up ? "TriggerMouseDown" : "TriggerMouseUp";
            var method = capi.Event.GetType().GetMethod(
                fn,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (method == null)
                throw new Exception("TriggerMouseDown not found");

            method.Invoke(
                capi.Event,
                new object[] { mouseEvent }
            );
        }

    }
}

