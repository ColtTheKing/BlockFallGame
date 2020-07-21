using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInfo : MonoBehaviour {
    public int numPlayers;
    public List<InputDevice> players;
    public int score;
    public bool gameOver;
    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }
}
