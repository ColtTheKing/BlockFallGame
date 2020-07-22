using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ControllerSelectMenu : MonoBehaviour {
    private InputController controls;
    private List<InputDevice> players;

    public TextMeshProUGUI[] controllerNames;
    public GameObject gameInfoPrefab;

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Awake() {
        controls = new InputController();
        controls.Menu.Confirm.performed += context => AddPlayer(context.control.device);
        controls.Menu.Cancel.performed += context => RemovePlayer(context.control.device);
        controls.Menu.Start.performed += context => StartGame();

        players = new List<InputDevice>();
    }

    void AddPlayer(InputDevice device) {
        if (players.Count < 4 && !players.Contains(device)) {
            players.Add(device);
            UpdatePlayerTexts();
        }
    }

    void RemovePlayer(InputDevice device) {
        if (players.Contains(device)) {
            players.Remove(device);
            UpdatePlayerTexts();
        }
    }

    void StartGame() {
        if (players.Count > 1) {
            GameObject gameInfoObj = GameObject.Find("GameInfo");
            if (gameInfoObj == null) {
                gameInfoObj = Instantiate(gameInfoPrefab);
                gameInfoObj.GetComponent<GameInfo>().name = "GameInfo";
            }

            GameInfo info = gameInfoObj.GetComponent<GameInfo>();
            info.players = players;

            SceneManager.LoadScene("Game");
        }
    }
    
    private void UpdatePlayerTexts() {
        for (int i = 0; i < controllerNames.Length; i++) {
            if (i < players.Count) {
                controllerNames[i].text = players[i].displayName;
                controllerNames[i].gameObject.SetActive(true);
            } else {
                controllerNames[i].gameObject.SetActive(false);
            }
        }
    }
}