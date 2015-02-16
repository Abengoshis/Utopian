using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
[RequireComponent(typeof(AudioSource))]
public class scrScrollText : MonoBehaviour
{
	public float LettersPerSecond = 20;
	public float LetterTimer = 0;
	public string text = "";	// Naming difference so that GetComponent<Text>().text can easily be changed to GetComponent<scrScrollText>().text
	private Text component;

	public bool Running = false;

	void Start ()
	{
		component = GetComponent<Text>();

		if (string.IsNullOrEmpty(text))
			text = component.text;

		component.text = "";
	}

	void Update ()
	{			
		if (Running)
		{
			if (LetterTimer >= text.Length)
			{
				audio.Stop ();

				if (LetterTimer >= text.Length + 1)
					Running = false;
			}
			else
			{
				if (!audio.isPlaying)
					audio.Play();
			}

			LetterTimer += Time.deltaTime * LettersPerSecond;
			component.text = text.Substring(0, (int)Mathf.Min (LetterTimer, text.Length));
		}
	}

	public void Run()
	{
		Running = true;
	}
	
	public void Restart()
	{
		component.text = "";
		LetterTimer = 0;
		Run ();
	}
	
	public void ChangeText(string replacement)
	{
		text = replacement;
		Restart ();
	}
	
	public void Stop()
	{
		Running = false;
	}
}
