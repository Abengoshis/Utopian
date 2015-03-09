using UnityEngine;
using System.Collections;

public class scrEnemy : MonoBehaviour
{
	public bool Expired { get; protected set; }

	public string Name;
	public float Speed = 10;
	public GameObject ExplosionPrefab;
	public Color ExplosionColour;
	public AudioClip FireSound;

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
					if (renderers[i].material.HasProperty("_Color") && materials[i].HasProperty("_Color"))
						renderers[i].material.color = Color.Lerp (materials[i].color, Color.white, t);
				}

			}
		}
	}

	protected virtual void OnTriggerEnter2D(Collider2D c)
	{
		if (c.gameObject.layer == LayerMask.NameToLayer("PBullet"))
		{
			scrBullet bullet = c.gameObject.GetComponent<scrBullet>();
			damageTimer += bullet.Damage;
		}
	}

	protected Vector3 GetInterceptDirection(Vector3 source, Rigidbody2D target, float bulletSpeed)
	{
		Vector3 pos = target.position;
		Vector3 vel = target.velocity;
		
		// Get direction.
		Vector3 direction = pos - source;
		
		// Get components of quadratic equation.
		float a = vel.x * vel.x + vel.y * vel.y + vel.z * vel.z - bulletSpeed * bulletSpeed;
		if (a == 0)
			a = 0.001f;
		float b = 2 * (vel.x * direction.x + vel.y * direction.y + vel.z * direction.z);
		float c = direction.x * direction.x + direction.y * direction.y + direction.z * direction.z;
		
		// Solve quadratic equation.
		bool solved = false;
		float q0 = 0.0f;
		float q1 = 0.0f;
		float eps = 0.00001f;
		if (Mathf.Abs(a) < eps)
		{
			if (Mathf.Abs(b) < eps)
			{
				if (Mathf.Abs(c) < eps)
				{
					solved = false;
				}
			}
			else
			{
				q0 = -c / b;
				q1 = -c / b;
				solved = false;
			}
		}
		else
		{
			float d = b * b - 4 * a * c;
			if (d >= 0)
			{
				d = Mathf.Sqrt(d);
				a = 2 * a;
				q0 = (-b + d) / a;
				q1 = (-b - d) / a;
				solved = true;
			}
		}
		
		// Find smallest positive solution.
		Vector3 solution = Vector2.zero;
		if (solved)
		{
			solved = false;
			float q = Mathf.Min(q0, q1);
			if (q < 0)
			{
				q = Mathf.Max(q0, q1);
			}
			if (q > 0)
			{
				solution = pos + vel * q;
				solved = true;
			}
		}
		
		if (!solved)
		{
			// Fallback equation.
			float t = direction.magnitude / bulletSpeed;
			solution = pos + vel * t;
		}
		
		return (solution - transform.position).normalized;
	}
	
}
