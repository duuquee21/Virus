void SpinEffect_float(
    float4 Color1,
    float4 Color2,
    float4 Color3,
    float2 ScreenCoords,
    float Time,
    out float4 OutColor
)
{
    // Configuración MODIFICADA: Eliminamos los giros principales.
    const float SPIN_ROTATION = 0.0; // Desactivado para no girar las coordenadas base.
    const float SPIN_SPEED = 0.0; // Desactivado para no hacer girar la animación interna.
    const float2 OFFSET = float2(0.0, 0.0);
    const float CONTRAST = 3.5;
    const float LIGTHING = 0.4;
    const float SPIN_AMOUNT = 0.0; // Desactivado para evitar la forma de remolino.
    const float PIXEL_FILTER = 745.0;
    const float SPIN_EASE = 1.0;
    const bool IS_ROTATE = false;
    const float2 ScreenSize = float2(1, 1);

    // 1. Pixelado y coordenadas base (Mantenemos la pixelación)
    float pixel_size = length(ScreenSize) / PIXEL_FILTER;
    float2 uv = floor(ScreenCoords / pixel_size) * pixel_size;
    
    // Simplificamos las UVs para que no dependan del centro para rotar.
    // Esto es CLAVE: al no centrar y rotar aquí, evitamos el giro de base.
    uv = (uv - 0.5 * ScreenSize) / length(ScreenSize) - OFFSET;
    
    // Eliminamos todo el bloque que calculaba 'new_pixel_angle' y 'speed' para el giro.
    // ... logic for rotating coords removed ...

    // 2. Simulación de líquido con movimiento pero SIN giro.
    // Escalamos las UVs para el detalle de la textura.
    uv *= 30.0;
    
    float2 uv2 = float2(uv.x + uv.y, 0);
    
    // El bucle principal genera el patrón de ondas complejas.
    [unroll]
    for (int i = 0; i < 5; i++)
    {
        uv2 += sin(max(uv.x, uv.y)) + uv;
        
        // --- MODIFICACIÓN CLAVE PARA EL MOVIMIENTO ---
        // Aquí es donde introducimos el tiempo (Time) para que las ondas se muevan,
        // pero de una forma que no cause una rotación neta.
        
        // Esta línea usa el tiempo para desplazar suavemente las fases de las ondas.
        // `Time * 0.1` y `Time * 0.05` controlan la velocidad de esta animación sutil.
        uv += 0.5 * float2(
            cos(5.1123314 + 0.353 * uv2.y + Time * 0.1), // Ondulación sutil en X
            sin(uv2.x - 0.05 * Time) // Ondulación sutil en Y
        );
        
        // Mantenemos la interferencia de ondas original.
        uv -= 1.0 * cos(uv.x + uv.y) - 1.0 * sin(uv.x * 0.711 - uv.y);
    }
    
    // 3. Cálculo de color (Igual que el original)
    float contrast_mod = (0.25 * CONTRAST + 0.5 * SPIN_AMOUNT + 1.2);
    float paint_res = min(2.0, max(0.0, length(uv) * (0.035) * contrast_mod));
    float c1p = max(0.0, 1.0 - contrast_mod * abs(1.0 - paint_res));
    float c2p = max(0.0, 1.0 - contrast_mod * abs(paint_res));
    float c3p = 1.0 - min(1.0, c1p + c2p);
    float light = (LIGTHING - 0.2) * max(c1p * 5.0 - 4.0, 0.0) + LIGTHING * max(c2p * 5.0 - 4.0, 0.0);
    OutColor = (0.3 / CONTRAST) * Color1 + (1.0 - 0.3 / CONTRAST) * (Color1 * c1p + Color2 * c2p + float4(c3p * Color3.rgb, c3p * Color1.a)) + light;
}