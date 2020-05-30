using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
    public Tetromino[] PRESETS;
    public GameObject floor;

    public static readonly int SIZE = 16;
    public static readonly int EMPTY = -1;
    public static List<Tetromino> tetrominos = new List<Tetromino>();
    public static int[,,] voxel_terrain = new int[SIZE, SIZE, SIZE];

    // if a tetromino is falling this is the difference between its stored y position and its real y position
    public static float fall_offset = 0f;

    const float TICK_SIZE = 0.5f;
    const int SPAWN_PERIOD = 1;
    float time_since_last_tick = 0.0f;
    int ticks_to_spawn = 0;

    public static int Terrain(Vector3Int p) {
        if (p.x < 0 || p.x >= SIZE) return -1;
        if (p.y < 0 || p.y >= SIZE) return -1;
        if (p.z < 0 || p.z >= SIZE) return -1;
        return voxel_terrain[p.x, p.y, p.z];
    }

    void Start() {
        floor.transform.position = new Vector3(SIZE * 0.5f - 0.5f, -0.5f, SIZE * 0.5f - 0.5f);
        floor.transform.localScale = new Vector3(SIZE, SIZE, SIZE);

        for (int x = 0; x < SIZE; ++x) {
            for (int y = 0; y < SIZE; ++y) {
                for (int z = 0; z < SIZE; ++z) {
                    voxel_terrain[x, y, z] = EMPTY;
                }
            }
        }
    }

    void UpdateLogic() {
        if (ticks_to_spawn-- == 0) {
            ticks_to_spawn = SPAWN_PERIOD;
            Tetromino tetromino = Instantiate(PRESETS[Random.Range(0, PRESETS.Length)]);
            TetrominoFactory.FromPreset(tetromino);
            tetrominos.Add(tetromino);
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

    void UpdateVisuals() {
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
