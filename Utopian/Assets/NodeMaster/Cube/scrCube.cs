using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrCube : MonoBehaviour
{
	public enum DataState
	{
		BLOCKED = 0,
		CLEAN = 1,
		INFECTED = 2
	}


	public LinkedListNode<GameObject> Cube { get; private set; }
	public DataState StatePrev { get; private set; }
	public DataState State { get; private set; }
	public bool Uploading { get; private set; }
	public GameObject ExplosionPrefab;
	public scrNode Parent;

	float infectionTransitionDuration = 2.0f;
	float infectionTransitionTimer = 0.0f;
	bool infectionTransitionCompleted = false;

	float damageTimer = 0;
	float damageToDestroy = 1.1f;

	scrPathfinder pathfinder;

	// Set infect over time flag.
	public void Infect()
	{
		StatePrev = State;
		State = DataState.INFECTED;
		if (Parent != null)
		{
			++Parent.InfectedCubeCount;
			Parent.Infect (0);			// Really hacky, will need to reprogram.
		}
	}
	
	// Immediately infect.
	public void InfectImmediate()
	{
		State = DataState.INFECTED;
		infectionTransitionCompleted = true;
		renderer.material = scrNodeMaster.Instance.MatCubeInfected;
		if (Parent != null)
		{
			++Parent.InfectedCubeCount;
			Parent.Infect (0);			// Really hacky, will need to reprogram.
		}
	}

	public void DestroyImmediate()
	{
		damageTimer = damageToDestroy;
	}

	public void Init(LinkedListNode<GameObject> cube, scrNode parent, Vector3 position, DataState state)
	{
		transform.position = position;
		transform.rotation = Quaternion.identity;
		Parent = parent;
		Cube = cube;

		StatePrev = state;
		State = state;
		if (State == DataState.INFECTED)
		{
			infectionTransitionCompleted = true;
			renderer.material = scrNodeMaster.Instance.MatCubeInfected;
		}
		else
		{
			infectionTransitionCompleted = false;
			infectionTransitionTimer = 0.0f;

			if (State == DataState.BLOCKED)
				renderer.material = scrNodeMaster.Instance.MatCubeBlocked;
			else
				renderer.material = scrNodeMaster.Instance.MatCubeUninfected;
		}

		damageTimer = 0;
		Uploading = false;
		pathfinder.Pause ();
	}

	public void Upload()
	{
		Uploading = true;
		Parent = null;
		pathfinder.Resume();
	}

	// Use this for initialization
	void Start ()
	{
		pathfinder = GetComponent<scrPathfinder>();
		pathfinder.Target = GameObject.Find ("AICore");
		Init (null, null, Vector3.zero, DataState.BLOCKED);
	}

	// Update is called once per frame
	void Update ()
	{
		if (Uploading)
		{
			// While uploading (pathing towards the core) check if within the core.
			if (transform.position.x < 10 && transform.position.x > -10 && transform.position.y < 10 && transform.position.y > -10)
			{
				scrAICore.Instance.Learn (State == DataState.INFECTED);

				scrNodeMaster.Instance.DeactivateCube(Cube);
				Uploading = false;
				return;
			}

			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.identity, Time.deltaTime * 100);
		}

		if (State == DataState.INFECTED && !infectionTransitionCompleted)
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
				renderer.material.SetColor("_GlowColor", Color.Lerp(StatePrev == DataState.BLOCKED ? scrNodeMaster.ColCubeBlocked : scrNodeMaster.ColCubeUninfected, scrNodeMaster.ColCubeInfected, transition));
			}
		}

		if (damageTimer >= damageToDestroy)
		{

			if (Parent == null)
			{
				GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
				explosion.particleSystem.startColor = Color.Lerp (renderer.material.GetColor("_GlowColor"), Color.white, 0.5f);

				scrNodeMaster.Instance.DeactivateCube(Cube);
			}
			else
			{
				if (Parent.VisibleToPlayer)
				{
					GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
					explosion.particleSystem.startColor = Color.Lerp (renderer.material.GetColor("_GlowColor"), Color.white, 0.5f);
				}

				Parent.RemoveCube(Cube);
			}
		}
		else
		{
			if (damageTimer < 0)
			{
				damageTimer = 0;

				switch (State)
				{
				case DataState.BLOCKED:
					renderer.material = scrNodeMaster.Instance.MatCubeBlocked;
					break;
				case DataState.CLEAN:
					renderer.material = scrNodeMaster.Instance.MatCubeUninfected;
					break;
				case DataState.INFECTED:
					renderer.material = scrNodeMaster.Instance.MatCubeInfected;
					break;
				}
			}
			else if (damageTimer > 0)
			{
				damageTimer -= 2 * Time.deltaTime;
				renderer.material.color = Color.Lerp(Color.black, Color.white, damageTimer / damageToDestroy);
			}
		}
	}

	void OnTriggerEnter2D(Collider2D c)
	{
		if (c.gameObject.layer == LayerMask.NameToLayer("PBullet") || c.gameObject.layer == LayerMask.NameToLayer("EBullet"))
		{
			scrBullet bullet = c.gameObject.GetComponent<scrBullet>();
			if (bullet.Infecter && State != DataState.INFECTED)
			{
				Infect ();
			}
			else
			{
				damageTimer += bullet.Damage;
			}

		}
	}
}
