using UnityEngine;
using System.Collections;

public class scrBullet : MonoBehaviour
{
	public enum BehaviourType
	{
		STANDARD,
		HOMING,
	}

	public GameObject ExplosionPrefab;

	public bool Visible { get; private set; }
	public bool Expired { get; private set; }
	public bool ExpireWhenNotVisible = false;
	public bool Infecter { get; private set; }
	public bool FromPlayer = false;	// Hax

	public Vector2 Direction;
	public float Speed;
	public float Damage;
	public float Wiggle;	// These things are hacky ways to implement powerups. I'll remake the entire game after research as I already have ideas for improving efficiency and gameplay.
	public float Erratic;
	public bool Penetrative;

	delegate void method();
	method updateBehaviour;
	method fixedUpdateBehaviour;

	private float life = 0;

	// Use this for initialization
	void Start ()
	{
		gameObject.SetActive(false);	
	}

	// Update is called once per frame
	void Update ()
	{
		Visible = !Expired && GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), collider2D.bounds);

		if (ExpireWhenNotVisible && !Visible)	
			Expired = true;

		if (updateBehaviour != null)
			updateBehaviour();

		life += Time.deltaTime;
	}

	void FixedUpdate()
	{
		if (fixedUpdateBehaviour != null)
			fixedUpdateBehaviour();
	}

	void OnTriggerEnter2D(Collider2D c)
	{
		if (Visible)
		{
			GameObject explosion = (GameObject)Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
			explosion.transform.forward = -Direction;
		}

		if (!Penetrative || c.transform.root.GetComponentInChildren<scrPlayer>() != null)
			Expired = true;
	}

	public void Init(BulletPowerup powerup, Vector3 position, Vector2 direction, bool expireWhenNotVisible, bool infecter)
	{
		life = 0;
		Expired = false;
		ExpireWhenNotVisible = expireWhenNotVisible;
		Infecter = infecter;
		transform.position = position;
		Direction = direction;
		transform.forward = direction;
		Speed = powerup.Speed;
		Damage = powerup.Size;
		Wiggle = powerup.Wiggle;
		Erratic = powerup.Erratic;
		Penetrative = powerup.Penetrative;

		if (Penetrative)
			transform.localScale = powerup.Size * new Vector3(1.5f, 0.3f, 0.3f);
		else
			transform.localScale = powerup.Size * Vector3.one;


		transform.Find ("Core").renderer.material.color = powerup.Colour;
		transform.Find ("Glow").renderer.material.SetColor("_TintColor", new Color(powerup.Colour.r * 0.5f, powerup.Colour.g * 0.5f, powerup.Colour.b * 0.5f, 0.0625f));

		if (!powerup.Homing)
		{
			StandardStart();
			updateBehaviour = StandardUpdate;
			fixedUpdateBehaviour = StandardFixedUpdate;
		}
		else
		{
			HomingStart();
			updateBehaviour = HomingUpdate;
			fixedUpdateBehaviour = HomingFixedUpdate;
		}
	}

	void ApplyWiggle()
	{
		float wig = Erratic * Speed * 0.01f * Mathf.Sin (life * Wiggle * 0.5f);
		transform.right = Direction + new Vector2(-Direction.y, Direction.x).normalized * wig;
	}

	#region Standard
	void StandardStart()
	{
		rigidbody2D.velocity = Direction * Speed;
	}

	void StandardUpdate()
	{
		ApplyWiggle();
	}

	void StandardFixedUpdate()
	{
		rigidbody2D.velocity = transform.right * Speed;
	}
	#endregion

	#region Homing
	void HomingStart()
	{
		rigidbody2D.velocity = Direction * Speed;
	}
	
	void HomingUpdate()
	{
		if (!FromPlayer)
		{
			Vector2 direction = scrPlayer.Instance.transform.position - transform.position;

			if (direction.magnitude < scrNodeMaster.CELL_SIZE && Vector2.Angle ((Vector2)transform.right, direction) < 22.5f)
				Direction = direction;
			else
				ApplyWiggle();

			return;
		}
		
		// Find the nearest target.
		float bestDistance = float.MaxValue;
		Collider2D bestCollider = null;
		foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, 100.0f, (1 << LayerMask.NameToLayer("Cube")) | (1 << LayerMask.NameToLayer("Enemy")) ))
		{
			Vector2 direction = (Vector2)c.transform.position - rigidbody2D.position;
			float distance = direction.magnitude;
			if (distance < bestDistance)
			{
				// Check if within an arc.
				if (Vector2.Angle ((Vector2)transform.right, direction) < 90.0f)
				{
					bestDistance = distance;
					bestCollider = c;
				}
			}
		}

		if (bestCollider != null)
		{
			Direction = (Vector2)(bestCollider.transform.position - transform.position);
			transform.right = Direction;
		}
		else
			transform.right = rigidbody2D.velocity;
	}
	
	void HomingFixedUpdate()
	{
		rigidbody2D.AddForce(transform.right * Speed * Time.fixedDeltaTime * 100);
		if (rigidbody2D.velocity.magnitude > Speed)
			rigidbody2D.velocity = rigidbody2D.velocity.normalized * Speed;

	}
	#endregion
}
