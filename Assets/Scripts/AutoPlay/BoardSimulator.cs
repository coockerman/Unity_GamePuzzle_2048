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

        foreach (var tile in board.GetTiles())
        {
            Vector2Int pos = tile.cell.coordinates;
            grid[pos.x, pos.y] = tile.state.number;
        }
    }

    private BoardSimulator(int[,] otherGrid)
    {
        width = otherGrid.GetLength(0);
        height = otherGrid.GetLength(1);
        grid = (int[,])otherGrid.Clone();
    }

    public BoardSimulator Clone() => new BoardSimulator(grid);

    public bool CanMove(Vector2Int direction)
    {
        return Clone().Move(direction);
    }

    public bool Move(Vector2Int direction)
    {
        bool moved = false;
        (int startX, int endX, int stepX) = GetLoopParams(direction.x, width);
        (int startY, int endY, int stepY) = GetLoopParams(direction.y, height);

        for (int x = startX; x != endX; x += stepX)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                if (grid[x, y] == 0) continue;
                moved |= SlideAndMerge(x, y, direction);
            }
        }
        return moved;
    }

    private (int start, int end, int step) GetLoopParams(int dirAxis, int size)
    {
        if (dirAxis > 0) return (size - 2, -1, -1);
        if (dirAxis < 0) return (1, size, 1);
        return (0, size, 1);
    }

    private bool SlideAndMerge(int startX, int startY, Vector2Int dir)
    {
        bool changed = false;
        int x = startX, y = startY;

        while (true)
        {
            int nextX = x + dir.x;
            int nextY = y + dir.y;

            if (!IsInside(nextX, nextY)) break;

            if (grid[nextX, nextY] == 0)
            {
                grid[nextX, nextY] = grid[x, y];
                grid[x, y] = 0;
                x = nextX; y = nextY;
                changed = true;
            }
            else if (grid[nextX, nextY] == grid[x, y])
            {
                grid[nextX, nextY] *= 2;
                grid[x, y] = 0;
                changed = true;
                break;
            }
            else break;
        }
        return changed;
    }

    private bool IsInside(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height);

    public void SpawnCell(Vector2Int cell, int value)
    {
        grid[cell.x, cell.y] = value;
    }

    public List<Vector2Int> GetEmptyCells()
    {
        var emptyCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == 0)
                    emptyCells.Add(new Vector2Int(x, y));
        return emptyCells;
    }

    public bool IsGameOver()
    {
        if (GetEmptyCells().Count > 0) return false;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            if (Clone().Move(dir))
                return false;
        }
        return true;
    }

    public float Evaluate()
    {
        float score = 0;
        int emptyCells = GetEmptyCells().Count;
        score += emptyCells * 200f;
        score += CalculateSmoothness() * 0.5f;
        score += CalculateMonotonicity() * 1.0f;
        int maxTile = GetMaxTile();
        score += Mathf.Log(maxTile == 0 ? 1 : maxTile) * 10f;
        return score;
    }

    private int GetMaxTile()
    {
        int maxTile = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                maxTile = Mathf.Max(maxTile, grid[x, y]);
        return maxTile;
    }

    private float CalculateMonotonicity()
    {
        float score = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width - 1; x++)
                score -= Mathf.Max(0, grid[x + 1, y] - grid[x, y]);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height - 1; y++)
                score -= Mathf.Max(0, grid[x, y + 1] - grid[x, y]);
        return -score;
    }

    private float CalculateSmoothness()
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
