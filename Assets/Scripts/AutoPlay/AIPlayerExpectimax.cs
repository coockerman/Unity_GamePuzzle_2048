using UnityEngine;
using System.Collections.Generic;

public class AIPlayerExpectimax : MonoBehaviour
{
    [SerializeField] private TileBoard board;
    [SerializeField] private float moveDelay = 0.2f;
    [SerializeField] private int searchDepth = 4;
    [SerializeField] private int recentMoveMemory = 2;
    
    public bool IsActive { get; private set; } = false;

    private float moveTimer;
    private int[,] previousGrid;
    private Queue<Vector2Int> recentDirections = new Queue<Vector2Int>();

    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private void Update()
    {
        if (!board.enabled || !IsActive) return;

        if (board.CheckForGameOver())
        {
            GameManager.Instance.GameOver();
            return;
        }

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveDelay)
        {
            moveTimer = 0f;
            ExecuteMove();
        }
    }

    public void SetActiveAI(bool status)
    {
        IsActive = status;
    }

    private void ExecuteMove()
    {
        SaveCurrentBoard();

        Vector2Int? bestDirection = ChooseBestDirection();
        if (!bestDirection.HasValue) return;

        board.MoveByAI(bestDirection.Value);
        RememberMove(bestDirection.Value);

        if (BoardIsUnchanged())
        {
            TryAlternativeDirection(bestDirection.Value);
        }
    }

    private Vector2Int? ChooseBestDirection()
    {
        float bestScore = float.NegativeInfinity;
        Vector2Int? chosenDirection = null;

        foreach (var direction in Directions)
        {
            if (IsMoveRecentlyUsed(direction)) continue;

            var simBoard = new BoardSimulator(board);
            if (simBoard.Move(direction))
            {
                float score = Expectimax(simBoard, searchDepth - 1, false);
                if (score > bestScore)
                {
                    bestScore = score;
                    chosenDirection = direction;
                }
            }
        }
        return chosenDirection;
    }

    private void TryAlternativeDirection(Vector2Int lastDirection)
    {
        List<Vector2Int> validAlternatives = new List<Vector2Int>();
        foreach (var direction in Directions)
        {
            if (direction == lastDirection || IsMoveRecentlyUsed(direction)) continue;
            if (new BoardSimulator(board).CanMove(direction))
                validAlternatives.Add(direction);
        }

        foreach (var altDir in validAlternatives)
        {
            SaveCurrentBoard();
            board.MoveByAI(altDir);
            if (!BoardIsUnchanged())
            {
                RememberMove(altDir);
                return;
            }
        }

        Vector2Int fallbackDir = validAlternatives.Count > 0
            ? validAlternatives[Random.Range(0, validAlternatives.Count)]
            : Directions[Random.Range(0, Directions.Length)];

        RememberMove(fallbackDir);
        board.MoveByAI(fallbackDir);
    }

    private void SaveCurrentBoard()
    {
        previousGrid = new int[board.Width, board.Height];
        foreach (var tile in board.GetTiles())
            previousGrid[tile.cell.coordinates.x, tile.cell.coordinates.y] = tile.state.number;
    }

    private bool BoardIsUnchanged()
    {
        foreach (var tile in board.GetTiles())
        {
            Vector2Int pos = tile.cell.coordinates;
            if (previousGrid[pos.x, pos.y] != tile.state.number)
                return false;
        }
        return true;
    }

    private void RememberMove(Vector2Int move)
    {
        if (recentDirections.Count >= recentMoveMemory)
            recentDirections.Dequeue();
        recentDirections.Enqueue(move);
    }

    private bool IsMoveRecentlyUsed(Vector2Int move)
    {
        return recentDirections.Contains(move);
    }

    private float Expectimax(BoardSimulator sim, int depth, bool isAITurn)
    {
        if (depth == 0 || sim.IsGameOver())
            return sim.Evaluate();

        if (isAITurn)
        {
            float best = float.NegativeInfinity;
            foreach (var dir in Directions)
            {
                var clone = sim.Clone();
                if (clone.Move(dir))
                    best = Mathf.Max(best, Expectimax(clone, depth - 1, false));
            }
            return best == float.NegativeInfinity ? sim.Evaluate() : best;
        }
        else
        {
            var emptyCells = sim.GetEmptyCells();
            if (emptyCells.Count == 0) return sim.Evaluate();

            float totalScore = 0f;
            foreach (var cell in emptyCells)
            {
                var s2 = sim.Clone(); s2.SpawnCell(cell, 2);
                totalScore += 0.9f * Expectimax(s2, depth - 1, true);

                var s4 = sim.Clone(); s4.SpawnCell(cell, 4);
                totalScore += 0.1f * Expectimax(s4, depth - 1, true);
            }
            return totalScore / emptyCells.Count;
        }
    }
}
