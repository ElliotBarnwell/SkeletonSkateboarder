using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        UpdateDisplay(0);
    }

    void OnEnable()
    {
        GameManager.OnScoreChanged += UpdateDisplay;
    }

    void OnDisable()
    {
        GameManager.OnScoreChanged -= UpdateDisplay;
    }

    void UpdateDisplay(int score)
    {
        label.text = $"Score: {score}";
    }
}
