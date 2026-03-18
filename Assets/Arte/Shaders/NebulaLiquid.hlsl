// El sufijo _float es obligatorio para que Shader Graph lo reconozca
void NebulaLiquid_float(float2 UV, float Time, float Speed, float Scale, float Distortion, out float2 OutUV)
{
    float2 p = UV * Scale;
    float t = Time * Speed;

    // Motor de movimiento de fluido (3 iteraciones para suavidad)
    float2 i = p;
    for (int n = 0; n < 3; n++)
    {
        i = p + float2(cos(t + i.y * Distortion + (float) n), sin(t + i.x * Distortion + (float) n));
        p += i * 0.5;
    }

    // Devolvemos las UVs deformadas, normalizadas de vuelta a un rango útil
    OutUV = p / Scale;
}