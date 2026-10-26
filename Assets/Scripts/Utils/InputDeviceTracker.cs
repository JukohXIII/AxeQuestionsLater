using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;

public enum ControlFamily { Keyboard, Xbox, PlayStation }

public static class InputDeviceTracker
{
    public static ControlFamily Current { get; private set; } = ControlFamily.Keyboard;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        InputSystem.onEvent -= OnEvent;
        Current = ControlFamily.Keyboard;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        InputSystem.onEvent -= OnEvent;
        InputSystem.onEvent += OnEvent;
    }

    static void OnEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

        // on ignore le bruit : un contrôle doit avoir bougé de façon significative
        foreach (var _ in eventPtr.EnumerateChangedControls(device: device, magnitudeThreshold: 0.3f))
        {
            Set(Classify(device));
            return;
        }
    }

    static ControlFamily Classify(InputDevice device)
    {
        if (device is DualShockGamepad) return ControlFamily.PlayStation;
        if (device is Gamepad) return ControlFamily.Xbox;   // XInput et manettes génériques
        return ControlFamily.Keyboard;                       // clavier / souris
    }

    static void Set(ControlFamily family)
    {
        if (family == Current) return;
        Current = family;
        Changed?.Invoke();
    }
}