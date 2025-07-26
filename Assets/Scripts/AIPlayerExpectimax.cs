using UnityEngine;
using System.Collections.Generic;

public class AIPlayerExpectimax : MonoBehaviour
{
    [SerializeField] private TileBoard board;
    [SerializeField] private float moveDelay = 0.2f;
    [SerializeField] private int searchDepth = 4;
    [SerializeField] private int recentMoveMemory = 2; // nhớ 2 hướng gần nhất
    public bool isActiveAI { get; private set; } = false;
    private float timer;
    private int[,] lastGrid;
    private Queue<Vector2Int> recentMoves = new Queue<Vector2Int>();

    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private void Update()
    {
        if (!board.enabled) return; // Game Over (board bị disable)

        if (!isActiveAI) return;
        // Kiểm tra Game Over
        if (board.CheckForGameOver())
        {
            Debug.Log("[AI] Game Over!");
            GameManager.Instance.GameOver();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= moveDelay)
        {
            timer = 0f;
            MakeMove();
        }
    }

    public void SetActiveAI(bool status)
    {
        isActiveAI = status;
    }

    private void MakeMove()
    {
        SaveBoardState();

        Vector2Int? bestMove = FindBestMove();
        if (!bestMove.HasValue)
        {
            Debug.Log("[AI] Không có nước đi hợp lệ.");
            return;
        }

        board.MoveByAI(bestMove.Value);
        AddRecentMove(bestMove.Value);
        Debug.Log($"[AI] Chọn hướng: {bestMove.Value}");

        if (IsBoardUnchanged())
        {
            Debug.LogWarning("[AI] Bàn cờ không thay đổi -> thử hướng khác");
            TryAlternativeMove(bestMove.Value);
        }
    }

    private Vector2Int? FindBestMove()
    {
        float bestScore = float.NegativeInfinity;
        Vector2Int? bestDir = null;

        foreach (var dir in Directions)
        {
            if (IsRecentMove(dir)) continue; // tránh lặp hướng

            var sim = new BoardSimulator(board);
            if (sim.Move(dir))
            {
                float score = Expectimax(sim, searchDepth - 1, false);
                Debug.Log($"[AI] Score {dir}: {score}");
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }
        }

        return bestDir;
    }

    private void TryAlternativeMove(Vector2Int lastMove)
    {
        List<Vector2Int> alternatives = new List<Vector2Int>();
        foreach (var dir in Directions)
        {
            if (dir == lastMove || IsRecentMove(dir)) continue;
            if (new BoardSimulator(board).CanMove(dir))
                alternatives.Add(dir);
        }

        foreach (var alt in alternatives)
        {
            SaveBoardState();
            board.MoveByAI(alt);
            if (!IsBoardUnchanged())
            {
                AddRecentMove(alt);
                Debug.Log($"[AI] Nước thay thế: {alt}");
                return;
            }
        }

        if (alternatives.Count > 0)
        {
            Vector2Int randomAlt = alternatives[Random.Range(0, alternatives.Count)];
            AddRecentMove(randomAlt);
            Debug.LogWarning($"[AI] Fallback random: {randomAlt}");
            board.MoveByAI(randomAlt);
        }
        else
        {
            // Nếu thật sự kẹt, chọn hướng ngẫu nhiên bất kỳ
            Vector2Int randomDir = Directions[Random.Range(0, Directions.Length)];
            AddRecentMove(randomDir);
            Debug.LogWarning($"[AI] Không có hướng khác -> Random bất kỳ: {randomDir}");
            board.MoveByAI(randomDir);
        }
    }

    private void SaveBoardState()
    {
        lastGrid = new int[board.Width, board.Height];
        foreach (var tile in board.GetTiles())
            lastGrid[tile.cell.coordinates.x, tile.cell.coordinates.y] = tile.state.number;
    }

    private bool IsBoardUnchanged()
    {
        foreach (var tile in board.GetTiles())
        {
            Vector2Int c = tile.cell.coordinates;
            if (lastGrid[c.x, c.y] != tile.state.number)
                return false;
        }
        return true;
    }

    private void AddRecentMove(Vector2Int move)
    {
        if (recentMoves.Count >= recentMoveMemory)
            recentMoves.Dequeue();
        recentMoves.Enqueue(move);
    }

    private bool IsRecentMove(Vector2Int move)
    {
        return recentMoves.Contains(move);
    }

    private float Expectimax(BoardSimulator sim, int depth, bool isAI)
    {
        if (depth == 0 || sim.IsGameOver())
            return sim.Evaluate();

        if (isAI)
        {
            float best = float.NegativeInfinity;
            foreach (var dir in Directions)
            {
                var s = sim.Clone();
                if (s.Move(dir))
                    best = Mathf.Max(best, Expectimax(s, depth - 1, false));
            }
            return best == float.NegativeInfinity ? sim.Evaluate() : best;
        }
        else
        {
            var empty = sim.GetEmptyCells();
            if (empty.Count == 0) return sim.Evaluate();

            float total = 0f;
            foreach (var cell in empty)
            {
                var s2 = sim.Clone(); s2.SpawnCell(cell, 2);
                total += 0.9f * Expectimax(s2, depth - 1, true);

                var s4 = sim.Clone(); s4.SpawnCell(cell, 4);
                total += 0.1f * Expectimax(s4, depth - 1, true);
            }
            return total / empty.Count;
        }
    }
}
