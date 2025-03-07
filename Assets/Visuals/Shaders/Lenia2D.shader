// maximum 16 kernels by using 4x4 matrix
// when matrix operation not available (e.g. exp, mod, equal, /), split into four float4 operations



Shader "Unlit/Lenia2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // EPSILON ("Epsilon", float) = 0.000001
        // samplingDist ("Sampling distance", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Change these for funni stuff
            const float EPSILON = 0.000001f;
            const float samplingDist = 1.;
            
            const int2 iResolution = float2(64, 64);

            
            const int4 iv0 = int4(0,0,0,0);
            const int4 iv1 = int4(1,1,1,1);
            const int4 iv2 = int4(2,2,2,2);
            const int4 iv3 = int4(3,3,3,3);
            const float4 v0 = float4(0.,0.,0.,0.);
            const float4 v1 = float4(1.,1.,1.,1.);
            //const float4x4 m0 = float4x4(v0, v0, v0, v0);
            //const float4x4 m1 = float4x4(v1, v1, v1, v1);
            

            // Règles à changer
            // species: GDNQYX Tessellatium (stable)
            const float baseNoise = 0.175;
            const float R = 12;  // space resolution = kernel radius
            const float T = 1;  // time resolution = number of divisions per unit time
            const float4x4    betaLen = float4x4(1., 1., 2., 2., 1., 2., 1., 1., 1., 2., 2., 2., 1., 2., 1., 0);  // kernel ring number
            const float4x4      beta0 = float4x4(1., 1., 1., 0., 1., 5./6., 1, 1, 1, 11./12., 3./4., 1, 1, 1./6., 1., 0.);  // kernel ring heights
            const float4x4      beta1 = float4x4(0., 0., 1./4., 1., 0., 1., 0., 0., 0., 1., 1., 11./12., 0., 1., 0., 0.);
            const float4x4      beta2 = float4x4(0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0., 0.);
            const float4x4         mu = float4x4(0.242, 0.375, 0.194, 0.122, 0.413, 0.221, 0.192, 0.492, 0.426, 0.361, 0.464, 0.361, 0.235, 0.381, 0.216, 0.);  // growth center
            const float4x4      sigma = float4x4(0.061, 0.1553, 0.0361, 0.0531, 0.0774, 0.0365, 0.0649, 0.1219, 0.1759, 0.1381, 0.1044, 0.0686, 0.0924, 0.1118, 0.0748, 1.);  // growth width
            const float4x4        eta = float4x4(0.144, 0.506, 0.332, 0.3, 0.502, 0.58, 0.344, 0.268, 0.582, 0.326, 0.418, 0.642, 0.39, 0.378, 0.294, 0.);  // growth strength
            const float4x4       relR = float4x4(0.98, 0.59, 0.5, 0.93, 0.73, 0.88, 0.93, 0.61, 0.84, 0.7, 0.57, 0.73, 0.74, 0.87, 0.72, 1.);  // relative kernel radius
            const float4x4        src = float4x4(0., 0, 0, 1., 1., 1., 2., 2., 2., 0., 0., 1., 1., 2., 2., 0.);  // source channels
            const float4x4        dist= float4x4(0., 0., 0., 1., 1., 1., 2., 2., 2., 1., 2., 0., 2., 0., 1., 0.);  // destination channels
            
            // precalculate
            static const int intR = int(ceil(R));
            static const float dt = 1/T; // time step

            static const float4 kmv = float4(0.5,0.5,0.5,0.5); // kernel ring center
            const float4x4 kmu = float4x4(kmv, kmv, kmv, kmv);
            static const float4 ksv = float4(0.15,0.15,0.15,0.15); // kernel ring width
            const float4x4 ksigma = float4x4(ksv, ksv, ksv, ksv);

            static const int4 src0 = int4(src[0]), src1 = int4(src[1]), src2 = int4(src[2]), src3 = int4(src[3]);
            static const int4 dst0 = int4(dist[0]), dst1 = int4(dist[1]), dst2 = int4(dist[2]), dst3 = int4(dist[3]);

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
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
            
            // Noise simplex 2D by iq - https://www.shadertoy.com/view/Msf3WH
            float2 hash( float2 p )
            {
	            p = float2( dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)) );
	            return -1.0 + 2.0 * frac(sin(p)*43758.5453123);
            }

            float noise( in float2 p )
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2;
                const float K2 = 0.211324865; // (3-sqrt(3))/6;

	            float2 i = floor(p + (p.x+p.y) * K1);
                float2 a = p - i + (i.x+i.y) * K2;
                float  m = step(a.y,a.x); 
                const float2 o = float2(m,1.0-m);
                const float2 b = a - o + K2;
	            const float2 c = a - 1.0 + 2.0 * K2;
                const float3 h = max(0.5 - float3(dot(a,a), dot(b,b), dot(c,c)), 0.0);
	            const float3 n = h * h * h * h * float3(dot(a,hash(i + 0.0)), dot(b,hash(i + o)), dot(c,hash(i + 1.0)));
                return dot(n, float3(70.0, 70.0, 70.0));
            }
            
            // Functions
            // modified from SmoothLife by davidar - https://www.shadertoy.com/view/Msy3RD
            // bell-shaped curve (Gaussian bump)
            float4x4 bell(const in float4x4 x, const in float4x4 m, const in float4x4 s)
            {
                float4x4 v = -((x-m) * (x-m))/s/s/2;
                return float4x4(exp(v[0]), exp(v[1]), exp(v[2]), exp(v[3]));
            }

            // get neighbor weights (vectorized) for given radius
            float4x4 getWeight(const in float r, const in float4x4 relR)
            {
                const float4x4 br = betaLen / relR * r;
                const int4 br0 = br[0];
                const int4 br1 = br[1];
                const int4 br2 = br[2];
                const int4 br3 = br[3];

                // This is fucked
                float4 idk1 = beta0[0] * float4(br0 == 0) + beta1[0] * float4(br0 == 1) + beta2[0] * float4(br0 == 2);
                float4 idk2 = beta0[1] * float4(br1 == 0) + beta1[1] * float4(br1 == 1) + beta2[1] * float4(br1 == 2);
                float4 idk3 = beta0[2] * float4(br2 == 0) + beta1[2] * float4(br2 == 1) + beta2[2] * float4(br2 == 2);
                float4 idk4 = beta0[3] * float4(br3 == 0) + beta1[3] * float4(br3 == 1) + beta2[3] * float4(br3 == 2);
                
                // (Br==0 ? beta0 : 0) + (Br==1 ? beta1 : 0) + (Br==2 ? beta2 : 0)
                float4x4 height = float4x4(idk1,idk2,idk3,idk4);

                float4 huh1 = br[0] % 1;
                float4 huh2 = br[1] % 1;
                float4 huh3 = br[2] % 1;
                float4 huh4 = br[3] % 1;

                const float4x4 mod1 = float4x4(huh1, huh2, huh3, huh4) ;
                return height * bell(mod1, kmu, ksigma);
            }

            // get colors (vectorized) from source channels
            float4 getSrc(in float3 v, const in int4 srcv)
            {
                return
                    v.r * float4(srcv == 0) + 
                    v.g * float4(srcv == 1) +
                    v.b * float4(srcv == 2);
            }

            // get color for destination channel
            float getDst(const in float4x4 m, const in int4 ch)
            {
                return 
                    dot(m[0], float4(dst0 == ch)) + 
                    dot(m[1], float4(dst1 == ch)) + 
                    dot(m[2], float4(dst2 == ch)) + 
                    dot(m[3], float4(dst3 == ch));
            }

            // get values at given position
            float4x4 getVal(const in float2 xy)
            {
                const float2 txy = (xy / iResolution.xy) % 1;
                const fixed4 val = tex2D(_MainTex, txy);
                return float4x4(getSrc(val, src0), getSrc(val, src1), getSrc(val, src2), getSrc(val, src3));
            }

            // draw the shape of kernels
            float3 drawKernel(const in float2 uv)
            {
                int2 ij = int2(uv / 0.25);  // 0..3
                float2 xy = (uv % 0.25) * 8. - 1.;  // -1..1
                if (ij.x > 3) return float3(0,0,0);
                float3 rgb = getWeight(length(xy), relR)[3-ij.y][ij.x];
                return rgb;
            }

            float4 mainImage(const float2 fragCoord)
            {
                const float2 uv = fragCoord / iResolution.xy;

                // loop through the neighborhood, optimized: same weights for all quadrants/octants
                // calculate the weighted average of neighborhood from source channel
                float4x4 sum = 0., total = 0.;
                // self
                float r = 0.;
                float4x4 weight = getWeight(r, relR);
                float4x4 valSrc = getVal(fragCoord + float2(0., 0.)); sum += valSrc * weight; total += weight;
                // orthogonal
                for (int x=1; x<=intR; x++)
                {
                    r = float(x) / R;
                    weight = getWeight(r, relR);
                    valSrc = getVal(fragCoord + float2(+x, 0.) * samplingDist); sum += valSrc * weight; total += weight;
                    valSrc = getVal(fragCoord + float2(-x, 0.) * samplingDist); sum += valSrc * weight; total += weight;
                    valSrc = getVal(fragCoord + float2(0., +x) * samplingDist); sum += valSrc * weight; total += weight;
                    valSrc = getVal(fragCoord + float2(0., -x) * samplingDist); sum += valSrc * weight; total += weight;
                }
                // diagonal
                for (int x=1; x<=intR; x++)
                {
                    r = sqrt(2.) * float(x) / R;
                    if (r <= 1.) {
                        weight = getWeight(r, relR);
                        valSrc = getVal(fragCoord + float2(+x, +x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(+x, -x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-x, +x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-x, -x) * samplingDist); sum += valSrc * weight; total += weight;
                    }
                }
                // others
                for (int y=1; y<=intR-1; y++)
                for (int x=y+1; x<=intR; x++)
                {
                    r = sqrt(float(x*x + y*y)) / R;
                    if (r <= 1.) {
                        weight = getWeight(r, relR);
                        valSrc = getVal(fragCoord + float2(+x, +y) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(+x, -y) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-x, +y) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-x, -y) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(+y, +x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(+y, -x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-y, +x) * samplingDist); sum += valSrc * weight; total += weight;
                        valSrc = getVal(fragCoord + float2(-y, -x) * samplingDist); sum += valSrc * weight; total += weight;
                    }
                }
                float4x4 avg = sum / (total + EPSILON);    // avoid divided by zero

                // calculate growth, add a small portion to destination channel
                const float4x4 growth = eta * bell(avg, mu, sigma) * 2. - 1.;
                const float3 growthDst = float3( getDst(growth, 0.), getDst(growth, 1.), getDst(growth, 2.));
                const float3 val = tex2D(_MainTex, uv);
                float3 rgb = clamp(dt * growthDst + val, 0., 1.);

                // debug: uncomment to show list of kernels
                // rgb = drawKernel(fragCoord / iResolution.y);

                // randomize at start, or add patch on mouse click
                // if (iFrame == 0 || iMouse.z > 0.)
                if (_Time.x == 0.)
                {
                    float x = noise(fragCoord/R/samplingDist + _Time.x % 1.) * 100;
                    float y = noise(fragCoord/R/samplingDist + _SinTime.x * 100);
                    float z = noise(fragCoord/R/samplingDist + _CosTime.x * 100);
                    
                    rgb = baseNoise + float3(x,y,z);
                }

                return float4(rgb, 1.);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = mainImage(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
