using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.Events;

public class Countdown : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDuration = 300f; // 5 minutes in seconds
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private bool startOnAwake = true;

    [Header("Warning Settings")]
    [SerializeField] private float warningThreshold = 30f; // When to start warning
    [SerializeField] private Color warningColor = Color.red;
    private Color originalColor;

    [Header("Events")]
    public UnityEvent onTimerComplete;
    public UnityEvent onTimerWarning;

    private float currentTime;
    private bool isRunning;
    private bool hasTriggeredWarning;

    private void Start()
    {
        if (timerText != null)
        {
            originalColor = timerText.color;
        }

        if (startOnAwake)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            TimerComplete();
        }
        else
        {
            UpdateDisplay();
            CheckWarning();
        }
    }

    public void StartTimer()
    {
        currentTime = gameDuration;
        isRunning = true;
        hasTriggeredWarning = false;
        UpdateDisplay();
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public void ResetTimer()
    {
        currentTime = gameDuration;
        isRunning = false;
        hasTriggeredWarning = false;
        if (timerText != null)
        {
            timerText.color = originalColor;
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText != null)
        {
            TimeSpan time = TimeSpan.FromSeconds(currentTime);
            timerText.text = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        }
    }

    private void CheckWarning()
    {
        if (!hasTriggeredWarning && currentTime <= warningThreshold)
        {
            hasTriggeredWarning = true;
            if (timerText != null)
            {
                timerText.color = warningColor;
            }
            onTimerWarning?.Invoke();
        }
    }

    private void TimerComplete()
    {
        currentTime = 0;
        isRunning = false;
        UpdateDisplay();
        onTimerComplete?.Invoke();
    }

    // Getter for current time
    public float GetCurrentTime()
    {
        return currentTime;
    }

    // Getter for timer state
    public bool IsRunning()
    {
        return isRunning;
    }
}
