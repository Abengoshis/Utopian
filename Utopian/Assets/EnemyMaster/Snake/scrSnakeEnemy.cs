using UnityEngine;
using System.Collections;

public class scrSnakeEnemy : scrEnemy
{
	public GameObject SegmentPrefab;

	public scrEnemy[] Segments { get; private set; }

	private Vector2[] nextPositions;
	private Vector2[] prevPositions;
	private float spacingTimer = 0.0f;
	private float spacingDelay = 0.1f;
	private Vector2 prevHeadPosition;

	private scrPathfinder pathfinder;

	private float fireRate = 10;
	private float fireTimer = 0;

	public override void Init()
	{
		// Create a segment for each character, which will trail after the head.
		Segments = new scrEnemy[Name.Length];
		prevPositions = new Vector2[Name.Length];
		nextPositions = new Vector2[Name.Length];
		for (int i = 0; i < Name.Length; ++i)
		{
			Segments[i] = ((GameObject)Instantiate(SegmentPrefab, transform.position, Quaternion.identity)).GetComponent<scrEnemy>();
			Segments[i].GetComponentInChildren<TextMesh>().text = Name[i].ToString();
			Segments[i].Name += "[" + Name[i].ToString() + "]";
			Segments[i].DamageToDestroy = 3;
			prevPositions[i] = transform.position;
			nextPositions[i] = transform.position;
		}
		prevHeadPosition = transform.position;
		pathfinder = GetComponent<scrPathfinder>();
		pathfinder.Target = scrPlayer.Instance.gameObject;
		
		DamageToDestroy = 30;
		DestroyOnExpire = false;
	}
	
	// Update is called once per frame
	protected override void Update ()
	{
		if (Expired)
		{
			// Destroy segments.
			if (ExplosionPrefab != null)
			{
				for (int i = 0; i < Segments.Length; ++i)
				{
					GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, Segments[i].transform.position, Quaternion.identity);
					explosion.particleSystem.startColor = Color.Lerp (Segments[i].renderer.material.color, Color.white, 0.5f);
					Destroy (Segments[i].gameObject);
				}
			}

			// NOW destroy self.
			Destroy (gameObject);
			return;
		}

		if (Vector2.Distance(transform.position, scrPlayer.Instance.transform.position) < 50 && !Physics.Linecast(transform.position, scrPlayer.Instance.transform.position, 1 << LayerMask.NameToLayer("Node")))
		{
			pathfinder.Pause();
			Vector3 direction = Vector3.Lerp ((transform.position - (Vector3)prevPositions[0]).normalized, (scrPlayer.Instance.transform.position - transform.position).normalized, 0.3f).normalized * Time.deltaTime * Speed;
			transform.position += direction;
		
			fireTimer += Time.deltaTime * fireRate;
			if (fireTimer >= 1)
			{
				fireTimer = 0;
				
				scrEnemyMaster.BulletPool.Create (new BulletPowerup(scrNodeMaster.ColCoreInfected, 80, 1, 0, 0, 0, false, false), transform.position, direction.normalized, false, true);
				audio.PlayOneShot(FireSound);
			}
		}
		else
		{
			pathfinder.Resume();
		}

		// Shift segments scaled by the velocity, which is worked out by the translation of the head since the last update. Deltatime is not needed because the translation is already dependant on it..
		spacingTimer += 0.05f * Vector2.Distance((Vector2)transform.position, prevHeadPosition);
		prevHeadPosition = transform.position;

		if (spacingTimer >= spacingDelay)
		{
			spacingTimer = 0;
			prevPositions[0] = nextPositions[0];
			nextPositions[0] = transform.position;
			for (int i = 1; i < Segments.Length; ++i)
			{
				prevPositions[i] = nextPositions[i];
				nextPositions[i] = prevPositions[i - 1];
			}
		}

		bool allExpired = true;
		for (int i = 0; i < Segments.Length; ++i)
		{
			if (Segments[i] == null)
				continue;

			// If the segment has expired, disable its enemy script, hide its children and show its "dead" skeleton child.
			if (Segments[i].Expired)
			{
				if (Segments[i].enabled)
				{
					Segments[i].renderer.material.color = Color.black;
					Segments[i].transform.localScale *= 0.5f;
					Segments[i].collider2D.enabled = false;
					Segments[i].enabled = false;
					Segments[i].GetComponentInChildren<TextMesh>().text = "";
				}
			}
			else
			{
				allExpired = false;
			}

			Vector2 direction = nextPositions[i] - prevPositions[i];
			Segments[i].transform.rotation = Quaternion.RotateTowards(Segments[i].transform.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2 (direction.y, direction.x)), 180 * Time.deltaTime);
			Segments[i].transform.position = Vector2.Lerp (prevPositions[i], nextPositions[i], spacingTimer / spacingDelay);
		}

		// If all segments are gone, destroy self.
		if (allExpired)
		{
			damageTimer = DamageToDestroy;
		}

		base.Update();
	}

	void OnGUI()
	{
		//for (int i = 0; i < Segments.Length; ++i)
		//	if (Segments[i].enabled)
				//scrGUI.Instance.DrawOutlinedText (Word[i].ToString(), Camera.main.WorldToViewportPoint(Segments[i].transform.position), Color.white, Color.black, 3);
	}
}
