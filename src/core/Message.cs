using ProtoBuf;
using Vintagestory.API.Client;

namespace TestHarnessMod.Core
{
    public interface IAckable
    {
        int RequestId { get; set; }
    }

    [ProtoContract]
    public class AckMessage
    {
        [ProtoMember(1)]
        public int RequestId;

        public AckMessage() { }
        public AckMessage(int requestId) { RequestId = requestId; }
    }

    [ProtoContract]
    public class SetLookMessage : IAckable
    {
        [ProtoMember(1)]
        public int RequestId { get; set; }
        [ProtoMember(2)]
        public float Yaw;
        [ProtoMember(3)]
        public float Pitch;

        public SetLookMessage() { }
        public SetLookMessage(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    [ProtoContract]
    public class KeyAction : IAckable
    {
        [ProtoMember(1)]
        public int RequestId { get; set; }
        [ProtoMember(2)]
        public bool Ctrl;
        [ProtoMember(3)]
        public bool Alt;
        [ProtoMember(4)]
        public bool Shift;
        [ProtoMember(5)]
        public bool KeyUp;
        [ProtoMember(6)]
        public int KeyCode;
        [ProtoMember(7)]
        public bool ReleaseAll;

        public KeyAction() { }
        public KeyAction(bool release)
        {
            KeyCode = 0;
            KeyUp = false;
            Ctrl = false;
            Alt = false;
            Shift = false;
            ReleaseAll = true;
        }
        public KeyAction(GlKeys code, bool up)
        {
            KeyCode = (int)code;
            KeyUp = up;
            Ctrl = false;
            Alt = false;
            Shift = false;
            ReleaseAll = false;
        }
    }
}
