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

    public void SetFakeKeyState(GlKeys key, bool isPressed, bool ctrl = false, bool shift = false, bool alt = false)
    {
        int keyCode = (int)key;

        // Sync state tracking
        if (isPressed)
            currentlyPressedKeys.Add(key);
        else
            currentlyPressedKeys.Remove(key);


        SetLowLevelKeyState(keyCode, isPressed);

        // Maintain modifier key state arrays so api queries like capi.Input.KeyboardKeyState[ControlLeft] pass
        if (ctrl) SetLowLevelKeyState((int)GlKeys.ControlLeft, isPressed);
        if (shift) SetLowLevelKeyState((int)GlKeys.ShiftLeft, isPressed);
        if (alt) SetLowLevelKeyState((int)GlKeys.AltLeft, isPressed);

        // Build KeyEvent with modifier flags populated
        var keyEvent = new KeyEvent
        {
            KeyCode = keyCode,
            KeyChar = (char)keyCode,
            CtrlPressed = ctrl,
            ShiftPressed = shift,
            AltPressed = alt,
            CommandPressed = false,
            Handled = false
        };

        // Find target hotkeys inside capi.Input.HotKeys matching combination rules
        var targetHotKeys = FindHotKeysFromInputApi(key, keyCode, ctrl, shift, alt);

        // Force state on individual HotKey objects before invocation
        foreach (var hk in targetHotKeys)
        {
            SetHotKeyIsDown(hk, isPressed);
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
                !isPressed
            });
        }
        catch (Exception ex)
        {
            capi.Logger.Error($"[KeySim] Error during TriggerHotKey: {ex}");
        }

        // Clean up PressedHotkeys list
        foreach (var hk in targetHotKeys)
        {
            SyncPressedHotkeysList(hk, isPressed);
        }
    }

    private void SetLowLevelKeyState(int keyCode, bool isPressed)
    {
        if (capi.Input.KeyboardKeyState != null && keyCode < capi.Input.KeyboardKeyState.Length)
            capi.Input.KeyboardKeyState[keyCode] = isPressed;

        if (capi.Input.KeyboardKeyStateRaw != null && keyCode < capi.Input.KeyboardKeyStateRaw.Length)
            capi.Input.KeyboardKeyStateRaw[keyCode] = isPressed;

        UpdateInputInSlot(keyCode, isPressed);
    }

    public void ReleaseAllKeys()
    {
        var activeKeys = new List<GlKeys>(currentlyPressedKeys);
        foreach (var key in activeKeys)
        {
            SetFakeKeyState(key, isPressed: false);
        }
        currentlyPressedKeys.Clear();

        if (capi.Input.KeyboardKeyState != null)
            Array.Clear(capi.Input.KeyboardKeyState, 0, capi.Input.KeyboardKeyState.Length);

        if (capi.Input.KeyboardKeyStateRaw != null)
            Array.Clear(capi.Input.KeyboardKeyStateRaw, 0, capi.Input.KeyboardKeyStateRaw.Length);

        try
        {
            PropertyInfo inSlotProp = capi.Input.GetType().GetProperty("InSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (inSlotProp?.GetValue(capi.Input) is bool[] inSlotArray)
            {
                Array.Clear(inSlotArray, 0, inSlotArray.Length);
            }
        }
        catch { /* ignore? log ?*/ }

        ClearPressedHotkeysList(hotkeyManager);
        ClearPressedHotkeysList(capi.Input);
    }

    private List<object> FindHotKeysFromInputApi(GlKeys key, int keyCode, bool ctrl, bool shift, bool alt)
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

            if (combo != null)
            {
                bool keyMatches = (combo.KeyCode == keyCode || combo.SecondKeyCode == keyCode);

                // Match modifiers if specified in key combination mapping
                bool modifierMatches = (combo.Ctrl == ctrl || !ctrl) &&
                                      (combo.Shift == shift || !shift) &&
                                      (combo.Alt == alt || !alt);

                if (keyMatches && modifierMatches) isMatch = true;
            }

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
            prop.SetValue(hotKeyObj, isDown);

        FieldInfo field = t.GetField("IsDown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(hotKeyObj, isDown);
    }

    private void SyncPressedHotkeysList(object targetHotKey, bool isPressed)
    {
        SyncList(hotkeyManager, targetHotKey, isPressed);
        SyncList(capi.Input, targetHotKey, isPressed);
    }

    private void SyncList(object container, object targetHotKey, bool isPressed)
    {
        if (container == null) return;
        FieldInfo pressedField = container.GetType().GetField("PressedHotkeys", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (pressedField?.GetValue(container) is IList pressedList)
        {
            if (isPressed)
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
