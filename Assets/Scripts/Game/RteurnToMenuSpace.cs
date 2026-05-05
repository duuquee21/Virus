using UnityEngine;

public class ReturnToMenuOnSpace : MonoBehaviour
{
    void Update()
    {
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteReturn();
        }
    }

    private void ExecuteReturn()
    {
        if (LevelManager.instance != null)
        {
            Debug.Log("Espacio pulsado: regresando al menú desde " + gameObject.name);

            LevelManager.instance.BotonPanelFinalSalirMenu();
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de LevelManager en la escena.");
        }
    }
}