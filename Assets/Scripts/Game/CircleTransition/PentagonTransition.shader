Shader "Custom/PentagonTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Size", Range(0.0, 1.5)) = 0
        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(0.0, 1.0)) = 0.5
        _Aspect ("Aspect Ratio", Float) = 1.77
        _Rotation ("Rotation", Range(0, 6.28)) = 0 // En radianes
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float _CenterX;
            float _CenterY;
            float _Aspect;
            float _Rotation;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Función para generar un polígono regular (Pentágono si N=5)
            float sdPentagon(float2 p, float r)
            {
                // Constantes para un pentágono regular
                const float3 k = float3(0.809016994, 0.587785252, 0.726542528);
                p.x = abs(p.x);
                p -= 2.0 * min(dot(float2(-k.x, k.y), p), 0.0) * float2(-k.x, k.y);
                p -= 2.0 * min(dot(float2(k.x, k.y), p), 0.0) * float2(k.x, k.y);
                p -= float2(clamp(p.x, -r * k.z, r * k.z), r);
                return length(p) * sign(p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Centrar y ajustar aspecto
                float2 uv = i.uv - float2(_CenterX, _CenterY);
                uv.x *= _Aspect;

                // 2. Aplicar rotación (opcional, para que la punta mire hacia arriba)
                float s = sin(_Rotation);
                float c = cos(_Rotation);
                uv = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

                // 3. Calcular distancia al pentágono
                float d = sdPentagon(uv, _Radius);

                // 4. Crear la máscara (el interior es transparente)
                float pentagonMask = smoothstep(0.0, 0.01, d);

                return fixed4(_Color.rgb, pentagonMask);
            }
            ENDCG
        }
    }
}