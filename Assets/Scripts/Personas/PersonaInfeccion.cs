using UnityEngine;
using UnityEngine.UI;

public class PersonaInfeccion : MonoBehaviour
{
    // ⏱ Tiempo global mejorable por upgrades
    public static float globalInfectTime = 2f;

    public SpriteRenderer spritePersona;
    public Color infectedColor = Color.red;

    public GameObject infectionBarCanvas;
    public Image fillingBarImage;

    private float currentInfectionTime;
    private bool isInsideZone = false;
    public bool alreadyInfected = false;

    [Header("Shiny Settings")]
    public bool isShiny = false;
    public Color shinyColor = Color.yellow;
    // Ya no usaremos este 'shinyReward' fijo, usaremos el de Guardado
    public int shinyReward = 1;

    void Start()
    {
        if (spritePersona == null)
            spritePersona = GetComponent<SpriteRenderer>();

        infectionBarCanvas.SetActive(false);
    }

    void Update()
    {
        if (alreadyInfected) return;

        if (isInsideZone)
            currentInfectionTime += Time.deltaTime;
        else
            currentInfectionTime -= Time.deltaTime * 2f;

        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, globalInfectTime);

        float progress = currentInfectionTime / globalInfectTime;
        fillingBarImage.transform.localScale = new Vector3(progress, 1f, 1f);

        if (currentInfectionTime > 0)
            infectionBarCanvas.SetActive(true);

        if (currentInfectionTime >= globalInfectTime)
            BecomeInfected();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alreadyInfected && other.CompareTag("InfectionZone"))
            isInsideZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("InfectionZone"))
            isInsideZone = false;
    }

    void BecomeInfected()
    {
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);
        spritePersona.color = infectedColor;

        if (LevelManager.instance != null)
        {
            LevelManager.instance.RegisterInfection();
        }
        
        if (InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayEffect(transform.position);
        }
        

        if (isShiny)
        {
            if (Guardado.instance != null)
            {
                // CAMBIO CLAVE: Usamos la función que calcula (Suma * Multiplicador Base)
                // Esto permite que si tienes +2 de suma y compras x7, recibas 14, 
                // pero si luego compras x10, pases a recibir 20 (no 140).
                int cantidadFinal = Guardado.instance.GetFinalShinyValue();
                Guardado.instance.AddShinyDNA(cantidadFinal);

                Debug.Log("¡Shiny Infectado! Valor Base (" + Guardado.instance.shinyValueSum +
                          ") x Multiplicador (" + Guardado.instance.shinyMultiplier + ") = " + cantidadFinal);
            }

            if (LevelManager.instance != null)
                LevelManager.instance.isShinyCollectedInRun = true;
        }
    }

    public void MakeShiny()
    {
        isShiny = true;
        spritePersona.color = shinyColor;
        transform.localScale = transform.localScale * 1.2f;
    }
}