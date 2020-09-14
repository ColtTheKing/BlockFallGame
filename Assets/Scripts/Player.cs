using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
    public float SPEED;
    float JUMP_SPEED = 5.0f;
    float AIR_ACCELERATION = 15.0f;
    float MU = 0.1f;
    float MASS = 25.0f;

    float h = 1.5f; // box height
    float l = 0.5f; // box length and width
    float r = 0.25f; //radius of the spheres that make up the corners of the player shape

    private bool alive;
    int lastGrounded;
    Vector3 velocity;
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
        lastGrounded = int.MinValue;
        velocity = Vector3.zero;
        selected_tetromino = null;
        selected_tetromino_id = -1;
        material = GetComponentInChildren<MeshRenderer>().material;
    }

    public void SetInputDevice(InputDevice device) {
        controls.devices = new InputDevice[] { device };
    }

    public bool BlockCollides(Vector3 cube, out Vector3 normal, out float intersection_depth) {
        Vector3 player = transform.position;

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
        return intersection_depth >= 0f;
    }

    public bool PlayerCollides(Vector3 other, out Vector3 normal, out float intersection_depth) {
        Vector3 player = transform.position;

        // nearest point in player box to other origin
        Vector3 p1 = new Vector3(Mathf.Clamp(other.x, player.x - l * 0.5f, player.x + l * 0.5f),
                                Mathf.Clamp(other.y, player.y - h * 0.5f, player.y + h * 0.5f),
                                Mathf.Clamp(other.z, player.z - l * 0.5f, player.z + l * 0.5f));

        // nearest point in other box to p1
        Vector3 p2 = new Vector3(Mathf.Clamp(p1.x, other.x - l * 0.5f, other.x + l * 0.5f),
                        Mathf.Clamp(p1.y, other.y - h * 0.5f, other.y + h * 0.5f),
                        Mathf.Clamp(p1.z, other.z - l * 0.5f, other.z + l * 0.5f));

        // p1 may equal p2
        // in that case normal and intersection distance are incorrect
        normal = p1 - p2;
        intersection_depth = 2.0f * r - normal.magnitude;

        normal.Normalize();
        return intersection_depth >= 0f;
    }

    // Determines if a cell within the world terrain is at least partially occupied by this player
    // MAY NEED TO CHANGE THIS TO WORK WITH NON 1x2x1 PLAYERS LATER
    public bool PositionOccupied(Vector3Int position) {
        if (!alive) {
            Vector3Int roundedPos = Vector3Int.RoundToInt(transform.position);

            if (position.x == roundedPos.x - 1 || position.x == roundedPos.x) {
                if (position.y == roundedPos.y - 1 || position.y == roundedPos.y || position.y == roundedPos.y + 1) {
                    if (position.z == roundedPos.z - 1 || position.z == roundedPos.z) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void HandleCollisions() {
        // collide with players
        for (int i = Game.players.IndexOf(this); i < Game.players.Count; ++i) {
            Player other = Game.players[i];
            if (PlayerCollides(other.transform.position, out Vector3 normal, out float intersection_depth)) {
                transform.position          += 0.5f * normal * intersection_depth;
                other.transform.position    -= 0.5f * normal * intersection_depth;

                if (normal.y == 1.0f) {
                    lastGrounded = Time.frameCount;
                } else if (normal.y == -1.0f) {
                    other.lastGrounded = Time.frameCount;
                }
                
                Vector3 relative_velocity = velocity - other.velocity;
                float collisionSpeed = Vector3.Dot(relative_velocity, -normal);
                if (collisionSpeed > 0.0f) {
                    velocity        += 0.5f * collisionSpeed * normal;
                    other.velocity  -= 0.5f * collisionSpeed * normal;
                }
            }
        }

        // collide with blocks
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

                        if (BlockCollides(block_position, out Vector3 normal, out float intersection_depth)) {
                            transform.position += normal * intersection_depth;

                            if (normal.y == 1.0f) {
                                lastGrounded = Time.frameCount;
                            }

                            Vector3 tetromino_velocity = (Game.tetrominos[tetromino_id].falling) ? Vector3.down / Game.tickSize : Vector3.zero;
                            Vector3 relative_velocity = velocity - tetromino_velocity;
                            float collisionSpeed = Vector3.Dot(relative_velocity, -normal);
                            if (collisionSpeed > 0.0f) {
                                velocity += collisionSpeed * normal;
                            }

                            if (normal == Vector3.zero) {
                                // player was improperly seperated and must be being crushed
                                CrushPlayer();
                            }
                        }
                    }
                }
            }
        }

        // collide with floor
        float min_y = (0.5f * h + r) - 0.5f;
        if (transform.position.y <= min_y) {
            transform.position += (min_y - transform.position.y) * Vector3.up;
            velocity.y = Mathf.Max(velocity.y, 0.0f);
            lastGrounded = Time.frameCount;
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

    Vector3 ClampSpeedXZ(Vector3 velocity, float max_speed) {
        float speed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
        if (speed > max_speed) {
            velocity.x *= max_speed / speed;
            velocity.z *= max_speed / speed;
        }
        return velocity;
    }

    private void AliveUpdate() {
        Vector2 xz_input = controls.Player.Movement.ReadValue<Vector2>();
        Vector3 acceleration = Physics.gravity;
        if (lastGrounded + 1 >= Time.frameCount) {
            // speed of ground relative to player feet
            Vector2 relative_velocity_xz = new Vector2(
                xz_input[0] * SPEED - velocity.x,
                xz_input[1] * SPEED - velocity.z
            );
            // rescale to try and match desired offset this frame
            // clamp acceleration to respect drag from ground
            Vector2 acceleration_xz = Vector2.ClampMagnitude(
                relative_velocity_xz * 2.0f / Time.deltaTime, 
                MU * MASS * -acceleration.y
            );
            acceleration.x += acceleration_xz[0];
            acceleration.z += acceleration_xz[1];

            if (controls.Player.Jump.triggered) {
                velocity.y = JUMP_SPEED;
            }
        } else {
            acceleration.x += xz_input[0] * AIR_ACCELERATION;
            acceleration.z += xz_input[1] * AIR_ACCELERATION;
        }

        // displacement = v*t + (a/2)*t*t
        // v += (a/2)*t; for correct displacement
        velocity += acceleration * 0.5f * Time.deltaTime;
        velocity = ClampSpeedXZ(velocity, SPEED);
        transform.position += velocity * Time.deltaTime;
        // v += (a/2)*t; again for correct velocity
        velocity += acceleration * 0.5f * Time.deltaTime;
        velocity = ClampSpeedXZ(velocity, SPEED);

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
            // Move the block along the x and z axes
            if (controls.Player.BlockMove.triggered)
                selected_tetromino.XZMove(controls.Player.BlockMove.ReadValue<Vector2>());

            // Rotate the block around the x axis
            if (controls.Player.XRotate.triggered)
                selected_tetromino.Rotate(0, controls.Player.XRotate.ReadValue<float>());

            // Rotate the block around the y axis
            if (controls.Player.YRotate.triggered)
                selected_tetromino.Rotate(1, controls.Player.YRotate.ReadValue<float>());

            // Rotate the block around the z axis
            if (controls.Player.ZRotate.triggered)
                selected_tetromino.Rotate(2, controls.Player.ZRotate.ReadValue<float>());
        }
    }

    void Update() {
        if (alive)
            AliveUpdate();
        else
            DeadUpdate();
    }
}
