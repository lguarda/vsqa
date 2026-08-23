using ProtoBuf;
using Vintagestory.API.Client;

namespace TestHarnessMod.Core
{
    [ProtoContract]
    public class SetLookMessage
    {
        [ProtoMember(1)]
        public float Yaw;

        [ProtoMember(2)]
        public float Pitch;

        // Protobuf requires a parameterless constructor
        public SetLookMessage() { }

        public SetLookMessage(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    [ProtoContract]
    public class KeyAction
    {
        [ProtoMember(1)]
        public bool Ctrl;

        [ProtoMember(2)]
        public bool Alt;

        [ProtoMember(3)]
        public bool Shift;

        [ProtoMember(4)]
        public bool KeyUp;

        [ProtoMember(5)]
        public int KeyCode;

        // Protobuf requires a parameterless constructor
        public KeyAction() { }
        public KeyAction(GlKeys code, bool up)
        {
            KeyCode = (int)code;
            KeyUp = up;
            Ctrl = false;
            Alt = false;
            Shift = false;
        }
    }
}
