using UnityEngine;
using System.Collections;

public class scrCube : MonoBehaviour
{
	public bool Infected { get; private set; }
	public GameObject ExplosionPrefab;
	public scrNode Parent;

	float infectionTransitionDuration = 2.0f;
	float infectionTransitionTimer = 0.0f;
	bool infectionTransitionCompleted = false;

	float damageTimer = 0;
	float damageToDestroy = 3;

	public GameObject SparkChild { get; private set; }

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
		SparkChild.particleSystem.startColor = scrNodeMaster.ColCubeInfected;
	}

	public void Reset()
	{
		transform.rotation = Quaternion.identity;
		
		Infected = false;
		infectionTransitionCompleted = false;
		infectionTransitionTimer = 0.0f;
		renderer.material = scrNodeMaster.Instance.MatCubeUninfected;
		SparkChild.particleSystem.startColor = scrNodeMaster.ColCubeUninfected;
	}
	
	// Use this for initialization
	void Start ()
	{
		SparkChild = transform.Find ("Spark").gameObject;
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
					SparkChild.particleSystem.startColor = scrNodeMaster.ColCubeInfected;
				}
				else
				{
					// Interpolate between the colours of the materials with a unique material.
					float transition = infectionTransitionTimer / infectionTransitionDuration;
					renderer.material.SetColor("_GlowColor", Color.Lerp(scrNodeMaster.ColCubeUninfected, scrNodeMaster.ColCubeInfected, transition));
				}
			}
		}

		if (damageTimer > damageToDestroy)
		{
			GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
			explosion.particleSystem.startColor = Color.Lerp (renderer.material.GetColor("_GlowColor"), Color.white, 0.5f);
			Parent.RemoveCube(gameObject);
		}
		else
		{
			if (damageTimer < 0)
			{
				damageTimer = 0;

				if (Infected)
					renderer.material = scrNodeMaster.Instance.MatCubeInfected;
				else
					renderer.material = scrNodeMaster.Instance.MatCubeUninfected;
			}
			else if (damageTimer > 0)
			{
				damageTimer -= Time.deltaTime * damageToDestroy;
				renderer.material.color = Color.Lerp(Color.black, Color.white, damageTimer / damageToDestroy);
			}
		}
	}

	void OnCollisionEnter(Collision c)
	{
		if (c.gameObject.layer == LayerMask.NameToLayer("PBullet"))
		{
			scrBullet bullet = c.gameObject.GetComponent<scrBullet>();
			damageTimer += bullet.Damage;
			SparkChild.transform.forward = -bullet.Direction;
			SparkChild.particleSystem.Emit (10);
		}
	}
}
