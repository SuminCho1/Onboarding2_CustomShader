#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0f - MIN_REFLECTIVITY;
    return range - metallic * range;
}

float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Sq(saturate(dot(surface.normal, h)));
    float lh2 = Sq(saturate(dot(light.direction, h)));
    float r2 = Sq(brdf.roughness);
    float d2 = Sq(nh2 * (r2 - 1.0f) + 1.00001f);
    float normalization = brdf.roughness * 4.0f + 2.0f;
    return r2 / (d2 * max(0.1f, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;

    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    float perceptualRoughness =
        PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    
    return brdf;
}

#endif
