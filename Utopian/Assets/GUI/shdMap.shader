Shader "Saye/Map"
{
	Properties
	{
		_BlockedColor ("Blocked Colour", Color) = ( 0.5, 0.5, 0.5, 1.0)
		_UninfectedColor ("Uninfected Colour", Color) = (0.0, 1.0, 1.0, 1.0)
		_InfectedColor ("Infected Colour", Color) = (1.0, 0.5, 0.0, 1.0)
		_GridSize ("Grid Size", int) = 10
		_CellSize ("Cell Size", int) = 1
		_PlayerX ("Player X", float) = 0
		_PlayerY ("Player Y", float) = 0
		
		_CellStates ("Cell State Grid", 2D) = "black" {}
		
				
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			half4 _BlockedColor;
			half4 _UninfectedColor;
			half4 _InfectedColor;
			int _GridSize;
			int _CellSize;
			float _PlayerX;
			float _PlayerY;
			sampler2D _CellStates;
			
			
			float toCellX(float value)
			{
				return ((((int)value / _CellSize ) + 1) / (float)_GridSize) + 0.5;
			}
			
			float toCellY(float value)
			{
				return ((int)(value / _CellSize) / (float)_GridSize) + 0.5;
			}
			
			float4 frag(v2f_img i) : COLOR
			{			
				float2 uv = i.uv;
				//if ((uv.x + uv.y + _Time.x * 0.2) % 0.02 < 0.01)
					//return fixed4(1.0,1.0,1.0,0.0);
				
				float grid = _GridSize * _CellSize;
				float cell = _CellSize / grid;
				
				// Check if occupied by the player.
				float px = ((_PlayerX / _CellSize + 0.5) / (float)_GridSize) + 0.5;
				float py = ((_PlayerY / _CellSize + 0.5) / (float)_GridSize) + 0.5;
				if (px > uv.x + cell * 0.3 && px < uv.x + cell * 0.7 && py > uv.y + cell * 0.3 && py < uv.y + cell * 0.7)
					return fixed4(1.0, 1.0, 1.0, 0.8);
				
				// Grid lines.
				float x = frac(uv.x * _GridSize);
				float y = frac(uv.y * _GridSize);
				if (x > 0.975 || x < 0.025 || y > 0.975 || y < 0.025)
					return fixed4(1.0, 1.0, 1.0, 0.5);
					
				// Check the state of the cell.				
				half4 colour = tex2D(_CellStates, uv);
				
				// Uninfected/infected state.
				if (x < 0.8 && x > 0.2 && y < 0.8 && y > 0.2)
				{
					if (colour.r == 1/255.0)
						return _BlockedColor;
				
					if (colour.r == 2/255.0)
						return _UninfectedColor;
					
					if (colour.r == 3/255.0)
						return _InfectedColor;	
								
					if (colour.r == 4/255.0)
					{
						if ((y - x + _Time.y) % 0.6 < 0.3)
							return _InfectedColor;
						
						return _UninfectedColor;
					}
					
					if (colour.r == 5/255.0)
					{
						if ((y - x + _Time.y) % 0.6 < 0.3)
							return _InfectedColor;
						
						return _BlockedColor;
					}
				}
				else
				{
					if (colour.g == 1 && (_Time.y % 1 < 0.5))
						return fixed4(1.0, 1.0, 1.0, 0.9);
				}
				
				return fixed4(0.0, 0.0, 0.0, 0.2);
				
			}
			
			ENDCG
		}	
	}
}