using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverScreen;   // Drag a UI panel here

    private bool gameOver = false;

    void Awake()
    {
        Instance = this;
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