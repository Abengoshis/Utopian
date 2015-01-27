Shader "Saye/Grid"
{
	Properties
	{
		_GridSize ("Grid Size", int) = 100
		_CellSize ("Cell Size", int) = 10
		_PlayerX ("Player X", float) = 0
		_PlayerY ("Player Y", float) = 0	// jusst have an array of the player position + all nodes!
				
	}

	SubShader
	{
		Tags { "Queue" = "Background" }
		ZWrite Off
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			int _GridSize;
			int _CellSize;
			float _PlayerX;
			float _PlayerY;
			
			float4 frag(v2f_img i) : COLOR
			{
				float2 uv = i.uv;
				float size = _GridSize * _CellSize;				
		
				// Process warps. --------
								
				// Get the direction to the player.
				float xDir = _PlayerX - uv.x * size + size * 0.5;
				float yDir = _PlayerY - uv.y * size + size * 0.5;
				
				// Get the distance to the player.
				float dist = sqrt(xDir * xDir + yDir * yDir);
								
				// Normalise the direction.
				xDir /= dist;
				yDir /= dist;
			
				float strength = 0.0000005f;	// put this in the array too
			
				// Push the coordinates out through the direction, less over distance.
				uv.x += (xDir * strength);
				uv.y += (yDir * strength);
				
				if (dist < strength * 400)
				{
					return fixed4(0.0, 0.0, 0.0, 1.0);
				}
				
				
				// Draw the grid. --------
				
				float x = frac(uv.x * _GridSize);
				float y = frac(uv.y * _GridSize);
				
				if (x > 0.99 || y > 0.99)
				{
					return fixed4(0.4, 0.4, 0.4, 1.0);
				}
				else if (x > 0.495 && x < 0.505 || y > 0.495 && y < 0.505)
				{
					return fixed4(0.2, 0.2, 0.2, 1.0);
				}
				else if (x > 0.245 && x < 0.255 || x > 0.745 && x < 0.755 ||
						 y > 0.245 && y < 0.255 || y > 0.745 && y < 0.755)
				{
					return fixed4(0.1, 0.1, 0.1, 1.0);		 
				}
				else
				{
					return fixed4(0.0, 0.0, 0.0, 1.0);
				}
			
				
			}
			
			ENDCG
		}	
	}
}