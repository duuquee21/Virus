using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Texturas del Cursor")]
    [SerializeField] private Texture2D cursorNormal;
    [SerializeField] private Texture2D cursorClick;

    [Header("Configuraci�n")]
    [SerializeField] private Vector2 hotspot = Vector2.zero; // (0,0) es la esquina superior izquierda

    void Start()
    {
        // Al empezar el juego, ponemos el cursor por defecto
        CambiarACursorNormal();
    }

    void Update()
    {
        // Detectar si se acaba de presionar el bot�n IZQUIERDO del rat�n (0)
        if (Input.GetMouseButtonDown(0))
        {
            SetearCursor(cursorClick);
        }

        // Detectar si se acaba de soltar el bot�n IZQUIERDO del rat�n (0)
        if (Input.GetMouseButtonUp(0))
        {
            SetearCursor(cursorNormal);
        }
        // Nunca forzar Cursor.visible aquí, solo cambiar la textura
    }

    // Funciones para mayor claridad y poder llamarlas desde fuera si quieres
    public void CambiarACursorNormal()
    {
        SetearCursor(cursorNormal);
    }

    private void SetearCursor(Texture2D textura)
    {
        // Solo cambiamos si hay una textura asignada
        if (textura != null)
        {
            // CursorMode.Auto usa cursores de hardware para mejor rendimiento
            Cursor.SetCursor(textura, hotspot, CursorMode.Auto);
        }
        else
        {
            Debug.LogWarning("Falta asignar una textura de cursor en el script de " + gameObject.name);
        }
    }
}