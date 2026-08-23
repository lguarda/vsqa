using System;
using System.Collections;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

public class KeyStateSimulator
{
    private readonly ICoreClientAPI capi;
    private readonly object hotkeyManager;
    private readonly MethodInfo triggerHotKeyMethod;

    public KeyStateSimulator(ICoreClientAPI capi)
    {
        this.capi = capi;

        // Fetch ScreenManager.hotkeyManager
        Type screenManagerType = Type.GetType("Vintagestory.Client.ScreenManager, VintagestoryLib")
            ?? throw new Exception("ScreenManager not found");

        FieldInfo hotkeyField = screenManagerType.GetField("hotkeyManager", BindingFlags.Static | BindingFlags.Public)
            ?? throw new Exception("hotkeyManager field not found");

        hotkeyManager = hotkeyField.GetValue(null)
            ?? throw new Exception("HotkeyManager instance is null");

        triggerHotKeyMethod = hotkeyManager.GetType().GetMethod(
            "TriggerHotKey",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(KeyEvent), typeof(IWorldAccessor), typeof(IPlayer), typeof(bool), typeof(bool) },
            modifiers: null
        ) ?? throw new Exception("TriggerHotKey signature mismatch");
    }

    /// <summary>
    /// Fakes a physical key press or release and holds state across frames.
    /// </summary>
    public void SetFakeKeyState(GlKeys key, bool isKeyUp)
    {
        int keyCode = (int)key;

        // 1. Maintain low-level raw key state (prevents game loop reset)
        if (capi.Input.KeyboardKeyState != null)
        {
            capi.Input.KeyboardKeyState[keyCode] = !isKeyUp;
        }

        // 2. Build KeyEvent
        var keyEvent = new KeyEvent
        {
            KeyCode = keyCode,
            KeyChar = '\0',
            CtrlPressed = false,
            ShiftPressed = false,
            AltPressed = false,
            CommandPressed = false,
            Handled = false
        };

        // 3. Keep internal HotkeyManager.PressedHotkeys in sync
        SyncPressedHotkeysList(keyCode, isKeyUp);

        // 4. Trigger initial down/up invocation
        triggerHotKeyMethod.Invoke(hotkeyManager, new object[]
        {
            keyEvent,
            capi.World,
            capi.World.Player,
            true,     // allowCharacterControls
            isKeyUp   // keyUp
        });
    }

    private void SyncPressedHotkeysList(int keyCode, bool isKeyUp)
    {
        // Access HotkeyManager.HotKeys (Dictionary<string, HotKey>)
        PropertyInfo hotkeysProp = hotkeyManager.GetType().GetProperty("HotKeys", BindingFlags.Instance | BindingFlags.Public);
        var hotkeys = hotkeysProp?.GetValue(hotkeyManager) as IDictionary;
        if (hotkeys == null) return;

        // Find the HotKey associated with this GlKey
        object targetHotKey = null;
        foreach (DictionaryEntry entry in hotkeys)
        {
            PropertyInfo mappingProp = entry.Value.GetType().GetProperty("CurrentMapping", BindingFlags.Instance | BindingFlags.Public);
            if (mappingProp?.GetValue(entry.Value) is KeyCombination combo && combo.KeyCode == keyCode)
            {
                targetHotKey = entry.Value;
                break;
            }
        }

        if (targetHotKey == null) return;

        // Mutate PressedHotkeys list so VS considers the hotkey pressed during tick updates
        FieldInfo pressedField = hotkeyManager.GetType().GetField("PressedHotkeys", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (pressedField?.GetValue(hotkeyManager) is IList pressedList)
        {
            if (!isKeyUp)
            {
                if (!pressedList.Contains(targetHotKey)) pressedList.Add(targetHotKey);
            }
            else
            {
                pressedList.Remove(targetHotKey);
            }
        }
    }
}
