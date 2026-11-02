using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPlaying { get; private set; }
    public static bool IsPaused { get; private set; }
    public static float Speed { get; private set; }

    [SerializeField] float startSpeed = 11f;
    [SerializeField] float maxSpeed = 26f;
    [SerializeField] float acceleration = 0.12f;
    [SerializeField] PlayerMovement player;
    [SerializeField] TMP_Text countdownText;
    [SerializeField] GameObject pausePanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text resultText;
    [SerializeField] TMP_Text bestText;
    [SerializeField] TMP_Text newBestText;
    [SerializeField] TMP_Text soundButtonText;
    [SerializeField] Material particleMaterial;

    bool over;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        IsPlaying = false;
        IsPaused = false;
        Speed = 0f;
        over = false;
        Physics.gravity = new Vector3(0f, -30f, 0f);
        Fx.ParticleMaterial = particleMaterial;
        MasterLevelInfo.ResetAll();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    void Start()
    {
        RefreshSoundLabel();
        StartCoroutine(Countdown());
    }

    void Update()
    {
        if (over) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (IsPaused) Resume();
            else if (IsPlaying) Pause();
        }
        if (Input.GetKeyDown(KeyCode.M)) ToggleMute();
        if (IsPlaying && !IsPaused) Speed = Mathf.Min(maxSpeed, Speed + acceleration * Time.deltaTime);
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(0.8f);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                AudioManager.PlayClick();
                yield return new WaitForSeconds(0.8f);
            }
            countdownText.text = "GO!";
        }
        Speed = startSpeed;
        IsPlaying = true;
        yield return new WaitForSeconds(0.7f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void Pause()
    {
        if (over || !IsPlaying) return;
        IsPaused = true;
        IsPlaying = false;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        AudioManager.PlayClick();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        IsPlaying = true;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        AudioManager.PlayClick();
    }

    public void ToggleMute()
    {
        AudioManager.ToggleMute();
        RefreshSoundLabel();
    }

    void RefreshSoundLabel()
    {
        if (soundButtonText != null) soundButtonText.text = AudioManager.Muted ? "SOUND: OFF" : "SOUND: ON";
    }

    public void GameOver()
    {
        if (over) return;
        over = true;
        IsPlaying = false;
        Speed = 0f;
        if (player != null) player.Die();
        AudioManager.PlayCrash();
        CameraFollow.Shake(0.6f);

        int score = MasterLevelInfo.Score;
        int bestScore = PlayerPrefs.GetInt("BESTSCORE", 0);
        bool newBest = score > bestScore;
        if (newBest) PlayerPrefs.SetInt("BESTSCORE", score);
        PlayerPrefs.SetInt("COINSAVE", PlayerPrefs.GetInt("COINSAVE", 0) + MasterLevelInfo.coinCount);
        PlayerPrefs.SetInt("GEMSAVE", PlayerPrefs.GetInt("GEMSAVE", 0) + MasterLevelInfo.gemCount);
        PlayerPrefs.SetInt("BESTDISTANCE", Mathf.Max(PlayerPrefs.GetInt("BESTDISTANCE", 0), MasterLevelInfo.distanceRun));
        PlayerPrefs.Save();

        if (resultText != null)
            resultText.text = "Distance   " + MasterLevelInfo.distanceRun + " m\nCoins   " + MasterLevelInfo.coinCount + "\nGems   " + MasterLevelInfo.gemCount + "\nScore   " + score.ToString("N0");
        if (bestText != null) bestText.text = "BEST  " + Mathf.Max(score, bestScore).ToString("N0");
        if (newBestText != null) newBestText.gameObject.SetActive(newBest);
        StartCoroutine(ShowGameOver());
    }

    IEnumerator ShowGameOver()
    {
        yield return new WaitForSeconds(1.3f);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        AudioManager.PlayClick();
        ScreenFader.Go(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        AudioManager.PlayClick();
        ScreenFader.Go(0);
    }
}
