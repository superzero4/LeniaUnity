Shader "Unlit/Lenia2DHLSL"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            RWTexture2D<float> _MainTex;
            sampler2D sampler_MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;

            // Common

            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(in float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2;
                const float K2 = 0.211324865; // (3-sqrt(3))/6;

                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float m = step(a.y, a.x);
                float2 o = float2(m, 1.0 - m);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, hash(i + 0.0)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));
                return dot(n, float3(70., 70., 70.));
            }

            // Buffer A

            const float R = 15.; // space resolution = kernel radius
            const float T = 10.; // time resolution = number of divisions per unit time
            const float dt = 1. / 10.; // time step
            const float mu = 0.14; // growth center
            const float sigma = 0.014; // growth width
            const float rho = 0.5; // kernel center
            const float omega = 0.15; // kernel width

            float bell(float x, float m, float s)
            {
                return exp(-(x - m) * (x - m) / s / s / 2.); // bell-shaped curve
            }

            float4 mainImage(in float2 fragCoord)
            {

                int2 iResolution = _MainTex_TexelSize.xy;
                
                float2 uv = fragCoord / iResolution.xy;

                float sum = 0.;
                float total = 0.;
                for (int x = -int(R); x <= int(R); x++)
                    for (int y = -int(R); y <= int(R); y++)
                    {
                        float r = sqrt(float(x * x + y * y)) / R;
                        float2 txy = fmod((fragCoord + float2(x, y)) / iResolution.xy, 1.);
                        float val = tex2D(sampler_MainTex, txy).r;
                        float weight = bell(r, rho, omega);
                        sum += val * weight;
                        total += weight;
                    }
                float avg = sum / total;

                float val = tex2D(sampler_MainTex, uv).r;
                float growth = bell(avg, mu, sigma) * 2. - 1.;
                float c = clamp(val + dt * growth, 0., 1.);

                if (_Time.x < 1.) // || iMouse.z > 0.
                    c = 0.013 + noise(fragCoord / R + fmod(_Time.y, 1.) * 100.);
                /*
                if (iMouse.z > 0.)
                {
                    float d = length((fragCoord.xy - iMouse.xy) / iResolution.xx);
                    if (d <= R / iResolution.x)
                        c = 0.02 + noise(fragCoord / R + fmod(_Time.y, 1.) * 100.);
                }
                */
                
                return float4(c, c, c, 1.);
            }


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = mainImage(i.uv);
                return col;
            }
            ENDCG
        }
    }
}