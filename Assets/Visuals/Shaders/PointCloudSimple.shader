Shader "PointCloud/PointCloudSimple"
{
    Properties
    {
        _Tint("Tint", Color) = (1, 1, 1, 1)
        _PointSize("Point Size", Float) = 0.05
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
                float pointSize : PSIZE;
            };

            StructuredBuffer<float> _PointBuffer;
            float4x4 _Transform;
            float4 _Tint;
            float _PointSize;
            uint _Width;
            uint _Height;
            uint _Depth;
            float _Size;

            v2f vert(appdata v)
            {
                v2f o;
                float life = _PointBuffer[v.vertexID];
                float3 position = float3(
                    (v.vertexID % _Width) * _Size / float(_Width),
                    ((v.vertexID / _Width) % _Height) * _Size / float(_Height),
                    (v.vertexID / (_Width * _Height)) * _Size / float(_Depth)
                );
                //Treat life as an index for debug
                //float3 positionLife = float3(
                //    (life % _Width) * _Size / float(_Width),
                //    ((life / _Width) % _Height) * _Size / float(_Height),
                //    (life/ (_Width * _Height)) * _Size / float(_Depth)
                //);
                o.pos =
                    UnityObjectToClipPos(
                        float4(position, 1.0)
                    );
                //mul(
                //_Transform,
                //);
                
                o.pointSize = _PointSize;

                float4 color;
                float fadedThreshold = 0.05;
                float minAlpha = 0.0;//fadedThreshold;
                float4 dead = float4(0, 0, 1, 1); // Blue
                float4 mid = float4(0, 1, 0, 1); // Green
                float4 full = float4(1, 0, 0, 1); // Red

                 // Interpolate between colors based on lifeValue
                if (life < 0.5)
                {
                    color = lerp(dead, mid, life * 2.0);
                }
                else
                {
                    color = lerp(mid, full, (life - 0.5) * 2.0);
                }
                if (life <= fadedThreshold)
                {
                    //same as dead alpha on the threshold but ramping toward 0, espcially 0 on 0, for visilibity
                    color.a = max(minAlpha,(life / fadedThreshold)*dead.a);
                }
                //Display the UVW for debug :
                //color = float4(positionLife,1);
                o.color = color * _Tint;
                return o;
            }
            #if SQUARE_GEOMETRY
            [maxvertexcount(36)]
            void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
            {
                v2f o;
                o.pointSize = input[0].pointSize;
                float4 pos = input[0].pos;
                float4 color = input[0].color;

                // Define the vertices of the cube
                float3 offsets[8] = {
                    float3(-1, -1, -1),
                    float3(1, -1, -1),
                    float3(-1, 1, -1),
                    float3(1, 1, -1),
                    float3(-1, -1, 1),
                    float3(1, -1, 1),
                    float3(-1, 1, 1),
                    float3(1, 1, 1)
                };

                float4 vertices[8];
                for (int i = 0; i < 8; i++)
                {
                    vertices[i] =
                        //mul(_Transform,
                        pos + float4(offsets[i] * _PointSize, 0)
                        //)
                        ;
                }

                // Define the indices for the 12 triangles that make up the cube
                int3 indices[12] = {
                    int3(0, 1, 2), int3(1, 3, 2), // Front face
                    int3(4, 5, 6), int3(5, 7, 6), // Back face
                    int3(0, 1, 4), int3(1, 5, 4), // Bottom face
                    int3(2, 3, 6), int3(3, 7, 6), // Top face
                    int3(0, 2, 4), int3(2, 6, 4), // Left face
                    int3(1, 3, 5), int3(3, 7, 5) // Right face
                };

                // Generate the triangles for the cube
                for (int i = 0; i < 12; i++)
                {
                    o.pos = vertices[indices[i].x];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = vertices[indices[i].y];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = vertices[indices[i].z];
                    o.color = color;
                    triStream.Append(o);
                }
            }
            #else
            [maxvertexcount(36)]
            void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
            {
                float4 origin = input[0].pos;
                float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);
                
                // Copy the basic information.
                v2f o;
                o.pointSize = input[0].pointSize;
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