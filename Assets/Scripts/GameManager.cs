using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TileBoard board;
    [SerializeField] private CanvasGroup gameOver;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;
    [SerializeField] private string highScoreName = "";
    private AIPlayerExpectimax ai;
    
    public int score { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject); 
            //Todo Immediate??
        } else {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        ai = GetComponent<AIPlayerExpectimax>();
        NewGame();
    }

    public void NewGame()
    {
        
        // reset score
        SetScore(0);
        hiscoreText.text = LoadHiscore().ToString();

        // hide game over screen
        gameOver.alpha = 0f;
        gameOver.interactable = false;

        // update board state
        board.ClearBoard();
        board.CreateTile();
        board.CreateTile();
        board.enabled = true;
        
    }

    public void ChangeStatusAI()
    {
        if (ai != null)
        {
            ai.SetActiveAI(!ai.IsActive);
            Debug.Log("AI Status Changed: " + ai.IsActive);
        }
    }
    public void GameOver()
    {
        if (ai != null)
        {
            Debug.Log("[GameManager] Game Over!");
            ai.SetActiveAI(false);
        }
            
        board.enabled = false;
        gameOver.interactable = true;

        StartCoroutine(Fade(gameOver, 1f, 1f));
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    public void IncreaseScore(int points)
    {
        SetScore(score + points);
    }

    private void SetScore(int score)
    {
        this.score = score;
        scoreText.text = score.ToString();

        SaveHiscore();
    }

    private void SaveHiscore()
    {
        int hiscore = LoadHiscore();

        if (score > hiscore) {
            PlayerPrefs.SetInt(highScoreName, score);
        }
    }

    private int LoadHiscore()
    {
        return PlayerPrefs.GetInt(highScoreName, 0);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
