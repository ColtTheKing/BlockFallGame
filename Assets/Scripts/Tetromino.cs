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
    }
}

public class Tetromino : MonoBehaviour {
    public static readonly float MISALIGN_THRESHOLD = 0.3f;

    public GameObject[] blocks;
    public Vector3Int[] positions;
    public Vector3 pivot;
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

    // Determines whether or not a block overlaps with any other block or player in the world
    // If falling is false, fall_offset will be assumed not to apply to this tetromino
    public static bool TetrominoCollide(Vector3Int[] tetromino, int tetromino_id, bool falling) {
        for (int i = 0; i < tetromino.Length; i++) {
            // If a position is not empty or holding a block from the current tetromino, it must be colliding
            int id_at_position = Game.Terrain(tetromino[i]);

            // Check for other blocks or players at the terrain position of the block
            if ((id_at_position != -1 && id_at_position != tetromino_id))
                return true;

            for (int j = 0; j < Game.players.Count; j++) {
                if (Game.players[j].PositionOccupied(tetromino[i])) {
                    return true;
                }
            }

            // Check at the position above if in between layers
            if (falling && Game.fall_offset < 1f && Game.fall_offset > 0f) {
                id_at_position = Game.Terrain(tetromino[i] + Vector3Int.up);

                // For this layer, don't collide with falling tetrominos since they will not be overlapping
                if ((id_at_position != -1 && id_at_position != tetromino_id && !Game.tetrominos[id_at_position].falling))
                    return true;

                for (int j = 0; j < Game.players.Count; j++) {
                    if (Game.players[j].PositionOccupied(tetromino[i] + Vector3Int.up)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    bool ShouldFall() {
        // can't fall if supported from below by another block
        foreach (Vector3Int p in positions) {
            if (p.y == 0) return false;

            int potential_support = Game.voxel_terrain[p.x, p.y - 1, p.z];
            if (potential_support != Game.EMPTY && potential_support != id) {
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

    private bool TryTransformTetromino(Vector3Int[] transformed_tetromino, Vector3 transformed_pivot) {
        // If the new position is out of bounds, don't move
        for (int i = 0; i < transformed_tetromino.Length; i++) {
            if (transformed_tetromino[i].x < 0 || transformed_tetromino[i].x >= Game.WIDTH
                || transformed_tetromino[i].y < 0 || transformed_tetromino[i].y >= Game.HEIGHT
                || transformed_tetromino[i].z < 0 || transformed_tetromino[i].z >= Game.WIDTH) {
                return false;
            }
        }

        // Check if the new position would collide with anything
        if (TetrominoCollide(transformed_tetromino, id, true)) {
            // If it collides, check to see if nudging it down slightly would allow it to fit
            if (Game.fall_offset <= MISALIGN_THRESHOLD && !TetrominoCollide(transformed_tetromino, id, false)) {
                // If this allows it to fit, move it and set it to a non-falling tetromino
                ClearVoxels();

                for (int i = 0; i < positions.Length; i++)
                    positions[i] = transformed_tetromino[i];
                pivot = transformed_pivot;

                // Update the voxel values
                WriteVoxels();

                falling = false;

                return true;
            }
        }
        else {
            // If it doesn't collide, move the tetromino
            ClearVoxels();

            for (int i = 0; i < positions.Length; i++)
                positions[i] = transformed_tetromino[i];
            pivot = transformed_pivot;

            // Update the voxel values
            WriteVoxels();

            return true;
        }
        
        return false;
    }

    public bool XZMove(Vector2 xz_move) {
        Vector3Int[] new_positions = new Vector3Int[positions.Length];
        Vector3 new_pivot = pivot;
        for (int i = 0; i < new_positions.Length; i++)
            new_positions[i] = positions[i];

        for (int i = 0; i < new_positions.Length; i++) {
            new_positions[i] = positions[i] + new Vector3Int((int)xz_move.x, 0, (int)xz_move.y);
        }
        new_pivot += new Vector3Int((int)xz_move.x, 0, (int)xz_move.y);

        if (TryTransformTetromino(new_positions, new_pivot)) {
            positions = new_positions;
            pivot = new_pivot;
            return true;
        }

        return false;
    }

    public bool Rotate(int axis, float direction) {
        Vector3Int[] new_positions = new Vector3Int[positions.Length];
        Vector3 new_pivot = pivot;
        for (int i = 0; i < new_positions.Length; i++)
            new_positions[i] = positions[i];

        bool clockwise = direction > 0;

        for (int i = 0; i < new_positions.Length; i++) {
            Vector3 dist_from_pivot = positions[i] - pivot;

            Vector3 temp = RotatePoint90(dist_from_pivot, axis, clockwise);
            temp += pivot;

            new_positions[i] = Vector3Int.FloorToInt(temp);
        }

        if (TryTransformTetromino(new_positions, new_pivot)) {
            positions = new_positions;
            pivot = new_pivot;
            return true;
        }

        return false;
    }

    public void UpdateID(int new_id) {
        id = new_id;
    }

    ~Tetromino() {
        ClearVoxels();
    }
}
