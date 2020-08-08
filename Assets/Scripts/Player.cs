using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
    public float SPEED;
    float h = 1.5f; // box height
    float l = 0.5f; // box length and width
    float r = 0.25f; //radius of the spheres that make up the corners of the player shape

    private bool alive;
    private Tetromino selected_tetromino;
    private int selected_tetromino_id;
    private Material material;

    private InputController controls;
    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();
    public void Awake() {
        controls = new InputController();
    }

    public void Start() {
        alive = true;
        selected_tetromino = null;
        selected_tetromino_id = -1;
        material = GetComponentInChildren<MeshRenderer>().material;
    }

    public void SetInputDevice(InputDevice device) {
        controls.devices = new InputDevice[] { device };
    }

    public bool PlayerCollides(Vector3 player, Vector3 cube, out Vector3 normal, out float intersection_depth) {
        // nearest point in player box to cube origin
        Vector3 p1 = new Vector3(Mathf.Clamp(cube.x, player.x - l * 0.5f, player.x + l * 0.5f),
                                Mathf.Clamp(cube.y, player.y - h * 0.5f, player.y + h * 0.5f),
                                Mathf.Clamp(cube.z, player.z - l * 0.5f, player.z + l * 0.5f));

        // nearest point in cube to p1
        Vector3 p2 = new Vector3(Mathf.Clamp(p1.x, cube.x - 0.5f, cube.x + 0.5f),
                                Mathf.Clamp(p1.y, cube.y - 0.5f, cube.y + 0.5f),
                                Mathf.Clamp(p1.z, cube.z - 0.5f, cube.z + 0.5f));

        // p1 may equal p2
        // in that case normal and intersection distance are incorrect
        normal = p1 - p2;
        intersection_depth = r - normal.magnitude;

        normal.Normalize();
        return intersection_depth > 0f;
    }

    private void HandleCollisions() {
        // Shoves player position down since their y position is in between voxels
        Vector3Int player_corner = Vector3Int.FloorToInt(transform.position - 1.5f * Vector3.up);

        for (int x = 0; x < 2; ++x) {
            for (int y = 0; y < 4; ++y) {
                for (int z = 0; z < 2; ++z) {
                    Vector3Int p = player_corner + new Vector3Int(x, y, z);
                    int tetromino_id = Game.Terrain(p);

                    if (tetromino_id != Game.EMPTY) {
                        float fall_offset = Game.tetrominos[tetromino_id].falling ? Game.fall_offset : 0f;
                        Vector3 block_position = new Vector3(p.x, p.y + fall_offset, p.z);

                        if (PlayerCollides(transform.position, block_position, out Vector3 normal, out float intersection_depth)) {
                            transform.position += normal * intersection_depth;
                            if (normal == Vector3.zero) {
                                // player was improperly seperated and must be being crushed
                                CrushPlayer();
                            }
                        }
                    }
                }
            }
        }
    }

    private void ResurrectPlayer() {
        // GetComponentInChildren<CapsuleCollider>().gameObject.SetActive(true);
        // Move to spawn position
    }

    private void KillPlayer() {
        // Disable the player's physical form
        GetComponentInChildren<CapsuleCollider>().gameObject.SetActive(false);

        alive = false;
        Game.PlayerDied();
    }

    private void CrushPlayer() {
        if (!alive)
            return;

        Debug.Log("Player Crushed");

        KillPlayer();
    }

    private void BurnPlayer() {
        if (!alive)
            return;

        Debug.Log("Player Burned");

        KillPlayer();
    }

    private void AliveUpdate() {
        Vector3 pos = transform.position;

        // Handles horizontal movement
        Vector2 xzmove = controls.Player.Movement.ReadValue<Vector2>();
        float ymove = controls.Player.Jump.ReadValue<float>() * 2 - 1;

        // Normalizes horizontal movement and adds in jumping
        // NOTE: Jumping currently can be negative to make up for out lack of gravity
        Vector3 fullmove = new Vector3(xzmove.x, ymove, xzmove.y);

        Vector3 velocity = fullmove * SPEED;
        pos += velocity * Time.deltaTime;
        pos.y = Mathf.Max(pos.y, r + h * 0.5f - 0.5f);
        transform.position = pos;

        HandleCollisions();

        // If the lava reaches the player, burn them to death
        if (Game.lava.transform.position.y > transform.position.y - (0.5 * h + r)) {
            BurnPlayer();
        }
    }

    private void DeadUpdate() {
        // If the block is on the ground, give up control of that block
        if (selected_tetromino != null && !selected_tetromino.falling) {
            // WILL NEED TO TRACK THESE BLOCKS LATER FOR SCORING
            selected_tetromino = null;
            selected_tetromino_id = -1;
        }

        // If the player doesn't have a block, try to find them one to control
        if (selected_tetromino == null) {
            selected_tetromino_id = Game.GrabTetromino(material, out selected_tetromino);
        }

        // If the player has a block, control it based player input
        if (selected_tetromino != null) {
            // Transform the block positions based on the input
            Vector3Int[] new_positions = new Vector3Int[selected_tetromino.blocks.Length];
            Vector3 new_pivot = selected_tetromino.rotation_point;
            for (int i = 0; i < new_positions.Length; i++)
                new_positions[i] = selected_tetromino.positions[i];

            // Move the block along the x and z axes
            if (controls.Player.BlockMove.triggered)
                selected_tetromino.XZMove(controls.Player.BlockMove.ReadValue<Vector2>());

            // Rotate the block around the x axis
            if (controls.Player.XRotate.triggered)
                selected_tetromino.Rotate(0, controls.Player.XRotate.ReadValue<float>());

            // Rotate the block around the y axis
            if (controls.Player.YRotate.triggered)
                selected_tetromino.Rotate(1, controls.Player.XRotate.ReadValue<float>());

            // Rotate the block around the z axis
            if (controls.Player.ZRotate.triggered)
                selected_tetromino.Rotate(2, controls.Player.XRotate.ReadValue<float>());
        }
    }

    void Update() {
        if (alive)
            AliveUpdate();
        else
            DeadUpdate();
    }
}
