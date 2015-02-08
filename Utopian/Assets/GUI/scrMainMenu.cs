using UnityEngine;
using System.Collections;

public class scrMainMenu : MonoBehaviour
{
	public GameObject Grid;

	public AudioClip SndButtonPress;

	
	// Use this for initialization
	void Start ()
	{
		Screen.lockCursor = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Grid.renderer.material.SetFloat("_PlayerX", mouse.x);
		Grid.renderer.material.SetFloat("_PlayerY", mouse.y);
	}

	public void Play()
	{
		Application.LoadLevel(1);
	}

	public void Quit()
	{
		Application.Quit();
	}
}
