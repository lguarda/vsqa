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
                .SetMessageHandler<KeyAction>(OnKeyAction)
                .RegisterMessageType<MouseAction>()
                .SetMessageHandler<MouseAction>(OnMouseAction);

        }
        private void AckIt(IAckable msg) {
            if (msg.requestId != 0) {
                channel.SendPacket(new AckMessage { requestId = msg.requestId });
            }
        }
        private void OnSetLook(SetLookMessage msg)
        {
            var entity = capi.World.Player.Entity;
            // I think this one works it bugged buffore
            // becaise set look after without delay reset this
            // TODO TRY IT OUT OR REMOVE THIS COMMENT
            //capi.Input.MousePitch = msg.pitch;
            capi.Input.MouseYaw = msg.yaw;
            entity.Pos.Pitch = msg.pitch;

            var elapsed = capi.World.ElapsedMilliseconds;
            AckIt(msg);

        }

        private void OnMouseAction(MouseAction msg)
        {
            Logger.clog($"Fake Mouse:{msg.btn} down:{msg.down}", true);
            MouseSimulator.Click(capi, msg.btn, msg.down);
            AckIt(msg);
        }

        private void OnKeyAction(KeyAction msg)
        {
            if (msg.releaseAll) {
                Logger.clog($"Release all key press");
                keySimulator.ReleaseAllKeys();
            }
            else {
                Logger.clog($"Fake key:{msg.code} pressed:{msg.pressed}", true);
                keySimulator.SetFakeKeyState((GlKeys)msg.code, msg.pressed);
            }
            AckIt(msg);
        }
    }
}

