Shader "Custom/Texture3D"
{
    Properties
    {
        //_Color ("Color", Color) = (1,1,1,1)
        _Alpha("Alpha", Range(0,1)) = 1
        _Depth("Depth", Range(0,1)) = 1
        [Toggle(BLUE_RED_RAMP)]
        _ramp ("Ramp from blue to red to show life", Float) = 0
        [Toggle(TEX_RGB)]
        _texRGB ("Use raw texture RGB", Float) = 0
        _MainTex ("Albedo (RGB)", 3D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler3D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            //float alpha
        };

        half _Glossiness;
        half _Metallic;
        half _Alpha;
        half _ramp;
        half _texRGB;
        half _Depth;
        //fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex3D(_MainTex, float3(IN.uv_MainTex, _Depth)) * (1.f, 1.f, 1.f, 1.f);
            fixed life = c.r;
            o.Albedo = fixed3(life,0,1.f);
            o.Alpha = _Alpha;
            //If texture is build RGB
            if (_texRGB > .5f)
            {
                o.Albedo = fixed3(c.r, c.g, c.b);
                o.Alpha = _Alpha * c.a;
            }
            else
            {
                //If texture is build with only channel, aka life value
                //And if we want faded white
                if (_ramp < .5f)
                {
                    o.Albedo = fixed3(life, life, life);
                    o.Alpha = _Alpha * (life+.5f);
                }
                else
                //Else if we want ramped blue to red
                {
                    o.Albedo = fixed3(life, 0, 1 - life);
                    o.Alpha = _Alpha * c.a;
                }
            }

            //Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}