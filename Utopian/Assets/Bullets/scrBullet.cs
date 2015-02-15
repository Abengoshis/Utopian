using UnityEngine;
using System.Collections;

public class scrBullet : MonoBehaviour
{
	public enum BehaviourType
	{
		STANDARD
	}

	public GameObject ExplosionPrefab;

	public bool Visible { get; private set; }
	public bool Expired { get; private set; }
	public bool ExpireWhenNotVisible = false;

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

	public void Init(BehaviourType type, Vector3 position, Vector2 direction, float speed, float damage, bool expireWhenNotVisible)
	{
		Expired = false;
		ExpireWhenNotVisible = expireWhenNotVisible;
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
}
