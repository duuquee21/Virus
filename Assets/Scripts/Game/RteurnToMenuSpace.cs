using UnityEngine;

public class ReturnToMenuOnSpace : MonoBehaviour
{
    void Update()
    {
        // Solo actúa si el objeto que contiene este script está activo en la jerarquía
        // y si se presiona la tecla Espacio.
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteReturn();
        }
    }

    private void ExecuteReturn()
    {
        // Verificamos que el LevelManager exista para evitar errores de referencia nula
        if (LevelManager.instance != null)
        {
            Debug.Log("Espacio pulsado: Regresando al menú desde " + gameObject.name);
            LevelManager.instance.ReturnToMenu();
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de LevelManager en la escena.");
        }
    }
}