Shader "Custom/JellyMovement" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Stiffness ("Rigidez", Range(1, 20)) = 5.0
        _DeformScale ("Escala de Deformacion", Range(0, 1)) = 0.2
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float2 _VelocityDir; // Dirección e intensidad del movimiento
            float _Stiffness;
            float _DeformScale;

            v2f vert(appdata_t v) {
                v2f o;
                
                // Calculamos cuánto se debe mover este vértice
                // Usamos la posición del vértice para que los bordes se deformen más que el centro
                float deformWeight = length(v.vertex.xy) * _DeformScale;
                
                // La deformación es opuesta a la velocidad (inercia)
                // Multiplicamos por el signo de la posición para que se "estire"
                float2 offset = _VelocityDir * deformWeight;
                
                v.vertex.xy -= offset;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDCG
        }
    }
}