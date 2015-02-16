using UnityEngine;
using System.Collections;

public class scrCamera : MonoBehaviour
{
	public static scrCamera Instance { get; private set; }
	public delegate void method();
	public static method PostRender;

	public Material MatOpenGL;

	public GameObject ChildMap;


	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		ChildMap.renderer.material.SetFloat("_PlayerX", scrPlayer.Instance.transform.position.x);
		ChildMap.renderer.material.SetFloat("_PlayerY", scrPlayer.Instance.transform.position.y);

		Texture2D t = new Texture2D(scrNodeMaster.GRID_SIZE, scrNodeMaster.GRID_SIZE, TextureFormat.RGBA32, false);
		t.filterMode = FilterMode.Point;
		for (int x = 0; x < scrNodeMaster.GRID_SIZE; ++x)
		{
			for (int y = 0; y < scrNodeMaster.GRID_SIZE; ++y)
			{
				if (scrNodeMaster.CellStates[x, y] > 0)
				{
					t.SetPixel(x, y, new Color((int)(scrNodeMaster.CellStates[x, y]) / 255.0f, 0, 0));
				}
			}
		}

		if (scrNodeMaster.Instance.NodeBeingUploaded != null)
		{
			int ux, uy;
			ux = scrNodeMaster.ToCellSpace(scrNodeMaster.Instance.NodeBeingUploaded.transform.position.x);
			uy = scrNodeMaster.ToCellSpace(scrNodeMaster.Instance.NodeBeingUploaded.transform.position.y);

			t.SetPixel(ux, uy, t.GetPixel(ux, uy) + new Color(0, 1.0f, 0, 0));
		}

		t.Apply();
		ChildMap.renderer.material.SetTexture("_CellStates", t);
	}

	void OnPostRender()
	{
		if (PostRender != null)
			PostRender();
	}
}
