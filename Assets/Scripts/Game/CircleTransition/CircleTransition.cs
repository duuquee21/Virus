using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Collections.Shaders.ShapeTransition
{
    // 1. EL ENUM DEBE ESTAR AQUÍ (DENTRO DEL NAMESPACE)
    public enum TransitionShape { Circle = 0, Hexagon = 1, Pentagon = 2 }

    public class ShapeTransition : MonoBehaviour
    {
        public TransitionShape selectedShape; // Ahora ya debería reconocerlo

        [Header("Settings")]
        [SerializeField] private float transitionDuration = 0.7f;
        [SerializeField] private float rotationAmount = 3.14f;

        private Image _blackScreen;
        private bool _isOpen = true;
        private Coroutine _currentTransition;

        // IDs de las propiedades del Shader
        private static readonly int RADIUS = Shader.PropertyToID("_Radius");
        private static readonly int ROTATION = Shader.PropertyToID("_Rotation");
        private static readonly int ASPECT = Shader.PropertyToID("_Aspect");
        private static readonly int SHAPE = Shader.PropertyToID("_Shape");

        private void Awake()
        {
            _blackScreen = GetComponentInChildren<Image>();
        }

        private void Start()
        {
            UpdateShaderProperties();
            // Estado inicial: abierto
            _blackScreen.material.SetFloat(RADIUS, 1.5f);
            _blackScreen.material.SetFloat(ROTATION, 0f);
            _isOpen = true;
        }

        private void Update()
        {
            // Control con la tecla P
            if (Input.GetKeyDown(KeyCode.P))
            {
                ToggleTransition();
            }
        }

        public void ToggleTransition()
        {
            if (_isOpen) CloseBlackScreen();
            else OpenBlackScreen();

            _isOpen = !_isOpen;
        }

        public void OpenBlackScreen()
        {
            UpdateShaderProperties();
            if (_currentTransition != null) StopCoroutine(_currentTransition);
            // Va desde cerrado (0) hasta abierto (1.5)
            _currentTransition = StartCoroutine(Transition(0f, 1.5f, rotationAmount, 0f));
        }

        public void CloseBlackScreen()
        {
            UpdateShaderProperties();
            if (_currentTransition != null) StopCoroutine(_currentTransition);
            // Va desde abierto (1.5) hasta cerrado (-0.1)
            _currentTransition = StartCoroutine(Transition(1.5f, -0.1f, 0f, rotationAmount));
        }

        private IEnumerator Transition(float startRad, float endRad, float startRot, float endRot)
        {
            var mat = _blackScreen.material;
            float time = 0f;
            while (time < transitionDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / transitionDuration);

                // Suavizado de movimiento (Ease In Out)
                float smoothT = t * t * (3f - 2f * t);

                mat.SetFloat(RADIUS, Mathf.Lerp(startRad, endRad, smoothT));
                mat.SetFloat(ROTATION, Mathf.Lerp(startRot, endRot, smoothT));

                yield return null;
            }
            mat.SetFloat(RADIUS, endRad);
            mat.SetFloat(ROTATION, endRot);
            _currentTransition = null;
        }

        private void UpdateShaderProperties()
        {
            if (_blackScreen == null) return;
            var mat = _blackScreen.material;
            mat.SetFloat(ASPECT, (float)Screen.width / Screen.height);
            mat.SetInt(SHAPE, (int)selectedShape);
        }

        // Método por si quieres cambiar la forma por código (ej. desde un botón)
        public void SetShape(int shapeIndex)
        {
            selectedShape = (TransitionShape)shapeIndex;
            UpdateShaderProperties();
        }
    }
}