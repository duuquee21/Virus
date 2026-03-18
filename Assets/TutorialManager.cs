using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("UI")]
    public GameObject tutorialPanel;
    public CanvasGroup tutorialCanvasGroup;
    public TextMeshProUGUI tutorialText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip typingClip;
    public int soundEveryNLetters = 2;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;

    [Header("Tutorial")]
    public float moveDistanceRequired = 1.5f;
    public float hideDelay = 2f;

    [Header("Typewriter")]
    public float letterDelay = 0.04f;

    [Header("Fade")]
    public float fadeDuration = 0.35f;

    private Vector3 startPlayerPos;
    private Transform playerTransform;

    private int tutorialStep = 0;
    private bool tutorialFinished = false;

    private Coroutine typingCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        instance = this;

        // Aseguramos que el panel esté cerrado por defecto, para evitar que quede vacío
        // cuando el juego se reinicia/continúa y no se inicia el tutorial.
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }
    }

    public void StartTutorial(Transform player)
    {
        playerTransform = player;
        startPlayerPos = player.position;
        tutorialStep = 0;
        tutorialFinished = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }

        StartFade(1f);
        ShowMessage("Mu\u00E9vete.");
    }

    private void Update()
    {
        if (tutorialFinished || playerTransform == null) return;

        switch (tutorialStep)
        {
            case 0:
                CheckMovementStep();
                break;

            case 1:
                break;
        }
    }

    private void CheckMovementStep()
    {
        float distance = Vector3.Distance(playerTransform.position, startPlayerPos);

        if (distance >= moveDistanceRequired)
        {
            tutorialStep = 1;
            ShowMessage("Infecta al primero.");
        }
    }
    public bool HasSeenTutorial()
    {
        return PlayerPrefs.GetInt("TutorialSeen", 0) == 1;
    }

    public void MarkTutorialAsSeen()
    {
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();
    }
    public void OnFirstPhaseAdvance()
    {
        if (tutorialFinished) return;

        tutorialFinished = true;

        // Asegurar que el panel esté visible al mostrar el mensaje final del tutorial.
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = true;
            tutorialCanvasGroup.blocksRaycasts = true;
        }

        ShowMessage("Perfecto. Ya empieza la propagaci\u00F3n.");

        if (LevelManager.instance != null)
        {
            LevelManager.instance.timerStarted = true;
        }

        MarkTutorialAsSeen();

        Invoke(nameof(HideTutorial), hideDelay);
    }
    private void HideTutorial()
    {
        StartFade(0f, true);
    }

    private void ShowMessage(string message)
    {
        if (tutorialText == null) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(message));
    }

    private IEnumerator TypeText(string message)
    {
        tutorialText.text = "";

        int visibleCharCount = 0;

        for (int i = 0; i < message.Length; i++)
        {
            char currentChar = message[i];
            tutorialText.text += currentChar;

            if (!char.IsWhiteSpace(currentChar))
            {
                visibleCharCount++;

                if (typingClip != null && audioSource != null && visibleCharCount % soundEveryNLetters == 0)
                {
                    audioSource.pitch = Random.Range(pitchMin, pitchMax);
                    audioSource.PlayOneShot(typingClip);
                }
            }

            yield return new WaitForSeconds(letterDelay);
        }

        typingCoroutine = null;
    }

    private void StartFade(float targetAlpha, bool disableOnEnd = false)
    {
        if (tutorialCanvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvas(targetAlpha, disableOnEnd));
    }

    private IEnumerator FadeCanvas(float targetAlpha, bool disableOnEnd)
    {
        float startAlpha = tutorialCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            tutorialCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        tutorialCanvasGroup.alpha = targetAlpha;

        if (disableOnEnd && tutorialPanel != null && targetAlpha <= 0f)
        {
            tutorialPanel.SetActive(false);
        }

        fadeCoroutine = null;
    }
}