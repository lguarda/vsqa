using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

public static class MouseSimulator
{
    private static MethodInfo updateMouseBtn;

    private static MethodInfo GetMethod(object clientMain)
    {
        return updateMouseBtn ??= clientMain.GetType().GetMethod(
            "UpdateMouseButtonState",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(EnumMouseButton), typeof(bool) },
            null
        );
    }

    // VintagestoryAPI.dll/Vintagestory.API.Common/EnumMouseButton.cs
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

    public static void ClickDown(ICoreClientAPI capi, EnumMouseButton button)
    {
        // capi.World underlying object IS ClientMain
        // look at Vintagestory.Client.NoObf
        object clientMain = capi.World;
        var mi = GetMethod(clientMain);
        mi.Invoke(clientMain, new object[] { button, true });
    }

    public static void ClickUp(ICoreClientAPI capi, EnumMouseButton button)
    {
        object clientMain = capi.World;
        var mi = GetMethod(clientMain);
        mi.Invoke(clientMain, new object[] { button, false });
    }
    public static void Click(ICoreClientAPI capi, EnumMouseButton button, bool down) {
        if (down) {
            ClickDown(capi, button);
        } else {
            ClickUp(capi, button);
        }
    }
}
