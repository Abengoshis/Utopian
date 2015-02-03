using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrAICore : MonoBehaviour
{
	public static scrAICore Instance { get; private set; }

	public float Corruption { get; private set; }
	const float CORRUPTION_MAX = 1000.0f;

	Slider corruptionSlider;

	void Start ()
	{
		Instance = this;

		corruptionSlider = GameObject.Find ("GUICanvas").transform.Find ("CorruptionSlider").GetComponent<Slider>();
	}

	void Update ()
	{
	
	}

	void Corrupt(int infection)
	{
		Corruption += infection;
		corruptionSlider.value = Corruption / CORRUPTION_MAX;
	}


}
