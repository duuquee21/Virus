using UnityEngine;
using System;

public class GestorDeFragmentos : MonoBehaviour
{
    // Señal NO estática para que el planeta escuche solo a sus propios fragmentos
    public event Action OnFragmentosAgotados;
    private bool señalEnviada = false;

    void Update()
    {
        if (señalEnviada) return;

        // Si ya no quedan hijos (fragmentos)
        if (transform.childCount == 0)
        {
            señalEnviada = true;
            OnFragmentosAgotados?.Invoke();
            Destroy(gameObject, 0.1f);
        }
    }
}