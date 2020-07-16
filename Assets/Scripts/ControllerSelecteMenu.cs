using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControllerSelecteMenu : MonoBehaviour
{
    private List<int> players;

    public GameObject gameInfoPrefab;

    public TextMeshProUGUI player1ControllerName;
    public TextMeshProUGUI player2ControllerName;
    public TextMeshProUGUI player3ControllerName;
    public TextMeshProUGUI player4ControllerName;

    void Start()
    {
        players = new List<int>();
    }

    void Update() {
        if (GetAcceptButtons() && players.Count < 4) {
            if (Input.GetKeyDown(KeyCode.Mouse0) && !players.Contains(0)) {
                players.Add(0);
            }
            if (Input.GetKeyDown(KeyCode.Joystick1Button0) && !players.Contains(1)) {
                players.Add(1);
            }
            if (Input.GetKeyDown(KeyCode.Joystick2Button0) && !players.Contains(2)) {
                players.Add(2);
            }
            if (Input.GetKeyDown(KeyCode.Joystick3Button0) && !players.Contains(3)) {
                players.Add(3);
            }
            if (Input.GetKeyDown(KeyCode.Joystick4Button0) && !players.Contains(4)) {
                players.Add(4);
            }
            if (Input.GetKeyDown(KeyCode.Joystick5Button0) && !players.Contains(5)) {
                players.Add(5);
            }
            if (Input.GetKeyDown(KeyCode.Joystick6Button0) && !players.Contains(6)) {
                players.Add(6);
            }
            if (Input.GetKeyDown(KeyCode.Joystick7Button0) && !players.Contains(7)) {
                players.Add(7);
            }
            if (Input.GetKeyDown(KeyCode.Joystick8Button0) && !players.Contains(8)) {
                players.Add(8);
            }
            UpdatePlayerTexts();
        }

        if (GetContinueButtons() && players.Count > 1) {
            GameObject gameInfoObj = GameObject.Find("GameInfo");
            if (gameInfoObj == null) {
                gameInfoObj = Instantiate(gameInfoPrefab);
                gameInfoObj.GetComponent<GameInfo>().name = "GameInfo";
            }

            GameInfo info = gameInfoObj.GetComponent<GameInfo>();
            info.numPlayers = players.Count;
            info.gameOver = false;

            for (int i = 0; i < info.numPlayers; i++) {
                switch (i) {
                    case 0:
                        info.player1Controller = players[i];
                        if (players[i] == 0)
                            info.keyboard[i] = true;
                        break;
                    case 1:
                        info.player2Controller = players[i];
                        if (players[i] == 0)
                            info.keyboard[i] = true;
                        break;
                    case 2:
                        info.player3Controller = players[i];
                        if (players[i] == 0)
                            info.keyboard[i] = true;
                        break;
                    case 3:
                        info.player4Controller = players[i];
                        if (players[i] == 0)
                            info.keyboard[i] = true;
                        break;
                }
            }
        }
    }

    private void UpdatePlayerTexts() {
        string[] controllerNames = Input.GetJoystickNames();
        for (int i = 0; i < players.Count; i++) {
            switch (i) {
                case 0:
                    if (players[i] == 0) {
                        player1ControllerName.text = "Keyboard";
                    }
                    else {
                        player1ControllerName.text = controllerNames[players[i] - 1];
                    }
                    player1ControllerName.gameObject.SetActive(true);
                    break;
                case 1:
                    if (players[i] == 0) {
                        player2ControllerName.text = "Keyboard";
                    }
                    else {
                        player2ControllerName.text = controllerNames[players[i] - 1];
                    }
                    player2ControllerName.gameObject.SetActive(true);
                    break;
                case 2:
                    if (players[i] == 0) {
                        player3ControllerName.text = "Keyboard";
                    }
                    else {
                        player3ControllerName.text = controllerNames[players[i] - 1];
                    }
                    player3ControllerName.gameObject.SetActive(true);
                    break;
                case 3:
                    if (players[i] == 0) {
                        player4ControllerName.text = "Keyboard";
                    }
                    else {
                        player4ControllerName.text = controllerNames[players[i] - 1];
                    }
                    player4ControllerName.gameObject.SetActive(true);
                    break;
            }
        }
    }

    //Checks the accept buttons on the keyboard and all controllers
    private bool GetAcceptButtons() {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    //Checks the back buttons on the keyboard and all controllers
    private bool GetBackButtons() {
        return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    //Checks the continue buttons on the keyboard and all controllers
    private bool GetContinueButtons() {
        return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton2);
    }
}
