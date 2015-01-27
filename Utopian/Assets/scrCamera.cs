using UnityEngine;
using System.Collections;

public class scrCamera : MonoBehaviour
{
	public static scrCamera Instance { get; private set; }
	public delegate void method();
	public static method PostRender;

	public Material MatOpenGL;


	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	void OnPostRender()
	{
		if (PostRender != null)
			PostRender();
	}
}
