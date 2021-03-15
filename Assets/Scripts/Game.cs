using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour {
    [Header("Presets")]
    public Tetromino[] tetrominoPresets;
    public GameObject[] playerPrefabs;
    [Header("GameObjects")]
    public GameObject floorGameObject;
    public GameObject lavaGameObject;
    [Header("Settings")]
    public int tetrominoSpawnPeriod;
    public int destroyBlockDelay;
    public int destroyBlockPeriod;

    public static float tickSize = 0.5f;
    public static readonly int WIDTH = 16;
    public static readonly int HEIGHT = 16;
    public static readonly int EMPTY = -1;
    public static readonly Vector3 SPAWN_POSITION = new Vector3(7.5f, 17f, 7.5f);
    public static List<Tetromino> tetrominos;
    public static List<Player> players;
    public static Lava lava;
    public static int[,,] voxel_terrain = new int[WIDTH, HEIGHT, WIDTH];
    // if a tetromino is falling this is the difference between its stored y position and its real y position
    public static float fall_offset;
    public static int dead_players;

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
        if (++dead_players >= players.Count - 1) {
            //EndGame();
        }
    }

    internal static void PlayerRessurected() {
        --dead_players;
    }

    public static void EndGame() {
        // determine the winner and send them back to the menu or something
        SceneManager.LoadScene("Menu");
    }

    public void Awake() {
        players = new List<Player>();
        tetrominos = new List<Tetromino>();
        lava = lavaGameObject.GetComponent<Lava>();

        GameObject infoObj = GameObject.Find("GameInfo");
        if (infoObj != null) {
            GameInfo info = infoObj.GetComponent<GameInfo>();
            for (int i = 0; i < info.players.Count; i++) {
                Vector3 spawnPosition = new Vector3(
                    (WIDTH - 1) * (i % 2),
                    0.5f,
                    (WIDTH - 1) * (i / 2 % 2)
                );
                GameObject gameObject = Instantiate(playerPrefabs[i], spawnPosition, Quaternion.identity);
                Player player = gameObject.GetComponent<Player>();
                player.SetInputDevice(info.players[i]);
                players.Add(player);
            }
        } else {
            // should only execute when running the game scene manually from the editor.
            Vector3 spawnPosition = new Vector3(0.0f, 0.5f, 0.0f);
            GameObject gameObject = Instantiate(playerPrefabs[0], spawnPosition, Quaternion.identity);
            Player player = gameObject.GetComponent<Player>();
            players.Add(player);
        }

        fall_offset = 0.0f;
        dead_players = 0;
        ticks_to_destroy_blocks = destroyBlockDelay;
    }

    void Start() {
        floorGameObject.transform.position = new Vector3(WIDTH * 0.5f - 0.5f, -0.5f, WIDTH * 0.5f - 0.5f);
        floorGameObject.transform.localScale = new Vector3(WIDTH, HEIGHT, WIDTH);

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

    //Grab the newest tetromino that isn't already taken or on the ground
    public static Tetromino GrabTetromino(Material material) {
        for (int i = tetrominos.Count - 1; i >= 0; i--) {
            if (!tetrominos[i].controlled && tetrominos[i].falling) {
                Tetromino tetromino = tetrominos[i];
                tetromino.controlled = true;

                // add colour to the blocks based on the player (make this a glow or something later?)
                MeshRenderer[] blocks = tetromino.GetComponentsInChildren<MeshRenderer>();
                for (int j = 0; j < blocks.Length; j++) {
                    blocks[j].material = material;
                }

                return tetromino;
            }
        }

        return null;
    }

    private void UpdateLogic() {
        // handle tetromino spawns
        if (ticks_to_spawn-- == 0) {
            ticks_to_spawn = tetrominoSpawnPeriod;

            Tetromino tetromino = Instantiate(tetrominoPresets[Random.Range(0, tetrominoPresets.Length)]);
            TetrominoFactory.FromPreset(tetromino, tetrominos.Count);
            tetrominos.Add(tetromino);
        }

        // handle mass tetromino destruction
        if (ticks_to_destroy_blocks-- == 0) {
            ticks_to_destroy_blocks = destroyBlockPeriod;

            DestroyBottomLayer();

            // Update the ids in case the array indices shifted due to a deletion
            for (int i = 0; i < tetrominos.Count; ++i) {
                tetrominos[i].UpdateID(i);
            }
        }

        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].WriteVoxels();
        }
        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].UpdateFalling();
        }
        for (int i = 0; i < tetrominos.Count; ++i) {
            tetrominos[i].Fall();
        }
    }

    private void UpdateVisuals() {
        foreach (Tetromino tetromino in tetrominos) {
            tetromino.UpdateVisuals();
        }
    }

    void Update() {
        time_since_last_tick += Time.deltaTime;
        while (time_since_last_tick > tickSize) {
            time_since_last_tick -= tickSize;
            UpdateLogic();
        }

        fall_offset = 1f - time_since_last_tick / tickSize;
        UpdateVisuals();
    }
}
