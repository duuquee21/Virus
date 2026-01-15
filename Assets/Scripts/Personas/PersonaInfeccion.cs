using UnityEngine;
using UnityEngine.UI;

public class PersonaInfeccion : MonoBehaviour
{
    public float timeToInfect = 2.0f;
    private float currentInfectionTime = 0f;
    private bool isVirusHovering = false; // ESTA ENCIMA DEL LA PERSONA
    public bool alreadyInfected = false;

    public SpriteRenderer spritePersona;
    public Color infectedColor = Color.red;
    public GameObject infectionBarCanvas;
    public Image fillingBarImage;


    private Transform virusTransform; // Variable nueva para localizar al virus
    void Start()
    {
        infectionBarCanvas.SetActive(false);
        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        // BUSCAMOS AL VIRUS AL NACER para poder vigilarlo
        GameObject virusObj = GameObject.FindGameObjectWithTag("Virus");
        if (virusObj != null)
        {
            virusTransform = virusObj.transform;
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (alreadyInfected) return;

        // --- SOLUCI�N DE SEGURIDAD ---
        // Calculamos la distancia real entre esta persona y el virus
        float distanceToVirus = 100f; // Valor alto por defecto
        if (virusTransform != null)
        {
            distanceToVirus = Vector2.Distance(transform.position, virusTransform.position);
        }

        // Solo permitimos infectar si el bool est� activo Y ADEM�S est� cerca f�sicamente (ej. menos de 1.5 metros)
        if (isVirusHovering && distanceToVirus < 3.0f)
        {
            currentInfectionTime += Time.deltaTime;

            // L�gica visual de la barra
            float progress = currentInfectionTime / timeToInfect;
            fillingBarImage.transform.localScale = new Vector3(progress, 1, 1);

            if (currentInfectionTime >= timeToInfect)
            {
                BecomeInfected();
            }
        }
        else
        {
            // Si el virus se alejareseteamos

            

            
            if (currentInfectionTime > 0) currentInfectionTime -= Time.deltaTime;
            {
                currentInfectionTime -= Time.deltaTime * 2; // Baja 2 veces m�s r�pido que sube
            }

            // Actualizar barra bajando
            float progress = currentInfectionTime / timeToInfect;
            fillingBarImage.transform.localScale = new Vector3(progress, 1, 1);

            if (currentInfectionTime <= 0)
            {
                currentInfectionTime = 0;
                infectionBarCanvas.SetActive(false);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
       if(!alreadyInfected && other.CompareTag("Virus"))
        {
            isVirusHovering = true;
            infectionBarCanvas.SetActive(true);
        }
    }

    void BecomeInfected()
    {
        alreadyInfected = true;
        infectionBarCanvas.SetActive(false);
        spritePersona.color = infectedColor;

        // CAMBIO: Avisamos al LevelManager
        if (LevelManager.instance != null)
        {
            LevelManager.instance.RegisterInfection();
        }

    }
}
