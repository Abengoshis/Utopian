using UnityEngine;
using System.Collections;

public class scrGUI : MonoBehaviour
{
	public static scrGUI Instance { get; private set; }
	public static Canvas GUICanvas { get; private set; }

	// Use this for initialization
	void Start ()
	{
		Instance = this;
		GUICanvas = this.GetComponent<Canvas>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		transform.Find ("Cursor").position = Camera.main.WorldToScreenPoint(scrPlayer.Instance.SimulatedCursorPosition);


	}
}
