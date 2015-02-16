using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrBulletPool : MonoBehaviour
{
	public int Capacity;
	public GameObject BulletPrefab;

	private GameObject[] bullets;
	private int[] links;	// Indexer to bullet array elements.
	private int available;

	// Use this for initialization
	void Start ()
	{
		bullets = new GameObject[Capacity];
		links = new int[Capacity];
		for (int i = 0; i < Capacity; ++i)
		{
			bullets[i] = (GameObject)Instantiate (BulletPrefab);
			links[i] = i + 1;
		}

		available = 0;
	}
	
	// Update is called once per frame
	void Update ()
	{
		// Loop through the pool and check for expiration.
		for (int i = 0; i < Capacity; ++i)
		{
			// If the bullet has expired, make it inactive and available.
			if (bullets[i].activeSelf && bullets[i].GetComponent<scrBullet>().Expired)
			{
				links[i] = available;
				available = i;
				bullets[i].SetActive(false);
			}
		}
	}

	public void Create(scrBullet.BehaviourType bulletType, Vector3 position, Vector3 direction, float speed, float damage, bool expireWhenNotVisible, bool infecter = false)
	{
		// Check whether there are any bullets left.
		if (available != Capacity)
		{
			// Get the bullet.
			GameObject bullet = bullets[available];

			// Set the available bullet to the bullet linked to this one.
			available = links[available];

			// Activate the bullet.
			bullet.SetActive(true);

			// Get the bullet script.
			scrBullet bulletScript = bullet.GetComponent<scrBullet>();
			bulletScript.Init (bulletType, position, direction, speed, damage, expireWhenNotVisible, infecter);
		}
	}
}
