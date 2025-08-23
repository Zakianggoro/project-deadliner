using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGame : MonoBehaviour
{
    [Header("Rounds / Sequence")]
    [SerializeField] private int totalRounds = 12;
    [SerializeField] private int startingLength = 5;
    [SerializeField] private int maxLength = 8;

    [Header("Timing")]
    [SerializeField] private float roundTime = 6f;          // seconds per round
    [SerializeField, Range(0f, 1f)] private float greenStart = 0.75f;
    [SerializeField, Range(0f, 1f)] private float greenEnd = 0.88f;

    [Header("Stagger")]
    [SerializeField] private float staggerDuration = 1.25f;

    [Header("Scoring")]
    [SerializeField] private int pointsPerArrow = 25;
    [SerializeField] private int sequenceBonus = 200;
    [SerializeField] private float streakStep = 0.25f; 

    [Header("UI References")]
    [SerializeField] private Image timerFill;                
    [SerializeField] private RectTransform sequenceContainer; 
    [SerializeField] private GameObject arrowItemPrefab;      
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("UI Colors")]
    [SerializeField] private Color arrowIdleColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color arrowPassedColor = new Color(0.6f, 1f, 0.7f, 1f);
    [SerializeField] private Color arrowWrongColor = new Color(1f, 0.5f, 0.5f, 1f);

    private enum State { Idle, Inputting, AwaitingSpace, Staggered, RoundWon, RoundLost, GameOver }
    private State state = State.Idle;

    private readonly KeyCode[] keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
    private List<KeyCode> sequence = new List<KeyCode>();
    private List<Image> sequenceIcons = new List<Image>();

    private int currentRound = 0;
    private int successCount = 0;
    private int currentIndex = 0; 
    private int currentLength = 0;

    private float timer = 0f;
    private bool spacePressedThisWindow = false;

    private bool isStaggered = false;
    private float staggerTimer = 0f;

    private int score = 0;
    private float streakMultiplier = 1f; 

    void Start()
    {
        BeginNextRound();
    }

    void Update()
    {
        if (state == State.GameOver) return;

        UpdateTimer();
        UpdateStagger();

        switch (state)
        {
            case State.Inputting:
                HandleArrowInput();
                if (currentIndex >= sequence.Count)
                {
                    state = State.AwaitingSpace;
                    SetStatus("Hit SPACE in the green window!");
                }
                break;

            case State.AwaitingSpace:
                HandleSpacebar();
                break;

            case State.Staggered:
                break;
        }
    }

    // --------------------- Round / Sequence ---------------------

    private void BeginNextRound()
    {
        if (currentRound >= totalRounds)
        {
            state = State.GameOver;
            SetStatus($"Game Over! Final Score: {score}");
            return;
        }

        currentRound++;
        currentLength = Mathf.Min(maxLength, startingLength + successCount);

        BuildSequence(currentLength);
        BuildSequenceUI();

        timer = 0f;
        spacePressedThisWindow = false;
        currentIndex = 0;
        isStaggered = false;
        staggerTimer = 0f;
        state = State.Inputting;

        UpdateRoundText();
        UpdateScoreText();
        SetStatus("Enter the arrows!");
        UpdateTimerVisual(0f);
    }

    private void BuildSequence(int length)
    {
        sequence.Clear();
        for (int i = 0; i < length; i++)
        {
            sequence.Add(keys[Random.Range(0, keys.Length)]);
        }
    }

    private void BuildSequenceUI()
    {
        // clear old
        foreach (Transform child in sequenceContainer) Destroy(child.gameObject);
        sequenceIcons.Clear();

        for (int i = 0; i < sequence.Count; i++)
        {
            var go = Instantiate(arrowItemPrefab, sequenceContainer);
            Image img = go.GetComponent<Image>();
            
            img.color = arrowIdleColor;
            go.transform.rotation = RotationFor(sequence[i]);
            
            sequenceIcons.Add(go.GetComponent<Image>());
        }
    }

    private Quaternion RotationFor(KeyCode k)
    {
        if (k == KeyCode.UpArrow) return Quaternion.Euler(0, 0, 0);
        if (k == KeyCode.RightArrow) return Quaternion.Euler(0, 0, -90);
        if (k == KeyCode.DownArrow) return Quaternion.Euler(0, 0, 180);
        return Quaternion.Euler(0, 0, 90);
    }

    private string KeyToGlyph(KeyCode k)
    {
        if (k == KeyCode.UpArrow) return "Up";
        if (k == KeyCode.DownArrow) return "Down";
        if (k == KeyCode.LeftArrow) return "Left";
        return "Right";
    }


    // --------------------- Timer / Green Window ---------------------

    private void UpdateTimer()
    {
        if (state == State.Idle || state == State.GameOver) return;

        timer += Time.deltaTime;
        float p = Mathf.Clamp01(timer / roundTime);
        UpdateTimerVisual(p);

        if (p >= 1f)
        {
            LoseRound();
        }
    }

    private void UpdateTimerVisual(float progress01)
    {
        if (timerFill)
        {
            timerFill.fillAmount = progress01;

            Color baseCol = Color.magenta;
            Color inWin = Color.green;
            float t = InGreenWindow(progress01) ? 1f : Mathf.InverseLerp(greenStart - 0.05f, greenStart, progress01);
            timerFill.color = Color.Lerp(baseCol, inWin, t);
        }
    }

    private bool InGreenWindow(float progress01)
    {
        return progress01 >= greenStart && progress01 <= greenEnd;
    }

    // --------------------- Input ---------------------

    private void HandleArrowInput()
    {
        if (isStaggered) return;
        if (Input.GetKeyDown(KeyCode.UpArrow)) Evaluate(KeyCode.UpArrow);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) Evaluate(KeyCode.DownArrow);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Evaluate(KeyCode.LeftArrow);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) Evaluate(KeyCode.RightArrow);
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerStagger("Too early! Finish the arrows first.");
        }
    }

    private void Evaluate(KeyCode pressed)
    {
        if (currentIndex >= sequence.Count) return;

        if (pressed == sequence[currentIndex])
        {
            if (currentIndex < sequenceIcons.Count && sequenceIcons[currentIndex] != null)
                sequenceIcons[currentIndex].color = arrowPassedColor;

            currentIndex++;
            AddScore(pointsPerArrow);
            SetStatus($"Good! {currentIndex}/{sequence.Count}");
            UpdateScoreText();
        }
        else
        {
            if (currentIndex < sequenceIcons.Count && sequenceIcons[currentIndex] != null)
                sequenceIcons[currentIndex].color = arrowWrongColor;

            TriggerStagger("Wrong arrow!");
        }
    }

    private void HandleSpacebar()
    {
        if (isStaggered) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float p = Mathf.Clamp01(timer / roundTime);
            if (InGreenWindow(p))
            {
                WinRound();
            }
            else
            {
                TriggerStagger("Bad timing!");
            }
        }

        // Defensive: arrows pressed after finishing input do nothing (you can also block them)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TriggerStagger("Finish with SPACE!");
        }
    }

    // --------------------- Stagger ---------------------

    private void TriggerStagger(string message)
    {
        isStaggered = true;
        staggerTimer = staggerDuration;
        state = State.Staggered;
        SetStatus($"Staggered! {message}");
    }

    private void UpdateStagger()
    {
        if (!isStaggered) return;

        staggerTimer -= Time.deltaTime;
        if (staggerTimer <= 0f)
        {
            isStaggered = false;

            // If we had finished the arrows we still need to lock with Space;
            // otherwise go back to Inputting.
            if (currentIndex >= sequence.Count)
            {
                state = State.AwaitingSpace;
                SetStatus("Recovered. Hit SPACE in the green window!");
            }
            else
            {
                state = State.Inputting;
                SetStatus("Recovered. Continue the arrows!");
            }
        }
    }

    // --------------------- Scoring / Round Results ---------------------

    private void WinRound()
    {
        state = State.RoundWon;
        successCount++;
        AddScore(Mathf.RoundToInt(sequenceBonus * streakMultiplier));
        streakMultiplier += streakStep;

        SetStatus($"Round {currentRound} Cleared! +{Mathf.RoundToInt(sequenceBonus * streakMultiplier)}");
        UpdateScoreText();

        Invoke(nameof(BeginNextRound), 0.8f);
    }

    private void LoseRound()
    {
        state = State.RoundLost;
        streakMultiplier = 1f;

        // Optional partial credit based on progress
        float progressPct = (sequence.Count == 0) ? 0f : (float)currentIndex / sequence.Count;
        int consolation = Mathf.RoundToInt(sequenceBonus * 0.25f * progressPct);
        if (consolation > 0) AddScore(consolation);

        SetStatus($"Failed Round {currentRound}. (Progress {currentIndex}/{sequence.Count})");
        UpdateScoreText();

        Invoke(nameof(BeginNextRound), 1.0f);
    }

    private void AddScore(int amount)
    {
        score += Mathf.Max(0, amount);
    }

    // --------------------- UI ---------------------

    private void UpdateRoundText()
    {
        if (roundText) roundText.text = $"Round {currentRound}/{totalRounds}  |  Len {currentLength}";
    }

    private void UpdateScoreText()
    {
        if (scoreText) scoreText.text = $"Score: {score}   x{streakMultiplier:0.00}";
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
