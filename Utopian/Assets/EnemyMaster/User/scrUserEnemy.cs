using UnityEngine;
using System.Collections;

public class scrUserEnemy : scrEnemy
{
	public scrNode SiegeNode { get; private set; }
	private int targetCubeIndex = 0;
	private float fireRate = 5;
	private float fireTimer = 0;

	private scrPathfinder pathfinder;
	
	public override void Init()
	{
		GetComponentInChildren<TextMesh>().text = Name;

		SiegeNode = null;

		pathfinder = GetComponent<scrPathfinder>();
		pathfinder.Target = scrPlayer.Instance.gameObject;

		DamageToDestroy = 10;
		DestroyOnExpire = true;
	}

	// Update is called once per frame
	protected override void Update ()
	{
		pathfinder.Resume();

		// If sieging, shoot at the node.
		if (SiegeNode != null && SiegeNode.Cubes != null)
		{
			if (!SiegeNode.gameObject.activeSelf || SiegeNode.Infected)
			{
				SiegeNode = null;
				pathfinder.Target = scrPlayer.Instance.gameObject;
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
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x)), 60 * Time.deltaTime);
			}

			fireTimer += Time.deltaTime * fireRate;
			if (fireTimer >= 1)
			{
				fireTimer = 0;

				scrEnemyMaster.BulletPool.Create (scrBullet.BehaviourType.STANDARD, transform.position, transform.right, 30, 1);
			}
		}
		// If not sieging, check for a node to siege.
		else
		{
			// Aim towards the player.
			Vector2 direction = scrPlayer.Instance.transform.position - transform.position;
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x)), 60 * Time.deltaTime);

			// Get nodes that are close by.
			Collider[] nearNodes = Physics.OverlapSphere(transform.position, scrNodeMaster.CELL_SIZE, 1 << LayerMask.NameToLayer("Node"));

			// Get a near node that is clean.
			scrNode clean = null;
			foreach (Collider c in nearNodes)
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
}
