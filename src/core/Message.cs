using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

// WIP i should probably have a message sub-directory
// so i can put all new message in separate files
// this is just a temporary "It work state"
namespace TestHarnessMod.Core
{
    public interface IAckable
    {
        int requestId { get; set; }
    }

    [ProtoContract]
    public class AckMessage
    {
        [ProtoMember(1)]
        public int requestId;

        public AckMessage() { }
        public AckMessage(int rid) { requestId = rid; }
    }

    [ProtoContract]
    public class SetLookMessage : IAckable
    {
        [ProtoMember(1)]
        public int requestId { get; set; }
        [ProtoMember(2)]
        public float yaw;
        [ProtoMember(3)]
        public float pitch;

        public SetLookMessage() { }
        public SetLookMessage(float y, float p)
        {
            yaw = y;
            pitch = p;
        }
    }

    [ProtoContract]
    public class KeyAction : IAckable
    {
        [ProtoMember(1)]
        public int requestId { get; set; }
        [ProtoMember(2)]
        public bool ctlr;
        [ProtoMember(3)]
        public bool alt;
        [ProtoMember(4)]
        public bool shift;
        [ProtoMember(5)]
        public bool pressed;
        [ProtoMember(6)]
        public int code;
        [ProtoMember(7)]
        public bool releaseAll;

        public KeyAction() { }
        public KeyAction(bool release)
        {
            code = 0;
            pressed = false;
            ctlr = false;
            alt = false;
            shift = false;
            releaseAll = true;
        }
        public KeyAction(GlKeys k, bool p)
        {
            code = (int)k;
            pressed = p;
            ctlr = false;
            alt = false;
            shift = false;
            releaseAll = false;
        }
    }

    [ProtoContract]
    public class MouseAction : IAckable
    {
        [ProtoMember(1)]
        public int requestId { get; set; }
        [ProtoMember(2)]
        public EnumMouseButton btn;
        [ProtoMember(3)]
        public bool down;
    }
}
