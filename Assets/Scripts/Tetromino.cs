using System.Collections.Generic;
using UnityEngine;
using Math = System.Math;

public class TetrominoFactory {
    static int Max(Vector3Int[] positions, int axis) {
        int max = positions[0][axis];
        for (int i = 1; i < positions.Length; ++i) {
            max = Math.Max(max, positions[i][axis]);
        };
        return max;
    }

    static int Min(Vector3Int[] positions, int axis) {
        int min = positions[0][axis];
        for (int i = 1; i < positions.Length; ++i) {
            min = Math.Max(min, positions[i][axis]);
        };
        return min;
    }

    public static void FromPreset(Tetromino tetromino) {
        // randomly shuffles the axis of each block position
        for (int i = 0; i < 2; ++i) {
            int r = Random.Range(i, 3);
            for (int j = 0; j < tetromino.positions.Length; ++j) {
                int temp = tetromino.positions[j][r];
                tetromino.positions[j][r] = tetromino.positions[j][i];
                tetromino.positions[j][i] = temp;
            }
        }

        int max_x = Max(tetromino.positions, 0);
        int max_y = Max(tetromino.positions, 1);
        int max_z = Max(tetromino.positions, 2);

        // offset randomly in [x,z] inside game bounds
        // ensure that tetromino is kissing the top of the game area
        Vector3Int offset = new Vector3Int(
            Random.Range(0, Game.WIDTH - max_x),
            Game.HEIGHT - max_y - 1,
            Random.Range(0, Game.WIDTH - max_z)
        );

        bool mirror_x = Random.value < 0.5f;
        bool mirror_y = Random.value < 0.5f;
        bool mirror_z = Random.value < 0.5f;

        for (int i = 0; i < tetromino.positions.Length; ++i) {
            Vector3Int p = tetromino.positions[i];
            if (mirror_x) p.x = max_x - p.x;
            if (mirror_y) p.y = max_y - p.y;
            if (mirror_z) p.z = max_z - p.z;
            p += offset;

            tetromino.positions[i] = p;
        }
    }
}

public class Tetromino : MonoBehaviour {
    public GameObject[] blocks;
    public Vector3Int[] positions;
    public bool falling;

    bool ShouldFall(int index) {
        // can't fall if supported from below by another block
        foreach (Vector3Int p in positions) {
            if (p.y == 0) return false;

            int potential_support = Game.voxel_terrain[p.x, p.y - 1, p.z];
            if (potential_support != Game.EMPTY && potential_support != index) {
                return false;
            }
        }
        // otherwise should fall
        return true;
    }

    public void UpdateFalling(int index) {
        falling = ShouldFall(index);
    }

    public void WriteVoxels(int index) {
        // set voxels covered by tetronimo
        foreach (Vector3Int p in positions) {
            Game.voxel_terrain[p.x, p.y, p.z] = index;
        }
    }

    public void ClearVoxels() {
        WriteVoxels(Game.EMPTY);
    }

    public void Fall(int index) {
        if (falling) {
            ClearVoxels();
            for (int i = 0; i < positions.Length; ++i) {
                --positions[i].y;
            }
            WriteVoxels(index);
        }
    }

    public void UpdateVisuals() {
        // if falling y is interpolated between current and previous value
        float fall_offset = falling ? Game.fall_offset : 0f;
        // update positions of each 
        for (int i = 0; i < positions.Length; ++i) {
            Vector3Int p = positions[i];
            blocks[i].transform.localPosition = new Vector3(p.x, p.y + fall_offset, p.z);
        }
    }

    public void RemoveBottom() {
        RemoveBlocksIf(p => p.y == 0);
    }

    public void RemoveBlocksIf(System.Predicate<Vector3Int> predicate) {
        // index of first block to be removed
        int first = 0;
        while (first < positions.Length && !predicate(positions[first])) ++first;

        // return early if no blocks need to be removed
        if (first == positions.Length) return;

        for (int i = first; i < positions.Length; ++i) {
            if (predicate(positions[i])) {
                // destroy blocks to remove
                Game.voxel_terrain[positions[i].x, 0, positions[i].z] = Game.EMPTY;
                Destroy(blocks[i]);
            } else {
                // move blocks to keep towards start
                positions[first] = positions[i];
                blocks[first] = blocks[i];
                ++first;
            }
        }

        System.Array.Resize(ref blocks, first);
        System.Array.Resize(ref positions, first);
    }

    ~Tetromino() {
        ClearVoxels();
    }
}
