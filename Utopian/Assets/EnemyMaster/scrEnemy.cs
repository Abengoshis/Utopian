using UnityEngine;
using System.Collections;

public class scrEnemy : MonoBehaviour
{
	public bool Expired { get; protected set; }

	public string Name;
	public float Speed = 10;
	public GameObject ExplosionPrefab;
	public Color ExplosionColour;

	public bool DestroyOnExpire = false;
	public float DamageToDestroy = 1;
	protected float damageTimer = 0;

	private Renderer[] renderers;
	private Material[] materials;

	public virtual void Init()
	{

	}

	// Use this for initialization
	protected virtual void Start ()
	{
		renderers = GetComponentsInChildren<Renderer>(false);
		materials = new Material[renderers.Length];
		for (int i = 0; i < renderers.Length; ++i)
			materials[i] = new Material(renderers[i].material);

		Expired = false;
	}

	public void DestroyImmediate()
	{
		damageTimer = DamageToDestroy;
	}
	
	// Update is called once per frame
	protected virtual void Update ()
	{
		if (Expired)
			return;

		if (damageTimer >= DamageToDestroy)
		{
			if (ExplosionPrefab != null)
			{
				GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
				explosion.particleSystem.startColor = ExplosionColour;
			}

			Expired = true;

			if (DestroyOnExpire)
				Destroy (gameObject);
		}
		else
		{
			if (damageTimer < 0)
			{
				damageTimer = 0;
				
				for(int i = 0; i < renderers.Length; ++i)
					renderers[i].material = materials[i];
			}
			else if (damageTimer > 0)
			{
				damageTimer -= 2 * Time.deltaTime;

				float t = damageTimer / DamageToDestroy;
				for(int i = 0; i < renderers.Length; ++i)
				{
					if (renderers[i].material.HasProperty("_Color"))
						renderers[i].material.color = Color.Lerp (materials[i].color, Color.white, t);
				}

			}
		}
	}

	void OnCollisionEnter2D(Collision2D c)
	{
		if (c.gameObject.layer == LayerMask.NameToLayer("PBullet"))
		{
			scrBullet bullet = c.gameObject.GetComponent<scrBullet>();
			damageTimer += bullet.Damage;
		}
	}
}
