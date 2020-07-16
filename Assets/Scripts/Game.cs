using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour {
    public Tetromino[] PRESETS;
    public GameObject floor;
    public int DESTROY_BLOCKS_PERIOD;
    public int SPAWN_PERIOD;
    public int DESTROY_BLOCKS_DELAY;
    public Lava lava;
    public Player[] players;

    public static readonly int WIDTH = 16;
    public static readonly int HEIGHT = 16;
    public static readonly int EMPTY = -1;
    public static List<Tetromino> tetrominos = new List<Tetromino>();
    public static int[,,] voxel_terrain = new int[WIDTH, HEIGHT, WIDTH];

    // if a tetromino is falling this is the difference between its stored y position and its real y position
    public static float fall_offset = 0f;
    public static int num_players = 4;
    public static int dead_players = 0;

    const float TICK_SIZE = 0.5f;
    float time_since_last_tick = 0.0f;
    int ticks_to_spawn = 0;
    int ticks_to_destroy_blocks = 0;

    public static int Terrain(Vector3Int p) {
        if (p.x < 0 || p.x >= WIDTH) return -1;
        if (p.y < 0 || p.y >= HEIGHT) return -1;
        if (p.z < 0 || p.z >= WIDTH) return -1;
        return voxel_terrain[p.x, p.y, p.z];
    }

    public static void PlayerDied() {
        if (++dead_players >= num_players - 1)
            EndGame();
    }

    public static void EndGame() {
        // determine the winner and send them back to the menu or something
        SceneManager.LoadScene("Menu");
    }

    public void Awake() {
        ticks_to_destroy_blocks = DESTROY_BLOCKS_DELAY;
        num_players = players.Length;

        foreach (Player p in players) {
            p.SetLava(lava);
        }
    }

    void Start() {
        floor.transform.position = new Vector3(WIDTH * 0.5f - 0.5f, -0.5f, WIDTH * 0.5f - 0.5f);
        floor.transform.localScale = new Vector3(WIDTH, HEIGHT, WIDTH);

        for (int x = 0; x < WIDTH; ++x) {
            for (int y = 0; y < HEIGHT; ++y) {
                for (int z = 0; z < WIDTH; ++z) {
                    voxel_terrain[x, y, z] = EMPTY;
                }
            }
        }
    }

    // Destroys the bottom layer of blocks, cauing the rest to fall
    private void DestroyBottomLayer() {
        HashSet<int> indices_to_delete = new HashSet<int>();

        for (int x = 0; x < WIDTH; ++x) {
            for (int z = 0; z < WIDTH; ++z) {
                int tetromino_id = voxel_terrain[x, 0, z];

                if (tetromino_id != EMPTY)
                    indices_to_delete.Add(tetromino_id);
            }
        }

        //Removes the bottom blocks in any tetromino that was touching the ground
        foreach (int index in indices_to_delete)
            tetrominos[index].RemoveBottom();

        //Delete any tetrominos that have no blocks
        for (int i = 0; i < tetrominos.Count; i++) {
            if (tetrominos[i].blocks.Length == 0) {
                Destroy(tetrominos[i].gameObject);
                tetrominos.RemoveAt(i--);
            }
        }
    }

    //Grab the newest block that isn't already in use
    public static Tetromino GrabTetromino(Material material) {
        // add colour to the blocks based on the player (make this a glow or something later?)
        Tetromino t = tetrominos[tetrominos.Count - 1];

        MeshRenderer[] blocks = t.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < blocks.Length; i++) {
            blocks[i].material = material;
        }

        return t;
    }

    private void UpdateLogic() {
        // handle tetromino spawns
        if (ticks_to_spawn-- == 0) {
            ticks_to_spawn = SPAWN_PERIOD;

            Tetromino tetromino = Instantiate(PRESETS[Random.Range(0, PRESETS.Length)]);
            TetrominoFactory.FromPreset(tetromino);
            tetrominos.Add(tetromino);
        }

        // handle mass tetromino destruction
        if (ticks_to_destroy_blocks-- == 0) {
            ticks_to_destroy_blocks = DESTROY_BLOCKS_PERIOD;

            DestroyBottomLayer();
        }

        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].WriteVoxels(i);
        }
        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].UpdateFalling(i);
        }
        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].Fall(i);
        }

        // do stuff that would change indices here
    }

    private void UpdateVisuals() {
        foreach (Tetromino tetromino in tetrominos) {
            tetromino.UpdateVisuals();
        }
    }

    void Update() {
        time_since_last_tick += Time.deltaTime;
        while (time_since_last_tick > TICK_SIZE) {
            time_since_last_tick -= TICK_SIZE;
            UpdateLogic();
        }

        fall_offset = 1f - time_since_last_tick / TICK_SIZE;
        UpdateVisuals();
    }
}
