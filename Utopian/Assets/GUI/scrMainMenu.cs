using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scrMainMenu : MonoBehaviour
{
	public GameObject Core;

	public Button PlayButton, OptionsButton, QuitButton;

	// Use this for initialization
	void Start ()
	{
		Screen.lockCursor = false;

		PlayButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 1.3f);
		OptionsButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 2.7f);
		QuitButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 4.1f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		Core.transform.Rotate (0, 0, 3 * Time.deltaTime);
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
