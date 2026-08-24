using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

// This is 99% AI generated, it work
// but i need to check if everything here is really mandatory

public class KeyStateSimulator
{
    private readonly ICoreClientAPI capi;
    private readonly object hotkeyManager;
    private readonly MethodInfo triggerHotKeyMethod;

    // Track all actively simulated pressed keys for quick teardown
    private readonly HashSet<GlKeys> currentlyPressedKeys = new HashSet<GlKeys>();

    public KeyStateSimulator(ICoreClientAPI capi)
    {
        this.capi = capi;

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

    // Fakes a physical key press (isKeyUp = false) or release (isKeyUp = true).
    public void SetFakeKeyState(GlKeys key, bool isKeyUp)
    {
        int keyCode = (int)key;

        // Track internal state for ReleaseAllKeys
        if (!isKeyUp)
        {
            currentlyPressedKeys.Add(key);
        }
        else
        {
            currentlyPressedKeys.Remove(key);
        }

        // Maintain low-level raw key state arrays
        if (capi.Input.KeyboardKeyState != null)
            capi.Input.KeyboardKeyState[keyCode] = !isKeyUp;

        if (capi.Input.KeyboardKeyStateRaw != null)
            capi.Input.KeyboardKeyStateRaw[keyCode] = !isKeyUp;

        UpdateInputInSlot(keyCode, !isKeyUp);

        // Build KeyEvent
        var keyEvent = new KeyEvent
        {
            KeyCode = keyCode,
            KeyChar = (char)keyCode,
            CtrlPressed = false,
            ShiftPressed = false,
            AltPressed = false,
            CommandPressed = false,
            Handled = false
        };

        // Find target hotkeys inside capi.Input.HotKeys
        var targetHotKeys = FindHotKeysFromInputApi(key, keyCode);

        // Force state on individual HotKey objects before invocation
        foreach (var hk in targetHotKeys)
        {
            SetHotKeyIsDown(hk, !isKeyUp);
        }

        // Invoke TriggerHotKey
        try
        {
            triggerHotKeyMethod.Invoke(hotkeyManager, new object[]
            {
                keyEvent,
                capi.World,
                capi.World.Player,
                true,
                isKeyUp
            });
        }
        catch (Exception ex)
        {
            capi.Logger.Error($"[KeySim] Error during TriggerHotKey: {ex}");
        }

        // Clean up PressedHotkeys list
        foreach (var hk in targetHotKeys)
        {
            SyncPressedHotkeysList(hk, isKeyUp);
        }
    }

    // Releases all keys currently held down by the simulator, plus resets raw array states.
    // Call this in your NUnit [TearDown] or test cleanup.
    public void ReleaseAllKeys()
    {
        // Release all tracked simulated keys through normal key-up flow
        var activeKeys = new List<GlKeys>(currentlyPressedKeys);
        foreach (var key in activeKeys)
        {
            SetFakeKeyState(key, isKeyUp: true);
        }
        currentlyPressedKeys.Clear();

        // Hard-reset raw key state arrays across all slots as a extra fallback
        if (capi.Input.KeyboardKeyState != null)
        {
            Array.Clear(capi.Input.KeyboardKeyState, 0, capi.Input.KeyboardKeyState.Length);
        }

        if (capi.Input.KeyboardKeyStateRaw != null)
        {
            Array.Clear(capi.Input.KeyboardKeyStateRaw, 0, capi.Input.KeyboardKeyStateRaw.Length);
        }

        // Clear InSlot arrays
        try
        {
            PropertyInfo inSlotProp = capi.Input.GetType().GetProperty("InSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (inSlotProp?.GetValue(capi.Input) is bool[] inSlotArray)
            {
                Array.Clear(inSlotArray, 0, inSlotArray.Length);
            }
        }
        catch { /* ignore */ }

        // Clear any lingering PressedHotkeys in HotkeyManager & InputManager
        ClearPressedHotkeysList(hotkeyManager);
        ClearPressedHotkeysList(capi.Input);
    }

    private List<object> FindHotKeysFromInputApi(GlKeys key, int keyCode)
    {
        var list = new List<object>();
        var hotkeys = capi.Input.HotKeys;
        if (hotkeys == null) return list;

        string keyEnumStr = key.ToString();

        foreach (KeyValuePair<string, HotKey> entry in hotkeys)
        {
            if (entry.Value == null) continue;

            string dictKey = entry.Key ?? "";
            HotKey hkObj = entry.Value;

            string codeVal = hkObj.Code ?? "";
            KeyCombination combo = hkObj.CurrentMapping;

            bool isMatch = false;

            if (dictKey.Equals(keyEnumStr, StringComparison.OrdinalIgnoreCase)) isMatch = true;
            if (codeVal.Equals(keyEnumStr, StringComparison.OrdinalIgnoreCase)) isMatch = true;
            if (combo != null && (combo.KeyCode == keyCode || combo.SecondKeyCode == keyCode)) isMatch = true;

            if (isMatch)
            {
                list.Add(hkObj);
            }
        }

        return list;
    }

    private void UpdateInputInSlot(int keyCode, bool isPressed)
    {
        try
        {
            PropertyInfo inSlotProp = capi.Input.GetType().GetProperty("InSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (inSlotProp?.GetValue(capi.Input) is bool[] inSlotArray && keyCode < inSlotArray.Length)
            {
                inSlotArray[keyCode] = isPressed;
            }
        }
        catch { /* ignore ?? */ }
    }

    private void SetHotKeyIsDown(object hotKeyObj, bool isDown)
    {
        Type t = hotKeyObj.GetType();
        PropertyInfo prop = t.GetProperty("IsDown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(hotKeyObj, isDown);
        }

        FieldInfo field = t.GetField("IsDown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(hotKeyObj, isDown);
        }
    }

    private void SyncPressedHotkeysList(object targetHotKey, bool isKeyUp)
    {
        FieldInfo pressedField = hotkeyManager.GetType().GetField("PressedHotkeys", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (pressedField?.GetValue(hotkeyManager) is IList pressedList)
        {
            if (!isKeyUp)
            {
                if (!pressedList.Contains(targetHotKey)) pressedList.Add(targetHotKey);
            }
            else
            {
                while (pressedList.Contains(targetHotKey))
                {
                    pressedList.Remove(targetHotKey);
                }
            }
        }

        FieldInfo inputPressedField = capi.Input.GetType().GetField("PressedHotkeys", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (inputPressedField?.GetValue(capi.Input) is IList inputPressedList)
        {
            if (!isKeyUp)
            {
                if (!inputPressedList.Contains(targetHotKey)) inputPressedList.Add(targetHotKey);
            }
            else
            {
                while (inputPressedList.Contains(targetHotKey))
                {
                    inputPressedList.Remove(targetHotKey);
                }
            }
        }
    }

    private void ClearPressedHotkeysList(object targetContainer)
    {
        if (targetContainer == null) return;
        FieldInfo pressedField = targetContainer.GetType().GetField("PressedHotkeys", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (pressedField?.GetValue(targetContainer) is IList pressedList)
        {
            pressedList.Clear();
        }
    }
}
