using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class Debounce : InputProcessor<float> {
#if UNITY_EDITOR
    static Debounce() {
        Initialize();
    }
#endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize() {
        InputSystem.RegisterProcessor<Debounce>();
    }

    [Tooltip("Minimum value before the control is allowed to trigger.")]
    public float trigger = 0.6f;
    [Tooltip("Maximum value before the control resets and is able to trigger again.")]
    public float release = 0.4f;

    private bool triggered = false;
    private int previousFrame = 0;
    private float previousReturn = 0.0f;

    public override float Process(float value, InputControl control) {
        // return cached result if already called this frame
        if (previousFrame == Time.frameCount) {
            return previousReturn;
        }

        previousFrame = Time.frameCount;

        if (value < release) {
            triggered = false;
        }

        if (!triggered && value > trigger) {
            triggered = true;
            previousReturn = 1.0f;
        } else {
            previousReturn = 0.0f;
        }

        return previousReturn;
    }
}