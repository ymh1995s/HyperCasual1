// New procedural skybox shader optimized for hyper-casual style
Shader "Custom/HyperCasualSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.20, 0.60, 1.00, 1.0)
        _HorizonColor ("Horizon Color", Color) = (1.00, 0.80, 0.60, 1.0)
        _BottomColor ("Bottom Color", Color) = (1.00, 0.95, 0.85, 1.0)
        _Exponent ("Blend Exponent", Range(0.1, 8.0)) = 1.5
        _Rotation ("Rotation (degrees)", Range(0, 360)) = 0
        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0.04
        _NoiseScale ("Noise Scale", Float) = 12.0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Use object-to-world to get a stable direction for skybox vertices
                float3 worldDir = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.dir = normalize(worldDir);
                return o;
            }

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            float _Exponent;
            float _Rotation;
            float _NoiseIntensity;
            float _NoiseScale;

            float2 Rotate2D(float2 p, float ang)
            {
                float s = sin(ang);
                float c = cos(ang);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            // Simple hash / value noise for subtle variation
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = i.dir;
                // Rotate around Y axis for simple sky rotation
                float ang = radians(_Rotation);
                float2 dirXZ = Rotate2D(dir.xz, ang);
                dir.x = dirXZ.x; dir.z = dirXZ.y;

                // Map y (-1..1) to 0..1
                float t = saturate(dir.y * 0.5 + 0.5);
                // Apply exponent to control contrast
                t = pow(t, _Exponent);

                fixed4 top = _TopColor;
                fixed4 horizon = _HorizonColor;
                fixed4 bottom = _BottomColor;

                // Blend: bottom -> horizon -> top
                // Create a two-stage blend where the horizon occupies the middle band
                float horizonBand = smoothstep(0.35, 0.65, t);
                fixed4 upper = lerp(horizon, top, saturate((t - 0.5) * 2.0));
                fixed4 color = lerp(bottom, upper, horizonBand);

                // Add subtle noise/banding for stylized hyper-casual look
                float n = noise(dir.xz * _NoiseScale) * _NoiseIntensity;
                color.rgb += n;

                return color;
            }
            ENDCG
        }
    }

    FallBack "RenderFX/Skybox"
}
