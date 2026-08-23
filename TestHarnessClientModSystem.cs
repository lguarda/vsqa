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

        public override void StartClientSide(ICoreClientAPI capi)
        {
            this.capi = capi;
            keySimulator = new KeyStateSimulator(capi);
            capi.Network.RegisterChannel("testharness")
                .RegisterMessageType<SetLookMessage>()
                .SetMessageHandler<SetLookMessage>(OnSetLook)
                .RegisterMessageType<KeyAction>()
                .SetMessageHandler<KeyAction>(OnKeyAction);

        }

        private void OnSetLook(SetLookMessage msg)
        {
            var entity = capi.World.Player.Entity;
            capi.ShowChatMessage($"OMG before Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");
            capi.Logger.Notification($"OMG before Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");
            //capi.Input.MousePitch = msg.Pitch;
            capi.Input.MouseYaw = msg.Yaw;
            entity.Pos.Pitch = msg.Pitch;

            capi.ShowChatMessage($"OMG before Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");
            capi.Logger.Notification($"OMG before Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");


            //entity.Pos.Yaw = msg.Yaw;
            //capi.ShowChatMessage($"OMG after Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");
            //capi.Logger.Notification($"OMG after Yaw:{entity.Pos.Yaw}|{entity.WalkYaw} Pitch:{entity.Pos.Pitch}");
            //entity.ServerPos.Yaw = msg.Yaw;
        }

        private void OnKeyAction(KeyAction msg)
        {
            //capi.ShowChatMessage("OMG let's go");
            keySimulator.SetFakeKeyState((GlKeys)msg.KeyCode, msg.KeyUp);
            //capi.ShowChatMessage($"client button:{EnumMouseButton.Button5}, state: {msg.KeyUp}");
            //capi.Logger.Notification($"client button:{EnumMouseButton.Button5}, state: {msg.KeyUp}");
            //MouseButton(500, 300, EnumMouseButton.Button5, msg.KeyUp);
            //MouseButton(500, 300, EnumMouseButton.Left, msg.KeyUp);
        }

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

