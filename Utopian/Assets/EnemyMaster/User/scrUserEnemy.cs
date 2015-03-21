using UnityEngine;
using System.Collections;

public class scrUserEnemy : scrEnemy
{
	public scrNode SiegeNode { get; private set; }
	private int targetCubeIndex = 0;
	private float fireRate = 0.5f;
	private float fireTimer = 0;
	private scrPathfinder pathfinder;
	public BulletPowerup powerup;

	private Color NameToColour(string name)
	{
		int angle = 0;
		for (int i = 0; i < name.Length; ++i)
			angle += name[i];
		angle %= 360;

		float x = 1 - Mathf.Abs((angle / 60.0f) % 2 - 1);

		// EXCLUDE COLOURS SIMILAR TO THE PLAYER!
		if (angle > 160 && angle < 220)
			angle -= 100;

		if (angle < 60)
			return new Color(1, x, 0.25f);

		if (angle < 120)
			return new Color(x, 1, 0.25f);

		if (angle < 180)
			return new Color(0.25f, 1, x);

		if (angle < 240)
			return new Color(0.25f, x, 1);

		if (angle < 300)
			return new Color(x, 0.25f, 1);

		return new Color(1, 0.25f, x);
	}

	public override void Init()
	{
		GetComponentInChildren<TextMesh>().text = Name;

		SiegeNode = null;

		pathfinder = GetComponent<scrPathfinder>();
		pathfinder.Target = scrPlayer.Instance.gameObject;

		powerup.Colour = NameToColour(Name);
		powerup.Size = 0.5f + (Mathf.Max(Name.Length, 16)) / 10.0f;
		powerup.Speed = 220.0f / powerup.Size;
		powerup.Wiggle = Name[0] < 80 ? Name[0] * 0.5f : 0;
		powerup.Erratic = (Mathf.Abs(Name[Name.Length - 1] - Name[0]) % 10) / 7.0f;
		powerup.Homing = Name[0].ToString().ToUpper()[0] < 'H';
		powerup.Penetrative = Name[Name.Length - 1] < '9';

		//powerup = new BulletPowerup(colour, speed, size, split, wiggle, homing, penetrative);

		Transform ship = transform.Find("Ship");
		ship.Find("Core_Core").renderer.material.color = powerup.Colour;
		ship.Find("Core_Glow").renderer.material.SetColor("_TintColor", new Color(powerup.Colour.r * 0.5f, powerup.Colour.g * 0.5f, powerup.Colour.b * 0.5f, 0.0625f));
		transform.GetComponentInChildren<TrailRenderer>().material.SetColor("_TintColor", powerup.Colour);
		ExplosionColour = powerup.Colour;
	}

	// Update is called once per frame
	protected override void Update ()
	{
		if (damageTimer >= DamageToDestroy)
		{
			scrPlayer.Instance.Powerups.Enqueue(powerup);
			scrGUI.Instance.EnqueuePowerup(Name, powerup.Colour);
		}
			
		pathfinder.Resume();

		// If sieging, shoot at the node.
		if (SiegeNode != null && SiegeNode.Cubes != null)
		{
			if (!SiegeNode.gameObject.activeSelf || SiegeNode.Infected)
			{
				SiegeNode = null;
				pathfinder.Target = scrPlayer.Instance.gameObject;
				base.Update();
				return;
			}

			if (targetCubeIndex >= SiegeNode.Cubes.Length)
				targetCubeIndex = 0;

			// Change cube if target cube is destroyed.
			if (SiegeNode.Cubes[targetCubeIndex] == null)
			{
				++targetCubeIndex;
			}
			else
			{
				Vector2 direction = SiegeNode.Cubes[targetCubeIndex].Value.transform.position - transform.position;
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x)), 180 * Time.deltaTime);
			
				// Check the distance.
				if (direction.magnitude < scrNodeMaster.CELL_SIZE)
				{
					fireTimer += Time.deltaTime * fireRate;
					if (fireTimer >= 1)
					{
						fireTimer = 0;
						
						StartCoroutine(Shoot(Random.Range (3, 8)));
					}
				}
			}
		}
		// If not sieging, check for a node to siege.
		else
		{
			// Aim towards the player.
			Vector2 direction = scrPlayer.Instance.transform.position - transform.position;
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x)), 180 * Time.deltaTime);

			if (direction.magnitude < scrNodeMaster.CELL_SIZE)
			{
				fireTimer += Time.deltaTime * fireRate;
				if (fireTimer >= 1)
				{
					fireTimer = 0;

					StartCoroutine(Shoot(Random.Range (3, 8)));
				}
			}

			// Get nodes that are close by.
			Collider2D[] nearNodes = Physics2D.OverlapCircleAll(transform.position, scrNodeMaster.CELL_SIZE, 1 << LayerMask.NameToLayer("Node"));

			// Get a near node that is clean.
			scrNode clean = null;
			foreach (Collider2D c in nearNodes)
			{
				scrNode n = c.GetComponent<scrNode>();
				if (!n.Infected && !n.Blocked)
				{											
					clean = n;
					break;
				}
			}

			// If the clean node was set, start sieging.
			if (clean != null)
			{
				SiegeNode = clean;
				pathfinder.Target = SiegeNode.gameObject;
			}
		}

		base.Update();
	}

	IEnumerator Shoot(int numTimes)
	{
		for (int i = 0; i < numTimes; ++i)
		{
			audio.PlayOneShot(FireSound);
			scrEnemyMaster.BulletPool.Create (powerup, transform.position, transform.right, false, false);
			yield return new WaitForSeconds(0.1f);
		}
	}
}