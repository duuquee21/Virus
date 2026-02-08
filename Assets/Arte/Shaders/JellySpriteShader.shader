Shader "Custom/JellyMultiImpact" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True" }
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
            // Arrays para manejar hasta 4 impactos simultáneos
            // x, y = posición local | z = deformación | w = radio/suavizado
            float4 _Impacts[4]; 

           v2f vert(appdata_t v) {
    v2f o;
    float2 originalPos = v.vertex.xy;
    float2 totalDeform = float2(0,0);

    for(int i = 0; i < 4; i++) {
        float2 impactPos = _Impacts[i].xy;
        float strength = _Impacts[i].z; // Positivo = afuera, Negativo = adentro
        float radius = 1.2; 

        float distToImpact = distance(originalPos, impactPos);
        
        if (distToImpact < radius) {
            // 1. Calculamos la atenuación suave (Smoothstep)
            float falloff = 1.0 - smoothstep(0.0, radius, distToImpact);
            
            // 2. Determinamos la dirección:
            // Usamos la dirección desde el centro del sprite para mantener la forma esférica
            float2 dir = normalize(originalPos);

            // 3. Aplicamos la deformación
            // El uso de (falloff * falloff) suaviza la base de la onda
            totalDeform += dir * (strength * (falloff * falloff));
        }
    }

    // 4. Límite de seguridad para evitar que el sprite se "rompa" o se cruce sobre sí mismo
    float maxMovement = 0.35;
    totalDeform = clamp(totalDeform, -maxMovement, maxMovement);

    v.vertex.xy += totalDeform;
    
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