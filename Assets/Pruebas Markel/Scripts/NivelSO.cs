using UnityEngine;

[CreateAssetMenu(fileName = "NuevoNivel", menuName = "Selector/Nivel")]
public class NivelSO : ScriptableObject
{
    public string nombreNivel;
    public Sprite imagenNivel;
    public string nombreEscena; // Para saber qué escena cargar al dar a "Jugar"
}