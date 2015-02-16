using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scrMainMenu : MonoBehaviour
{
	public GameObject Ship;

	public Button PlayButton, OptionsButton, QuitButton;

	// Use this for initialization
	void Start ()
	{
		Screen.lockCursor = false;

		PlayButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 1.2f);
		OptionsButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 1.6f);
		QuitButton.transform.Find ("Text").GetComponent<scrScrollText>().Invoke("Run", 2.0f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		Ship.transform.Rotate (6 * Time.deltaTime, 10 * Time.deltaTime, 3 * Time.deltaTime);
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
