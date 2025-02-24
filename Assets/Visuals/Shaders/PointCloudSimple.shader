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
            "RenderType"="Opaque"
        }
        Pass
        {
            CGPROGRAM
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

            StructuredBuffer<float4> _PointBuffer;
            float4x4 _Transform;
            float4 _Tint;
            float _PointSize;

            v2f vert(appdata v)
            {
                v2f o;
                float4 p = _PointBuffer[v.vertexID];
                float3 position = p.xyz;
                float4 color = float4(p.w, p.w, p.w, 1);
                o.pos = UnityObjectToClipPos(float4(position, 1.0));
                //mul(
                //_Transform,
                //);
                o.color = color * _Tint;
                o.pointSize = _PointSize;
                return o;
            }

            [maxvertexcount(12)]
            void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
            {
                v2f o;
                o.pointSize = input[0].pointSize;
                float4 pos = input[0].pos;
                float4 color = input[0].color;

                // Define the base vertices of the pyramid
                float3 baseOffsets[4] = {float3(-.5, -1, -.5), float3(0, 1, 0), float3(.5, -1, 0), float3(-.5, -1, .5)};
                float4 baseVertices[4];
                for (int i = 0; i < 4; i++)
                {
                    baseVertices[i] = pos + float4(baseOffsets[i] * _PointSize, 0);
                }

                // Define the apex of the pyramid
                float4 apex = pos + float4(0, 0, _PointSize * 2, 0);

                // Generate the base of the pyramid (2 triangles)
                for (int i = 0; i < 4; i++)
                {
                    o.pos = baseVertices[i];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = baseVertices[(i + 1) % 4];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = baseVertices[(i + 2) % 4];
                    o.color = color;
                    triStream.Append(o);
                }

                // Generate the sides of the pyramid (4 triangles)
                for (int i = 0; i < 4; i++)
                {
                    o.pos = baseVertices[i];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = baseVertices[(i + 1) % 4];
                    o.color = color;
                    triStream.Append(o);

                    o.pos = apex;
                    o.color = color;
                    triStream.Append(o);
                }
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}