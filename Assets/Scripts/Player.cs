using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    public float SPEED;
    float h = 1.5f; // box height
    float l = 0.5f; // box length and width
    float r = 0.25f; //radius of the spheres that make up the corners of the player shape

    private bool alive;
    private Tetromino selected_block;
    public static Lava lava;
    private Material material;

    public void Start()
    {
        alive = true;
        selected_block = null;
        material = GetComponentInChildren<MeshRenderer>().material;
    }

    public bool Collides(Vector3 player, Vector3 cube, out Vector3 normal, out float intersection_depth) {
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

                        if (Collides(transform.position, block_position, out Vector3 normal, out float intersection_depth)) {
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

    private void KillPlayer() {
        // Destroy the player's physical form
        Destroy(GetComponentInChildren<CapsuleCollider>().gameObject);

        // give them the newest tetromino to control and give it the player's colour
        selected_block = Game.GrabTetromino(material);

        alive = false;
        Game.PlayerDied();
    }

    private void CrushPlayer() {
        Debug.Log("Player Crushed");

        KillPlayer();
    }

    private void BurnPlayer() {
        Debug.Log("Player Burned");

        KillPlayer();
    }

    private void AliveUpdate() {
        Vector3 pos = transform.position;

        // Handles horizontal movement
        Vector3 xzmove = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        // Normalizes horizontal movement and adds in jumping
        // NOTE: Jumping currently can be negative to make up for out lack of gravity
        Vector3 fullmove = xzmove.normalized + new Vector3(0, Input.GetAxisRaw("Jump") * 2f - 1f, 0);

        Vector3 velocity = fullmove * SPEED;
        pos += velocity * Time.deltaTime;
        pos.y = Mathf.Max(pos.y, r + h * 0.5f - 0.5f);
        transform.position = pos;

        HandleCollisions();

        // If the lava reaches the player, burn them to death
        if (lava.transform.position.y > transform.position.y - 1)
        {
            BurnPlayer();
        }
    }

    private void DeadUpdate() {
        Vector3 xzmove = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        float xrot = Input.GetAxisRaw("XRotation");
        float yrot = Input.GetAxisRaw("YRotation");
        float zrot = Input.GetAxisRaw("ZRotation");

        // If the block is on the ground, give them a new one to control
        if(!selected_block.falling) {
            // MAY NEED TO TRACK THESE BLOCKS LATER FOR SCORING
            selected_block = Game.GrabTetromino(material);
        }
    }

    void Update() {
        if (alive)
            AliveUpdate();
        else
            DeadUpdate();
    }
}
