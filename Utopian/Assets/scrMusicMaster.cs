using UnityEngine;
using System.Collections;

public class scrMusicMaster : MonoBehaviour
{
	public static scrMusicMaster Instance { get; private set; }


	public AudioClip Intro, Loop, Outtro;

	// Use this for initialization
	void Start ()
	{
		Instance = this;

		enabled = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!audio.isPlaying)
		{
			// todo:
			audio.Play();
		}
	}
}
