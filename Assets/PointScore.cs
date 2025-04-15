using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PointScore : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private int correctAnswerIndex;

    [Header("Visual Feedback")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerCorrectAnswer = 10;
    [SerializeField] private string scoreCategory = "Quiz";
    [SerializeField] private bool useCategoryScoring = false;
    
    private bool isProcessingAnswer = false;

    private void Start()
    {
        // Add click listeners to all buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int buttonIndex = i; // Capture the index for the lambda
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(buttonIndex));
        }
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        if (isProcessingAnswer) return;
        
        // Set processing flag to prevent additional selections
        isProcessingAnswer = true;
        
        if (selectedIndex == correctAnswerIndex)
        {
            // Correct answer
            ChangeButtonColor(selectedIndex, correctColor);
            
            // Update score using ScoreTracker
            if (useCategoryScoring)
            {
                ScoreTracker.Instance.AddCategoryPoints(scoreCategory, pointsPerCorrectAnswer);
            }
            else
            {
                ScoreTracker.Instance.AddPoints(pointsPerCorrectAnswer);
            }
        }
        else
        {
            // Incorrect answer
            ChangeButtonColor(selectedIndex, incorrectColor);
            
            // Show the correct answer
            ChangeButtonColor(correctAnswerIndex, correctColor);
        }
        
        // Disable all buttons
        SetAllButtonsInteractable(false);
        
        // Keep isProcessingAnswer true to prevent further selections
        // until next question is set up
    }
    
    private void ChangeButtonColor(int buttonIndex, Color color)
    {
        if (buttonIndex >= 0 && buttonIndex < answerButtons.Length)
        {
            Image buttonImage = answerButtons[buttonIndex].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color;
            }
        }
    }
    
    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            button.interactable = interactable;
        }
    }
    
    // Call this when moving to the next question
    public void SetupNextQuestion(int newCorrectAnswerIndex)
    {
        correctAnswerIndex = newCorrectAnswerIndex;
        isProcessingAnswer = false;
        
        // Reset all button colors for the new question
        foreach (Button button in answerButtons)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.white;
            }
            
            // Make buttons clickable again
            button.interactable = true;
        }
    }
}
