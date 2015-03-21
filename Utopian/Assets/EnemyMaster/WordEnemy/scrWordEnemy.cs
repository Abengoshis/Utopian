using UnityEngine;
using System.Collections;

public class scrWordEnemy : scrEnemy
{
	const float BEHAVIOUR_UPDATE_DELAY_STANDARD = 2.0f;
	float behaviourUpdateDelay = 0.0f;
	float behaviourUpdateTimer = 0.0f;

	Vector2 currentDirection = Vector2.zero;
	bool canCollideWithCubes = false;

	public override void Init()
	{
		Name = Name.ToLower();
		GetComponentInChildren<TextMesh>().text = Name;

		// Stretch depending on the length of the word.
		if (Name.Length > 1)
		{
			Transform wedgeFront = transform.Find ("Ship").Find ("WedgeFront");
			Transform wedgeBack = transform.Find ("Ship").Find ("WedgeBack");

			switch (Name.Length)
			{
			case 2:
				wedgeFront.localScale = wedgeBack.localScale = new Vector3(2.75f, 1.0f, 1.0f);
				break;
			case 3:
				wedgeFront.localScale = wedgeBack.localScale = new Vector3(4.3f, 1.0f, 1.0f);
				break;
			case 4:
				wedgeFront.localScale = wedgeBack.localScale = new Vector3(5.75f, 1.0f, 1.0f);
				break;
			}
			
			GetComponent<BoxCollider2D>().bounds.Expand(new Vector3(0.0f, wedgeFront.localScale.x, 0.0f));
		}

		Invoke("AllowCollision", 1.2f);
	}

	protected override void Start ()
	{
		base.Start ();
	}
	
	// Update is called once per frame
	protected override void Update ()
	{		
		base.Update();

		behaviourUpdateTimer += Time.deltaTime;
		if (behaviourUpdateTimer >= behaviourUpdateDelay)
		{
			behaviourUpdateTimer = 0.0f;
			behaviourUpdateDelay = BEHAVIOUR_UPDATE_DELAY_STANDARD + Random.Range(-0.5f, 0.2f);
			
			// Get direction to player.
			Vector2 differenceToPlayer = scrPlayer.Instance.transform.position - transform.position;
			float distanceToPlayer = differenceToPlayer.magnitude;

			// Whether to shoot at the player.
			if (distanceToPlayer < 300.0f)
			{
				Vector2 directionToPlayer = differenceToPlayer.normalized;
				float dot = Vector2.Dot ((Vector2)transform.right, directionToPlayer);
				if (Mathf.Abs (dot) > 0.5f)
				{
					Vector3 gunSource = Vector3.zero;
					
					// Check whether the player is in front or behind.
					if (dot > 0)	// In front.
					{
						gunSource.x = 0.9f;
					}
					else // Behind.
					{
						gunSource.x = -0.9f;
					}

					StartCoroutine(ShootBarrage(gunSource));

				}

				currentDirection = GetInterceptDirection(transform.position, scrPlayer.Instance.rigidbody2D, Speed);
			}
			else
			{
				// Set direction randomly.
				float angle = Random.Range (0, 360);
				currentDirection = new Vector2(Mathf.Sin (angle), Mathf.Cos (angle));
			}
		}

		rigidbody2D.AddForce(currentDirection * Speed);
		if (rigidbody2D.velocity.magnitude > Speed)
			rigidbody2D.velocity = rigidbody2D.velocity.normalized * Speed;

		transform.right = (Vector3)rigidbody2D.velocity;

	}

	IEnumerator ShootBarrage(Vector3 source)
	{
		for (int i = 0; i < Random.Range (4, 8); ++i)
		{
			Vector3 worldSource = transform.TransformPoint(source);
			Vector3 shootDirection = GetInterceptDirection(worldSource, scrPlayer.Instance.rigidbody2D, 120);
			scrEnemyMaster.BulletPool.Create (new BulletPowerup(scrNodeMaster.ColCoreInfected, 120, 1, 0, 0, 0, false, false), worldSource, (scrPlayer.Instance.transform.position - worldSource).normalized, true, true);
			audio.PlayOneShot(FireSound);

			yield return new WaitForSeconds(0.1f);
		}
	}

	void AllowCollision()
	{
		foreach (Collider2D c in GetComponents<Collider2D>())
			c.enabled = true;
	}
}
