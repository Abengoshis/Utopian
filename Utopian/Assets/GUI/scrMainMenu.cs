using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scrMainMenu : MonoBehaviour
{
	public static bool RidiculousMode = false;
	public static bool Registered = false;
	public static bool Unregistered = true;

	public GameObject Core;

	public Button PlayButton, TutorialButton, QuitButton;

	// Use this for initialization
	void Start ()
	{
		GameObject.Find ("Canvas").transform.Find ("Toggle").GetComponent<Toggle>().isOn = RidiculousMode;
		GameObject.Find ("Canvas").transform.Find ("Registered").GetComponent<Toggle>().isOn = Registered;
		GameObject.Find ("Canvas").transform.Find ("Unregistered").GetComponent<Toggle>().isOn = Unregistered;

		Screen.lockCursor = false;

		PlayButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 1.3f);
		TutorialButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 2.7f);
		QuitButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 4.1f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		Core.transform.Rotate (0, 0, 3 * Time.deltaTime);
	}

	public void Play()
	{
		Application.LoadLevel("Arena");
	}

	public void Tutorial()
	{
		Application.LoadLevel("Tutorial");
	}

	public void Quit()
	{
		Application.Quit();
	}

	public void ToggleRidiculousMode()
	{
		RidiculousMode = GameObject.Find ("Canvas").transform.Find ("Toggle").GetComponent<Toggle>().isOn;
	}

	public void ToggleRegistered()
	{
		Registered = GameObject.Find ("Canvas").transform.Find ("Registered").GetComponent<Toggle>().isOn;
	}

	public void ToggleUnregistered()
	{
		Unregistered = GameObject.Find ("Canvas").transform.Find ("Unregistered").GetComponent<Toggle>().isOn;
	}
}
