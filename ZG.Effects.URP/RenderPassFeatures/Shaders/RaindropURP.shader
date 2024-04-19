Shader "ZG/RaindropURP"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

#define S(a, b, t) smoothstep(a, b, t)

    struct RaindropVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float2 uvStereo   : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    float _ElpasedTime = 0.0f;

    float _RainZoom = 0.5f;
    float _RainAmount = 0.3;// M.y;

    float _MaxBlur = 5.0f;
    float _MinBlur = 2.0f;

    float3 N13(float p) 
    {
        //  from DAVE HOSKINS
        float3 p3 = frac(float3(p, p, p) * float3(.1031, .11369, .13787));
        p3 += dot(p3, p3.yzx + 19.19);
        return frac(float3((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y, (p3.y + p3.z) * p3.x));
    }

    float4 N14(float t) 
    {
        return frac(sin(t * float4(123., 1024., 1456., 264.)) * float4(6547., 345., 8799., 1564.));
    }

    float N(float t) 
    {
        return frac(sin(t * 12345.564) * 7658.76);
    }

    float Saw(float b, float t) 
    {
        return S(0., b, t) * S(1., b, t);
    }

    float2 DropLayer2(float2 uv, float t) 
    {
        float2 UV = uv;

        uv.y += t * 0.75;
        float2 a = float2(6., 1.);
        float2 grid = a * 2.;
        float2 id = floor(uv * grid);

        float colShift = N(id.x);
        uv.y += colShift;

        id = floor(uv * grid);
        float3 n = N13(id.x * 35.2 + id.y * 2376.1);
        float2 st = frac(uv * grid) - float2(.5, 0);

        float x = n.x - .5;

        float y = UV.y * 20.;
        float wiggle = sin(y + sin(y));
        x += wiggle * (.5 - abs(x)) * (n.z - .5);
        x *= .7;
        float ti = frac(t + n.z);
        y = (Saw(.85, ti) - .5) * .9 + .5;
        float2 p = float2(x, y);

        float d = length((st - p) * a.yx);

        float mainDrop = S(.4, .0, d);

        float r = sqrt(S(1., y, st.y));
        float cd = abs(st.x - x);
        float trail = S(.23 * r, .15 * r * r, cd);
        float trailFront = S(-.02, .02, st.y - y);
        trail *= trailFront * r * r;

        y = UV.y;
        float trail2 = S(.2 * r, .0, cd);
        float droplets = max(0., (sin(y * (1. - y) * 120.) - st.y)) * trail2 * trailFront * n.z;
        y = frac(y * 10.) + (st.y - .5);
        float dd = length(st - float2(x, y));
        droplets = S(.3, 0., dd);
        float m = mainDrop + droplets * r * trailFront;

        //m += st.x>a.y*.45 || st.y>a.x*.165 ? 1.2 : 0.;
        return float2(m, trail);
    }

    float StaticDrops(float2 uv, float t) 
    {
        uv *= 40.;

        float2 id = floor(uv);
        uv = frac(uv) - .5;
        float3 n = N13(id.x * 107.45 + id.y * 3543.654);
        float2 p = (n.xy - .5) * .7;
        float d = length(uv - p);

        float fade = Saw(.025, frac(t + n.z));
        float c = S(.3, 0., d) * frac(n.z * 10.) * fade;
        return c;
    }

    float2 Drops(float2 uv, float t, float l0, float l1, float l2) 
    {
        float s = StaticDrops(uv, t) * l0;
        float2 m1 = DropLayer2(uv, t) * l1;
        float2 m2 = DropLayer2(uv * 1.85, t) * l2;

        float c = s + m1.x + m2.x;
        c = S(.3, 1., c);

        return float2(c, max(m1.y * l0, m2.y * l1));
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float2 uvStereo = (((uv * _ScreenParams.xy) - .5 * _ScreenParams.xy) / _ScreenParams.y)* _RainZoom;

        float elapsedTime = _ElpasedTime;

        //uv *= 0.3f;
        //UV = (UV - .5)*(.9 + zoom * .1) + .5;

        float staticDrops = S(-.5, 1., _RainAmount) * 2.;
        float layer1 = S(.25, .75, _RainAmount);
        float layer2 = S(.0, .5, _RainAmount);

        float2 c = Drops(uvStereo, elapsedTime, staticDrops, layer1, layer2);
        #ifdef CHEAP_NORMALS
        float2 n = float2(ddx(c.x), ddy(c.x));// cheap normals (3x cheaper, but 2 times shittier ;))
        #else
        float2 e = float2(.001, 0.);
        float cx = Drops(uvStereo + e, elapsedTime, staticDrops, layer1, layer2).x;
        float cy = Drops(uvStereo + e.yx, elapsedTime, staticDrops, layer1, layer2).x;
        float2 n = float2(cx - c.x, cy - c.x);		// expensive normals
        #endif

        float focus = lerp(_MaxBlur - c.y, _MinBlur, S(.1, .2, c.x));
        float4 lod = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, float2(uv.x + n.x, uv.y + n.y), focus);

        return float4(lod.rgb, lod.a);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Raindrop"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
