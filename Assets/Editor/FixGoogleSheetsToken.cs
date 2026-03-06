using UnityEngine;
using UnityEditor;
using System.IO;

public class FixGoogleSheetsToken
{
    [MenuItem("Tools/Borrar Token de Google")]
    public static void BorrarToken()
    {
        // Busca en la carpeta Roaming
        string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string googleAuthPath = Path.Combine(appData, "Google.Apis.Auth");

        bool borrado = false;

        if (Directory.Exists(googleAuthPath))
        {
            Directory.Delete(googleAuthPath, true);
            Debug.Log("✅ ¡Token borrado con éxito de la carpeta Roaming!");
            borrado = true;
        }

        // Busca en la carpeta Local por si acaso
        string localData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        string localAuthPath = Path.Combine(localData, "Google.Apis.Auth");

        if (Directory.Exists(localAuthPath))
        {
            Directory.Delete(localAuthPath, true);
            Debug.Log("✅ ¡Token borrado con éxito de la carpeta Local!");
            borrado = true;
        }

        if (!borrado)
        {
            Debug.LogWarning("⚠️ No se encontró ningún token atascado. ¡Prueba a autorizar en Unity de nuevo!");
        }
    }
}