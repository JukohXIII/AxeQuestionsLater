using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input/Control Glyphs")]
public class ControlGlyphs : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Ex: f, buttonSouth, buttonWest, leftShoulder, dpad/up")]
        public string control;
        public Sprite sprite;
    }

    public Entry[] keyboard;
    public Entry[] xbox;
    public Entry[] playstation;

    public Sprite Get(ControlFamily family, string control)
    {
        Entry[] list = family switch
        {
            ControlFamily.Xbox => xbox,
            ControlFamily.PlayStation => playstation,
            _ => keyboard,
        };
        if (list == null || string.IsNullOrEmpty(control)) return null;

        foreach (var e in list)
            if (string.Equals(e.control, control, StringComparison.OrdinalIgnoreCase))
                return e.sprite;
        return null;
    }
}
    public static class BindingGlyph
    {
        public static bool TryGet(InputAction action, ControlFamily family, out string control, out string display)
        {
            control = null;
            display = null;
            bool wantPad = family != ControlFamily.Keyboard;

            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite) continue;

                string path = b.effectivePath;
                if (string.IsNullOrEmpty(path)) continue;

                string layout = InputControlPath.TryGetDeviceLayout(path);
                if (string.IsNullOrEmpty(layout)) continue;

                bool isPad = InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad");
                bool isKeyboardMouse = layout == "Keyboard" || layout == "Mouse";
                if (wantPad ? !isPad : !isKeyboardMouse) continue;

                // "<Keyboard>/f" -> "f" ; "<Gamepad>/buttonSouth" -> "buttonSouth" ; "<Keyboard>/#(f)" -> "f"
                string name = path.Substring(path.IndexOf('/') + 1);
                if (name.StartsWith("#(") && name.EndsWith(")"))
                    name = name.Substring(2, name.Length - 3);

                control = name;
                display = InputControlPath.ToHumanReadableString(
                    path, InputControlPath.HumanReadableStringOptions.OmitDevice
                );
                return true;
            }
            return false;
        }
    }
