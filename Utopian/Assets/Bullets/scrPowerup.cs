using UnityEngine;
using System.Collections;

public struct BulletPowerup
{
	public Color Colour;
	public float Speed;
	public float Size;
	public int Split;
	public float Wiggle;
	public float Erratic;
	public bool Homing;
	public bool Penetrative;

	public BulletPowerup(Color colour, float speed, float size, int split, float wiggle, float erratic, bool homing, bool penetrative)
	{
		Colour = colour;
		Speed = speed;
		Size = size;
		Split = split;
		Wiggle = wiggle;
		Erratic = erratic;
		Homing = homing;
		Penetrative = penetrative;
	}
}

public class scrPowerup : MonoBehaviour
{



	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}
}
