Shader "Saye/CubeShader"
{
	Properties
	{
		_Color ("Main Colour", Color) = (1.0, 1.0, 1.0, 1.0)
		_GlowColor("Glow Colour", Color) = (1.0, 1.0, 1.0, 1.0)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Geometry+2" "RenderType"="Opaque" }
 
		CGPROGRAM		
		#pragma surface surf Cube

		half4 LightingCube (SurfaceOutput s, half3 lightDir, half atten)
		{
			half n = dot(s.Normal, lightDir);
			half4 c;
			
			// If the alpha is 0, do not light.
			if (s.Alpha != 0)
				c.rgb = s.Albedo * _LightColor0.rgb * (n * atten * 2);
			else
				c.rgb = s.Albedo;
			
			c.a = 1.0;
			return c;
		}

		struct Input
		{
			float2 uv_MainTex;
	   	};

		half4 _Color;
		half4 _GlowColor;
		sampler2D _MainTex;


		void surf (Input IN, inout SurfaceOutput o)
		{		
			if (IN.uv_MainTex.x < 0.08 || IN.uv_MainTex.x > 0.92 || IN.uv_MainTex.y < 0.08 || IN.uv_MainTex.y > 0.92)
			{
				o.Albedo = _GlowColor.rgb;
				o.Alpha = 0;	// Here alpha is actually going to be used as an unlit flag.
			}
			else
			{
				o.Albedo = _Color.rgb;
				o.Alpha = 1;
			}
		}
		ENDCG
	} 
}