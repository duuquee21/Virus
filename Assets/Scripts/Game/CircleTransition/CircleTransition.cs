using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Collections.Shaders.ShapeTransition
{
    public enum TransitionShape { Circle = 0, Hexagon = 1, Pentagon = 2 }

    public class ShapeTransition : MonoBehaviour
    {
        public TransitionShape selectedShape; // Aparecerá como un dropdown en Unity
        private Image _blackScreen;

        private static readonly int RADIUS = Shader.PropertyToID("_Radius");
        private static readonly int CENTER_X = Shader.PropertyToID("_CenterX");
        private static readonly int CENTER_Y = Shader.PropertyToID("_CenterY");
        private static readonly int ASPECT = Shader.PropertyToID("_Aspect");
        private static readonly int SHAPE = Shader.PropertyToID("_Shape");

        private void Awake()
        {
            _blackScreen = GetComponentInChildren<Image>();
        }

        private void Start()
        {
            UpdateShaderProperties();
            _blackScreen.material.SetFloat(RADIUS, 1.5f); // Totalmente abierto
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) OpenBlackScreen();
            if (Input.GetKeyDown(KeyCode.Alpha2)) CloseBlackScreen();
        }

        public void OpenBlackScreen()
        {
            UpdateShaderProperties();
            StartCoroutine(Transition(0.6f, 0, 1.5f));
        }

        public void CloseBlackScreen()
        {
            UpdateShaderProperties();
            // Cambiamos el valor final de 0 a -0.1f para asegurar que la forma desaparezca del todo
            StartCoroutine(Transition(0.6f, 1.5f, -0.1f));
        }

        private void UpdateShaderProperties()
        {
            var mat = _blackScreen.material;
            mat.SetFloat(CENTER_X, 0.5f);
            mat.SetFloat(CENTER_Y, 0.5f);
            mat.SetFloat(ASPECT, (float)Screen.width / Screen.height);

            // Enviamos el índice del Enum (0, 1 o 2) al shader
            mat.SetInt(SHAPE, (int)selectedShape);
        }

        private IEnumerator Transition(float duration, float start, float end)
        {
            var mat = _blackScreen.material;
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float smoothT = t * t * (3f - 2f * t);
                mat.SetFloat(RADIUS, Mathf.Lerp(start, end, smoothT));
                yield return null;
            }
            mat.SetFloat(RADIUS, end);
        }

        // Añade esto a tu script de transición (el que está en el namespace Collections.Shaders.CircleTransition)
        public void SetShape(int shapeIndex)
        {
            // 1. Actualizamos la variable interna para que UpdateShaderProperties use el valor correcto
            selectedShape = (TransitionShape)shapeIndex;

            // 2. Aplicamos al material inmediatamente
            var mat = _blackScreen.material;
            mat.SetInt(SHAPE, shapeIndex);
            mat.SetFloat(ASPECT, (float)Screen.width / Screen.height);
        }
    }
}