using UnityEngine;
using System.Collections;

public class scrBullet : MonoBehaviour
{
	public enum BehaviourType
	{
		STANDARD
	}

	public bool Expired { get; private set; }

	public Vector3 Direction;
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
		transform.right = Direction;

		if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), collider.bounds))	
			Expired = true;

		if (updateBehaviour != null)
			updateBehaviour();
	}

	void FixedUpdate()
	{
		if (fixedUpdateBehaviour != null)
			fixedUpdateBehaviour();
	}

	void OnCollisionEnter(Collision c)
	{
		Expired = true;
	}

	public void Init(BehaviourType type, Vector3 position, Vector3 direction, float speed, float damage = 1)
	{
		Expired = false;
		transform.position = position;
		Direction = direction;
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
		rigidbody.velocity = Direction * Speed;
	}

	void StandardUpdate()
	{

	}

	void StandardFixedUpdate()
	{

	}
	#endregion
}
