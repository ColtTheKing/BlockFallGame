using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInfo : MonoBehaviour {
    public List<InputDevice> players;
    public List<int> scores;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }
}
