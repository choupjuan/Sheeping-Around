using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private int initialScore = 0;
    [SerializeField] private TextMeshProUGUI scoreDisplay;
    [SerializeField] private string scoreFormat = "Score: {0}";

    [Header("Category Scores")]
    [SerializeField] private bool trackCategoryScores = false;
    
    private int currentScore;
    private Dictionary<string, int> categoryScores = new Dictionary<string, int>();

    // Events
    public event Action<int> OnScoreChanged;
    public event Action<string, int> OnCategoryScoreChanged;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Optional: make persistent between scenes
        // DontDestroyOnLoad(gameObject);
        
        currentScore = initialScore;
    }

    private void Start()
    {
        UpdateScoreDisplay();
    }

    public void AddPoints(int points)
    {
        if (points <= 0) return;
        
        currentScore += points;
        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(currentScore);
    }

    public void SubtractPoints(int points)
    {
        if (points <= 0) return;
        
        currentScore -= points;
        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(currentScore);
    }
    
    public void AddCategoryPoints(string category, int points)
    {
        if (!trackCategoryScores || points <= 0) return;
        
        // Initialize category if it doesn't exist
        if (!categoryScores.ContainsKey(category))
        {
            categoryScores[category] = 0;
        }
        
        categoryScores[category] += points;
        
        // Also add to total score
        currentScore += points;
        UpdateScoreDisplay();
        
        OnCategoryScoreChanged?.Invoke(category, categoryScores[category]);
        OnScoreChanged?.Invoke(currentScore);
    }
    
    public int GetCategoryScore(string category)
    {
        if (!trackCategoryScores || !categoryScores.ContainsKey(category))
        {
            return 0;
        }
        
        return categoryScores[category];
    }

    private void UpdateScoreDisplay()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.text = string.Format(scoreFormat, currentScore);
        }
    }
    
    public void ResetScore()
    {
        currentScore = initialScore;
        categoryScores.Clear();
        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(currentScore);
    }
    
    // Save and load functionality
    public void SaveScore()
    {
        PlayerPrefs.SetInt("PlayerScore", currentScore);
        PlayerPrefs.Save();
    }
    
    public void LoadScore()
    {
        if (PlayerPrefs.HasKey("PlayerScore"))
        {
            currentScore = PlayerPrefs.GetInt("PlayerScore");
            UpdateScoreDisplay();
            OnScoreChanged?.Invoke(currentScore);
        }
    }
}
