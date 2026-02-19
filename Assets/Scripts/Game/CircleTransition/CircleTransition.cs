using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Collections.Shaders.CircleTransition
{
    public class CircleTransition : MonoBehaviour
    {
        public Transform player;

        private Canvas _canvas;
        private Image _blackScreen;

        private Vector2 _playerCanvasPos;
    
        private static readonly int RADIUS = Shader.PropertyToID("_Radius");
        private static readonly int CENTER_X = Shader.PropertyToID("_CenterX");
        private static readonly int CENTER_Y = Shader.PropertyToID("_CenterY");

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _blackScreen = GetComponentInChildren<Image>();
        }

        private void Start()
        {
            DrawBlackScreen();
            // Forzamos a que el radio sea 1 (abierto) al empezar
            _blackScreen.material.SetFloat(RADIUS, 1f);
        }

        private void Update()
        {
            // We control by keyboard for fast prototype

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OpenBlackScreen();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                CloseBlackScreen();
            }
        }

        public void OpenBlackScreen()
        {
            DrawBlackScreen();
            StartCoroutine(Transition(2, 0, 1));
        }

        public void CloseBlackScreen()
        {
            DrawBlackScreen();
            StartCoroutine(Transition(2, 1, 0));
        }

        private void DrawBlackScreen()
        {
            // Ya no necesitamos calcular la posición del jugador
            var mat = _blackScreen.material;

            // 0.5f es el centro exacto en la mayoría de Shaders de transición (coordenadas UV)
            mat.SetFloat(CENTER_X, 0.5f);
            mat.SetFloat(CENTER_Y, 0.5f);
        }

        private IEnumerator Transition(float duration, float beginRadius, float endRadius)
        {
            var mat = _blackScreen.material;
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime; // Independiente de la pausa
                float t = Mathf.Clamp01(time / duration);

                // Aplicamos un suavizado opcional con SmoothStep para que se vea más profesional
                float smoothT = t * t * (3f - 2f * t);
                float radius = Mathf.Lerp(beginRadius, endRadius, smoothT);

                mat.SetFloat(RADIUS, radius);
                yield return null;
            }

            mat.SetFloat(RADIUS, endRadius);
        }
    }
}