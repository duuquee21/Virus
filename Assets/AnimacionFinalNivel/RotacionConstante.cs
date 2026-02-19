using UnityEngine;
using UnityEngine.UI; // Necesario para componentes de Interfaz (Image)

public class ObjetoInteractivoCompleto : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    public Vector3 velocidadRotacionMax = new Vector3(0, 0, 100);
    [Range(0.1f, 10f)] public float suavizadoFrenadoRotacion = 2f;

    [Header("Configuración de Posición")]
    public Vector2 offsetPosicion = new Vector2(50f, 50f);
    [Range(0.1f, 20f)] public float suavizadoMovimiento = 5f;

    [Header("Configuración Visual")]
    public Sprite spriteNormal;
    public Sprite spriteAlPasarRaton;

    // Referencias internas
    private Vector3 posicionOriginal;
    private Vector3 posicionObjetivo;
    private Vector3 velocidadActualRotacion;
    private bool estaPausado = false;

    // Soporte para SpriteRenderer (2D) o Image (UI)
    private SpriteRenderer sRenderer;
    private Image uiImage;

    void Awake()
    {
        // Intentamos obtener cualquiera de los dos componentes
        sRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
    }

    void Start()
    {
        posicionOriginal = transform.localPosition;
        posicionObjetivo = posicionOriginal;
        velocidadActualRotacion = velocidadRotacionMax;

        // Establecer el sprite inicial
        CambiarSprite(spriteNormal);
    }

    void Update()
    {
        // 1. Rotación con frenado suave
        Vector3 objetivoVel = estaPausado ? Vector3.zero : velocidadRotacionMax;
        velocidadActualRotacion = Vector3.Lerp(velocidadActualRotacion, objetivoVel, Time.deltaTime * suavizadoFrenadoRotacion);
        transform.Rotate(velocidadActualRotacion * Time.deltaTime);

        // 2. Movimiento con suavizado
        transform.localPosition = Vector3.Lerp(transform.localPosition, posicionObjetivo, Time.deltaTime * suavizadoMovimiento);
    }

    public void ActivarEfecto()
    {
        estaPausado = true;
        posicionObjetivo = posicionOriginal + (Vector3)offsetPosicion;
        CambiarSprite(spriteAlPasarRaton);
    }

    public void DesactivarEfecto()
    {
        estaPausado = false;
        posicionObjetivo = posicionOriginal;
        CambiarSprite(spriteNormal);
    }

    private void CambiarSprite(Sprite nuevoSprite)
    {
        if (nuevoSprite == null) return;

        if (sRenderer != null) sRenderer.sprite = nuevoSprite;
        else if (uiImage != null) uiImage.sprite = nuevoSprite;
    }
}