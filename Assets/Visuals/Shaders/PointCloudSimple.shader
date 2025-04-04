Shader "PointCloud/PointCloudSimple"
{
    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _PointSize("Point Size", Float) = 0.01
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Transparent"
        }
        Cull Back
        ZWrite On
        
        Pass
        {
            CGPROGRAM
            #define SQUARE_GEOMETRY 0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_Position;
                float4 color : COLOR;
            };

            StructuredBuffer<float> _PointBuffer;
            float4x4 _Transform;
            const float4 _Tint;
            const float _PointSize;
            const uint _Width;
            const uint _Height;
            const uint _Depth;
            const float _Size;
            const float fadedThreshold = 0.01;

            v2f vert(appdata v)
            {
                v2f o;
                float life = _PointBuffer[v.vertexID];
                float3 position = float3(
                    v.vertexID % _Width * _Size / float(_Width),
                    v.vertexID / _Width % _Height * _Size / float(_Height),
                    v.vertexID / (_Width * _Height) * _Size / float(_Depth)
                );
                //Treat life as an index for debug
                //float3 positionLife = float3(
                //    (life % _Width) * _Size / float(_Width),
                //    ((life / _Width) % _Height) * _Size / float(_Height),
                //    (life/ (_Width * _Height)) * _Size / float(_Depth)
                //);
                o.pos = UnityObjectToClipPos(float4(position, 1.0));
                //mul(
                //_Transform,
                //);
                
                // o.pointSize = _PointSize;

                float4 color;
                const float4 dead = float4(0, 0, 1, 0);// Blue
                const float4 mid = float4(0, 1, 0, 1); // Green
                const float4 full = float4(1, 0, 0, 1);// Red

                 // Interpolate between colors based on lifeValue
                if (life < 0.5)
                {
                    color = lerp(dead, mid, life * 2.0);
                }
                else if (life <= 1.0)
                {
                    color = lerp(mid, full, (life - 0.5) * 2.0);
                }
                else
                {
                    color = full;
                }
                // Display the UVW for debug :
                // color = float4(positionLife,1);
                o.color = color * _Tint;
                return o;
            }
            #if SQUARE_GEOMETRY
            [maxvertexcount(8)]
            void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
            {
                if (input[0].color.a <= fadedThreshold)
                {
                    return;
                }
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float xl = _PointSize;
                float yl = _PointSize * aspect;

                // create square
                v2f p11, p12, p13, p21;

                p11.color = p12.color = p13.color = p21.color = input[0].color;
                //p11.pointSize = p12.pointSize = p13.pointSize = p21.pointSize = p22.pointSize = p23.pointSize = input[0].color;

                p11.pos = float4(input[0].pos.x - xl, input[0].pos.y - yl, input[0].pos.z, input[0].pos.w);
                p12.pos = float4(input[0].pos.x + xl, input[0].pos.y - yl, input[0].pos.z, input[0].pos.w);
                p13.pos = float4(input[0].pos.x - xl, input[0].pos.y + yl, input[0].pos.z, input[0].pos.w);
                p21.pos = float4(input[0].pos.x + xl, input[0].pos.y + yl, input[0].pos.z, input[0].pos.w);

                triStream.Append(p11);
                triStream.Append(p12);
                triStream.Append(p13);
                
                triStream.Append(p21);
                triStream.Append(p13);
                triStream.Append(p12);
            }
            #else
            [maxvertexcount(36)]
            void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
            {
                if (input[0].color.a <= fadedThreshold)
                {
                    return;
                }
                
                float4 origin = input[0].pos;
                float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);
                
                // Copy the basic information.
                v2f o;
                o.color = input[0].color;
                o.pos = origin;
                triStream.Append(o);
                // Determine the number of slices based on the radius of the
                // point on the screen.
                float radius = extent.y / origin.w * _ScreenParams.y;
                uint slices = min((radius + 1) / 5, 4) + 2;

                // Slightly enlarge quad points to compensate area reduction.
                // Hopefully this line would be complied without branch.
                if (slices == 2) extent *= 1.2;

                // Top vertex
                o.pos.y = origin.y + extent.y;
                o.pos.xzw = origin.xzw;
                triStream.Append(o);

                UNITY_LOOP for (uint i = 1; i < slices; i++)
                {
                    float sn, cs;
                    sincos(UNITY_PI / slices * i, sn, cs);

                    // Right side vertex
                    o.pos.xy = origin.xy + extent * float2(sn, cs);
                    triStream.Append(o);

                    // Left side vertex
                    o.pos.x = origin.x - extent.x * sn;
                    triStream.Append(o);
                }

                // Bottom vertex
                o.pos.x = origin.x;
                o.pos.y = origin.y - extent.y;
                triStream.Append(o);

                triStream.RestartStrip();
            }
            #endif
            half4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}