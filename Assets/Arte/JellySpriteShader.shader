Shader "Custom/JellySprite" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ImpactPos ("Impact Position", Vector) = (0,0,0,0)
        _Deform ("Deformation Amount", Float) = 0
    }
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "CanUseSpriteAtlas"="True" 
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float2 texcoord  : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _ImpactPos;
            float _Deform;

           v2f vert(appdata_t v) {
    v2f o;

    // 1. Dirección desde el centro del círculo hacia el vértice actual
    float2 dirFromCenter = normalize(v.vertex.xy);
    
    // 2. Distancia del impacto al vértice
    float distToImpact = distance(v.vertex.xy, _ImpactPos.xy);
    
    // 3. Determinamos si el impacto es "externo" o "interno"
    // Si la distancia del impacto al centro es mayor que la del vértice, viene de fuera.
    float impactDistFromCenter = length(_ImpactPos.xy);
    float vertexDistFromCenter = length(v.vertex.xy);
    
    // Si viene de fuera, el factor es negativo (hacia adentro)
    // Si viene de dentro, el factor es positivo (hacia afuera)
    float directionFactor = (impactDistFromCenter > vertexDistFromCenter) ? -1.0 : 1.0;

    // 4. Suavizado de la zona de influencia
    float radius = 0.8;
    float falloff = saturate(1.0 - (distToImpact / radius));
    float softEffect = pow(falloff, 3.0) * _Deform;

    // 5. Aplicamos el movimiento en el eje que conecta el centro con el vértice
    v.vertex.xy += dirFromCenter * softEffect * directionFactor;

    o.vertex = UnityObjectToClipPos(v.vertex);
    o.texcoord = v.texcoord;
    o.color = v.color;
    return o;
}


            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                return col;
            }
            ENDCG
        }
    }
}