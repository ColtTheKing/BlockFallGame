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

    //Takes a newly spawned tetromino and randomizes its parameters in the game world
    public static void FromPreset(Tetromino tetromino, int id) {
        // randomly shuffles the axis of each block position
        for (int i = 0; i < 2; ++i) {
            int r = Random.Range(i, 3);
            // Block positions
            for (int j = 0; j < tetromino.positions.Length; ++j) {
                int temp = tetromino.positions[j][r];
                tetromino.positions[j][r] = tetromino.positions[j][i];
                tetromino.positions[j][i] = temp;
            }
            // Pivot position
            float tempf = tetromino.pivot[r];
            tetromino.pivot[r] = tetromino.pivot[i];
            tetromino.pivot[i] = tempf;
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

        // Block positions
        for (int i = 0; i < tetromino.positions.Length; ++i) {
            Vector3Int p = tetromino.positions[i];
            if (mirror_x) p.x = max_x - p.x;
            if (mirror_y) p.y = max_y - p.y;
            if (mirror_z) p.z = max_z - p.z;
            p += offset;

            tetromino.positions[i] = p;
        }

        //Pivot position
        Vector3 pf = tetromino.pivot;
        if (mirror_x) pf.x = max_x - pf.x;
        if (mirror_y) pf.y = max_y - pf.y;
        if (mirror_z) pf.z = max_z - pf.z;
        pf += offset;

        tetromino.pivot = pf;

        tetromino.UpdateID(id);

        tetromino.owner = null;
    }
}

public class Tetromino : MonoBehaviour {
    public static readonly float MISALIGN_THRESHOLD = 0.3f;

    public GameObject[] blocks;
    public Vector3Int[] positions;
    public Vector3 pivot;
    public Player owner;
    public bool falling, controlled;
    private int id;

    // Takes point v and rotates it arounds the corresponding axis (0,1,2 correspond to x,y,z)
    public static Vector3 RotatePoint90(Vector3 v, int axis, bool clockwise) {
        int sign = clockwise ? 1 : -1;
        int A = (++axis < 3) ? axis : axis - 3;
        int B = (++axis < 3) ? axis : axis - 3;

        float temp = v[A];
        v[A] = v[B] * sign;
        v[B] = -temp * sign;

        return v;
    }

    bool ShouldFall() {
        // can't fall if supported from below by another block
        foreach (Vector3Int p in positions) {
            if (p.y == 0) return false;

            int index = Game.voxel_terrain[p.x, p.y - 1, p.z];
            if (index != Game.EMPTY && index != id && !(falling && Game.tetrominos[index].falling)) {
                return false;
            }
        }
        // otherwise should fall
        return true;
    }

    public void UpdateFalling() {
        falling = ShouldFall();
    }

    public void WriteVoxels() {
        // set voxels covered by tetronimo
        foreach (Vector3Int p in positions) {
            Game.voxel_terrain[p.x, p.y, p.z] = id;
        }
    }

    public void ClearVoxels() {
        foreach (Vector3Int p in positions) {
            Game.voxel_terrain[p.x, p.y, p.z] = Game.EMPTY;
        }
    }

    public void Fall() {
        if (falling) {
            ClearVoxels();
            for (int i = 0; i < positions.Length; ++i) {
                --positions[i].y;
            }
            --pivot.y;
            WriteVoxels();
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

    // Determines whether or not a block overlaps with any other block or player in the world
    private bool PositionsLegal(Vector3Int[] new_positions, bool incrementLayer, bool ignoreFalling) {
        for (int i = 0; i < new_positions.Length; i++) {
            Vector3Int position = new_positions[i];
            if (incrementLayer) ++position.y;

            if (position.x < 0 || position.x >= Game.WIDTH
                || position.y < 0 || position.y >= Game.HEIGHT
                || position.z < 0 || position.z >= Game.WIDTH) {
                return false;
            }

            // Check for other blocks
            int index = Game.Terrain(position);
            if (index != -1 && index != id) {
                if (!ignoreFalling || !Game.tetrominos[index].falling) {
                    return false;
                }
            }

            // Check for players
            for (int j = 0; j < Game.players.Count; j++) {
                if (Game.players[j].PositionOccupied(position)) {
                    return false;
                }
            }
        }

        return true;
    }

    void ApplyTransform(Vector3Int[] new_positions, Vector3 new_pivot) {
        // Update the voxel values
        ClearVoxels();
        positions = new_positions;
        WriteVoxels();

        pivot = new_pivot;
    }

    bool TryTransform(Vector3Int[] new_positions, Vector3 new_pivot, bool try_settle) {
        bool lower_legal = PositionsLegal(new_positions, false, false);
        if (!falling) {
            if (lower_legal) ApplyTransform(new_positions, new_pivot);
            return lower_legal;
        }

        bool upper_legal = PositionsLegal(new_positions, true, lower_legal);

        if (!lower_legal && !upper_legal) {
            return false;
        }
        if (!try_settle && (!lower_legal || !upper_legal)) {
            return false;
        }

        if (!upper_legal) {
            // check if we can settle down to make legal
            if (Game.fall_offset > MISALIGN_THRESHOLD) {
                return false;
            }
            falling = false;
        }
        if (!lower_legal) {
            // check if we can settle up to make legal
            if (1.0f - Game.fall_offset > MISALIGN_THRESHOLD) {
                return false;
            }
            for (int i = 0; i < new_positions.Length; i++) {
                ++new_positions[i].y;
            }
            falling = false;
        }

        ApplyTransform(new_positions, new_pivot);
        return true;
    }

    public bool XZMove(Vector2 xz_move) {
        Vector3Int[] new_positions = new Vector3Int[positions.Length];
        for (int i = 0; i < new_positions.Length; i++) {
            new_positions[i] = positions[i] + new Vector3Int((int)xz_move.x, 0, (int)xz_move.y);
        }
        Vector3 new_pivot = pivot + new Vector3Int((int)xz_move.x, 0, (int)xz_move.y);

        return TryTransform(new_positions, new_pivot, true);
    }

    public bool Rotate(int axis, float direction) {
        Vector3Int[] new_positions = new Vector3Int[positions.Length];
        bool clockwise = direction > 0;
        for (int i = 0; i < new_positions.Length; i++) {
            Vector3 temp = positions[i];
            temp -= pivot;
            temp = RotatePoint90(temp, axis, clockwise);
            temp += pivot;
            new_positions[i] = Vector3Int.RoundToInt(temp);
        }

        return TryTransform(new_positions, pivot, true);
    }

    public void UpdateID(int new_id) {
        id = new_id;
    }

    ~Tetromino() {
        ClearVoxels();
    }
}
