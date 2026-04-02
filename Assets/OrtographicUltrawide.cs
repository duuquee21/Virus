using UnityEngine;

[ExecuteInEditMode]
public class OrthographicUltrawide : MonoBehaviour
{
    public float horizontalSize = 25f; // El ancho que quieres que se vea siempre
    private Camera cam;

    void Start() => cam = GetComponent<Camera>();

    void Update()
    {
        // Ajusta el Size ortográfico basándose en el aspect ratio
        // Size = Ancho_Deseado / Aspect_Ratio / 2
        float currentAspect = (float)Screen.width / Screen.height;
        cam.orthographicSize = horizontalSize / currentAspect / 2f;
    }
}