#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight(Surface surface, Light light)
{
    float nDotL = dot(surface.normal, light.direction);
    return saturate(nDotL * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surfaceWS, BRDF brdf, CustomGI gi)
{
    ShadowData shadowData = GetShadowData(surfaceWS);

    float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
    
    const int directionalLightCount = GetDirectionalLightCount();
    for (int i = 0; i < directionalLightCount; ++i)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }
    return color;
}

#endif
