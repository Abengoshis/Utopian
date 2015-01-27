using UnityEngine;
using System.Collections;

public class scrCube : MonoBehaviour
{
	public bool Infected { get; private set; }

	float infectionTransitionDuration = 2.0f;
	float infectionTransitionTimer = 0.0f;
	bool infectionTransitionCompleted = false;

	// Set infect over time flag.
	public void Infect()
	{
		Infected = true;
	}
	
	// Immediately infect.
	public void InfectImmediate()
	{
		Infected = true;
		infectionTransitionCompleted = true;
		renderer.material = scrNodeMaster.Instance.MatCubeInfected;
	}

	public void Reset()
	{
		transform.rotation = Quaternion.identity;
		
		Infected = false;
		infectionTransitionCompleted = false;
		infectionTransitionTimer = 0.0f;
		renderer.material = scrNodeMaster.Instance.MatCubeUninfected;
	}

	// Use this for initialization
	void Start ()
	{
		Reset ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Infected)
		{
			if (!infectionTransitionCompleted)
			{
				infectionTransitionTimer += Time.deltaTime;
				if (infectionTransitionTimer >= infectionTransitionDuration)
				{
					infectionTransitionCompleted = true;
					renderer.material = scrNodeMaster.Instance.MatCubeInfected;
				}
				else
				{
					// Interpolate between the colours of the materials with a unique material.
					float transition = infectionTransitionTimer / infectionTransitionDuration;
					renderer.material.color = Color.Lerp(scrNodeMaster.ColCubeUninfected[0], scrNodeMaster.ColCubeInfected[0], transition);
					renderer.material.SetColor("_GlowColor", Color.Lerp(scrNodeMaster.ColCubeUninfected[1], scrNodeMaster.ColCubeInfected[1], transition));
					renderer.material.SetFloat("_Shininess", transition);
				}
			}
		}
	}
}
