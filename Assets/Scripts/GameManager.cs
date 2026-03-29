using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static event Action<int> OnScoreChanged;

    [Header("UI")]
    public GameObject gameOverScreen;

    private bool gameOver = false;
    private int  score    = 0;

    void Awake()
    {
        Instance = this;
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }

    void Start()
    {
        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;

        Debug.Log("Game Over!");

        // Stop everything
        Time.timeScale = 0f;

        // Show game over screen if assigned
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }

    // Call this from a restart button
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}