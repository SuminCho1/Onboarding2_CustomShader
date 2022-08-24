#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.distanceAttenuation) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(Surface surface, float3 positionWS, BRDF brdf)
{
    float3 color = 0.0f;

    color += GetLighting(surface, brdf, GetMainLight());

    int additionalLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightCount; ++i)
    {
        Light addtionalLight = GetAdditionalLight(i, positionWS);
        color += GetLighting(surface, brdf, addtionalLight);
    }
    
    return color;
}
#endif
