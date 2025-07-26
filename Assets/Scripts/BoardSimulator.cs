using System.Collections.Generic;
using UnityEngine;

public class BoardSimulator
{
    private int[,] grid;
    private int width, height;

    public BoardSimulator(TileBoard board)
    {
        width = board.Width;
        height = board.Height;
        grid = new int[width, height];
        var tiles = board.GetTiles();
        foreach (var tile in tiles)
        {
            Vector2Int c = tile.cell.coordinates;
            grid[c.x, c.y] = tile.state.number;
        }
    }

    public bool CanMove(Vector2Int dir)
    {
        var copy = Clone();
        return copy.Move(dir);
    }

    
    private BoardSimulator(int[,] otherGrid)
    {
        width = otherGrid.GetLength(0);
        height = otherGrid.GetLength(1);
        grid = (int[,])otherGrid.Clone();
    }

    public BoardSimulator Clone()
    {
        return new BoardSimulator(grid);
    }

    public bool Move(Vector2Int dir)
    {
        bool changed = false;
        int startX = (dir == Vector2Int.right) ? width - 2 : 0;
        int endX = (dir == Vector2Int.right) ? -1 : width;
        int stepX = (dir == Vector2Int.right) ? -1 : 1;

        int startY = (dir == Vector2Int.down) ? height - 2 : 0;
        int endY = (dir == Vector2Int.down) ? -1 : height;
        int stepY = (dir == Vector2Int.down) ? -1 : 1;

        for (int x = startX; x != endX; x += stepX)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                if (grid[x, y] == 0) continue;
                int newX = x, newY = y;
                while (true)
                {
                    int nextX = newX + dir.x;
                    int nextY = newY + dir.y;
                    if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) break;

                    if (grid[nextX, nextY] == 0)
                    {
                        grid[nextX, nextY] = grid[newX, newY];
                        grid[newX, newY] = 0;
                        newX = nextX; newY = nextY;
                        changed = true;
                    }
                    else if (grid[nextX, nextY] == grid[newX, newY])
                    {
                        grid[nextX, nextY] *= 2;
                        grid[newX, newY] = 0;
                        changed = true;
                        break;
                    }
                    else break;
                }
            }
        }
        return changed;
    }

    public void SpawnCell(Vector2Int cell, int value)
    {
        grid[cell.x, cell.y] = value;
    }

    public List<Vector2Int> GetEmptyCells()
    {
        var list = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == 0)
                    list.Add(new Vector2Int(x, y));
        return list;
    }

    public bool IsGameOver()
    {
        if (GetEmptyCells().Count > 0) return false;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            var copy = Clone();
            if (copy.Move(dir))
                return false;
        }
        return true;
    }

    public float Evaluate()
    {
        float score = 0;

        // 1. Ưu tiên nhiều ô trống
        int empty = GetEmptyCells().Count;
        score += empty * 200f;

        // 2. Smoothness (càng "mượt" càng tốt)
        score += CalcSmoothness() * 0.5f;

        // 3. Monotonicity (ưu tiên hàng/cột giảm dần)
        score += CalcMonotonicity() * 1.0f;

        // 4. Khuyến khích số lớn (có thể giảm trọng số này)
        int maxTile = 0;
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            maxTile = Mathf.Max(maxTile, grid[x, y]);
        score += Mathf.Log(maxTile) * 10f; // log để tránh lệch quá nhiều

        return score;
    }

    private float CalcMonotonicity()
    {
        float monoScore = 0;

        // Kiểm tra từng hàng
        for (int y = 0; y < height; y++)
        {
            float current = 0, next = 0;
            for (int x = 0; x < width - 1; x++)
            {
                current = grid[x, y];
                next = grid[x + 1, y];
                if (next > current) monoScore -= (next - current);
            }
        }

        // Kiểm tra từng cột
        for (int x = 0; x < width; x++)
        {
            float current = 0, next = 0;
            for (int y = 0; y < height - 1; y++)
            {
                current = grid[x, y];
                next = grid[x, y + 1];
                if (next > current) monoScore -= (next - current);
            }
        }

        return -monoScore; // càng ít vi phạm tăng giảm thì điểm càng cao
    }


    private float CalcSmoothness()
    {
        float smoothness = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == 0) continue;
                if (x + 1 < width && grid[x + 1, y] != 0)
                    smoothness -= Mathf.Abs(grid[x, y] - grid[x + 1, y]);
                if (y + 1 < height && grid[x, y + 1] != 0)
                    smoothness -= Mathf.Abs(grid[x, y] - grid[x, y + 1]);
            }
        }
        return -smoothness;
    }
}
