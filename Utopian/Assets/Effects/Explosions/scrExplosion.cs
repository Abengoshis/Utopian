using UnityEngine;
using System.Collections;

public class scrExplosion : MonoBehaviour
{
	public float Duration;
	float timer = 0.0f;

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		timer += Time.deltaTime;
		if (timer >= Duration)
		{
			Destroy (gameObject);
		}
	}
}
