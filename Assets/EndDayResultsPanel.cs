using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndDayResultsPanel : MonoBehaviour
{
    public static EndDayResultsPanel instance;

    [Header("UI")]
    public GameObject panel;

    public TextMeshProUGUI infectedText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI zoneText;
    public TextMeshProUGUI finalCoinsText;

    [Header("Shiny UI")]
    public TextMeshProUGUI shinyFoundText;
    public TextMeshProUGUI shinyMultiplierText;
    public TextMeshProUGUI shinyFinalText;

    public Button continueButton;

    [Header("Timing")]
    public float letterSpeed = 0.03f;
    public float stepDelay = 0.5f;

    int finalCoins;
    int finalShinies;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
        continueButton.gameObject.SetActive(false);
    }

    public void ShowResults(
        int infected,
        int baseMultiplier,
        int zoneMultiplier,
        int shiniesCaptured,
        int shinyMultiplier
    )
    {
        panel.SetActive(true);
        continueButton.gameObject.SetActive(false);

        StartCoroutine(AnimateResults(
            infected,
            baseMultiplier,
            zoneMultiplier,
            shiniesCaptured,
            shinyMultiplier
        ));
    }

    IEnumerator AnimateResults(
        int infected,
        int baseMultiplier,
        int zoneMultiplier,
        int shiniesCaptured,
        int shinyMultiplier
    )
    {
        ClearTexts();

        // --- MONEDAS ---
        yield return StartCoroutine(TypeText(infectedText, "Infectados: " + infected));
        yield return new WaitForSeconds(stepDelay);

        yield return StartCoroutine(TypeText(multiplierText, "x " + baseMultiplier));
        yield return new WaitForSeconds(stepDelay);

        yield return StartCoroutine(TypeText(zoneText, "x " + zoneMultiplier));
        yield return new WaitForSeconds(stepDelay);

        finalCoins = infected * baseMultiplier * zoneMultiplier;
        yield return StartCoroutine(TypeText(finalCoinsText, "= " + finalCoins + " monedas"));

        yield return new WaitForSeconds(stepDelay * 1.5f);

        // --- SHINYS ---
        yield return StartCoroutine(TypeText(shinyFoundText, "Shinys capturados: " + shiniesCaptured));
        yield return new WaitForSeconds(stepDelay);

        yield return StartCoroutine(TypeText(shinyMultiplierText, "x " + shinyMultiplier));
        yield return new WaitForSeconds(stepDelay);

        finalShinies = shiniesCaptured * shinyMultiplier;
        yield return StartCoroutine(TypeText(shinyFinalText, "= " + finalShinies + " ADN Shiny"));

        continueButton.gameObject.SetActive(true);
    }

    void ClearTexts()
    {
        infectedText.text = "";
        multiplierText.text = "";
        zoneText.text = "";
        finalCoinsText.text = "";

        shinyFoundText.text = "";
        shinyMultiplierText.text = "";
        shinyFinalText.text = "";
    }

    IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";

        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(letterSpeed);
        }
    }

    // BOTÓN CONTINUAR
    public void Continue()
    {
        panel.SetActive(false);
        LevelManager.instance.OnEndDayResultsFinished(finalCoins, finalShinies);
    }
}
