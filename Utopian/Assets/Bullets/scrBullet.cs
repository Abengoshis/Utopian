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

	public Vector2 Direction;
	public float Speed;
	public float Damage;

	delegate void method();
	method updateBehaviour;
	method fixedUpdateBehaviour;

	// Use this for initialization
	void Start ()
	{
		gameObject.SetActive(false);	
	}

	// Update is called once per frame
	void Update ()
	{
		Visible = !Expired && GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), collider2D.bounds);

		transform.right = Direction;

		if (ExpireWhenNotVisible && !Visible)	
			Expired = true;

		if (updateBehaviour != null)
			updateBehaviour();
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

		Expired = true;
	}

	public void Init(BehaviourType type, Vector3 position, Vector2 direction, float speed, float damage, bool expireWhenNotVisible, bool infecter)
	{
		Expired = false;
		ExpireWhenNotVisible = expireWhenNotVisible;
		Infecter = infecter;
		transform.position = position;
		Direction = direction;
		transform.forward = direction;
		Speed = speed;
		Damage = damage;

		switch (type)
		{
		case BehaviourType.STANDARD:
			StandardStart();
			updateBehaviour = StandardUpdate;
			fixedUpdateBehaviour = StandardFixedUpdate;
			break;
		case BehaviourType.HOMING:
			HomingStart();
			updateBehaviour = HomingUpdate;
			fixedUpdateBehaviour = HomingFixedUpdate;
			break;
		}
	}

	#region Standard
	void StandardStart()
	{
		rigidbody2D.velocity = Direction * Speed;
	}

	void StandardUpdate()
	{

	}

	void StandardFixedUpdate()
	{

	}
	#endregion

	#region Homing
	void HomingStart()
	{
		rigidbody2D.velocity = Direction * Speed;
	}
	
	void HomingUpdate()
	{
		transform.right = (Vector3)rigidbody2D.velocity;

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
				if (Vector2.Angle ((Vector2)transform.right, direction) < 22.5f)
				{
					bestDistance = distance;
					bestCollider = c;
				}
			}
		}

		if (bestCollider != null)
		{
			Direction = (Vector2)(bestCollider.transform.position - transform.position);
			Debug.Log (Direction);
		}
	}
	
	void HomingFixedUpdate()
	{
		rigidbody2D.AddForce(Direction * Speed * Time.fixedDeltaTime * 10);
		if (rigidbody2D.velocity.magnitude > Speed)
			rigidbody2D.velocity = rigidbody2D.velocity.normalized * Speed;
	}
	#endregion
}
