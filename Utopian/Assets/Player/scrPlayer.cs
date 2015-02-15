using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class scrPlayer : MonoBehaviour
{
	public static scrPlayer Instance { get; private set; }

	public float SimulatedCursorSensitivity = 10;
	public Vector2 SimulatedCursorPosition { get; private set; }
	Vector2 simulatedCursorOffset = Vector2.zero;
	float simulatedCursorRadius = 80.0f;

	// --
	float damageToDestroy = 3.0f;
	float damageTimer = 0;
	Renderer[] renderers;
	Color[] colours;

	// --
	float acceleration = 200.0f;
	float topSpeed = 200.0f;

	public Vector2 MoveDirection { get; private set; }
	public Vector2 AimDirection { get; private set; }

	// --
	public AudioClip FireSound;
	scrBulletPool bulletPool;
	float fireRate = 20;	// Shots per second.
	float fireTimer = 0;
	int fireMode = 0;

	public GameObject BombExplosionPrefab;
	public int Bombs { get; private set; }
	bool bombDeployed = false;

	Vector3[] gunOffsets = new Vector3[] { new Vector3(3, 0.85f), new Vector3(3, -0.85f) };

	void Start ()
	{
		Instance = this;
		SimulatedCursorPosition = transform.position;

		renderers = GetComponentsInChildren<Renderer>();
		colours = new Color[renderers.Length];
		for (int i = 0; i < renderers.Length; ++i)
		{
			if (renderers[i].material.HasProperty("_Color"))
				colours[i] = renderers[i].material.color;
			else
				colours[i] = renderers[i].material.GetColor("_TintColor");
		}

		MoveDirection = Vector2.zero;
		AimDirection = Vector2.zero;

		bulletPool = GetComponent<scrBulletPool>();
		Bombs = 3;

		Screen.lockCursor = true;

		// Disable. Reenabled by the master.
		enabled = false;
	}
	
	void Update ()
	{
		if (Input.GetKey(KeyCode.Y))
		{
			scrResults.TimeEnd = Time.time;
			Application.LoadLevel("Results");
		}

		if (fireTimer < 1)
			fireTimer += fireRate * Time.deltaTime;

		if (damageTimer >= damageToDestroy)
		{
			//GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
			//explosion.particleSystem.startColor = Color.Lerp (renderer.material.GetColor("_GlowColor"), Color.white, 0.5f);
			
			gameObject.SetActive(false);
		}
		else
		{
			damageTimer -= Time.deltaTime;

			float t = damageTimer / damageToDestroy;
			for(int i = 0; i < renderers.Length; ++i)
			{
				if (renderers[i].material.HasProperty("_Color"))
					renderers[i].material.color = Color.Lerp (colours[i], Color.white, t);
				else
					renderers[i].material.SetColor("_TintColor", Color.Lerp (colours[i], Color.white, t));
			}
		}

		ProcessInput ();
	}

	void FixedUpdate()
	{
		// Add acceleration in the target move direction.
		rigidbody2D.AddForce(MoveDirection * acceleration, ForceMode2D.Force);
		if (rigidbody2D.velocity.magnitude > topSpeed)
			rigidbody2D.velocity = rigidbody2D.velocity.normalized * topSpeed;
	}

	void ProcessInput()
	{
		simulatedCursorOffset += new Vector2(Input.GetAxis ("Mouse X") / Screen.width, Input.GetAxis ("Mouse Y") / Screen.height) * 1000 * SimulatedCursorSensitivity;
		if (simulatedCursorOffset.magnitude > simulatedCursorRadius)
			simulatedCursorOffset = simulatedCursorOffset.normalized * simulatedCursorRadius;
		SimulatedCursorPosition = (Vector2)transform.position + simulatedCursorOffset;

		// Move the camera half way between the player and simulated cursor.
		Vector2 half = Vector2.Lerp (transform.position, SimulatedCursorPosition, 0.5f);
		Camera.main.transform.position = new Vector3(half.x, half.y, Camera.main.transform.position.z);

		// Get the aim direction.
		Vector2 aim = SimulatedCursorPosition - (Vector2)transform.position;
		if (aim != Vector2.zero)
			aim.Normalize();
		AimDirection = aim;

		// Face the simulated mouse position.
		float roll = Mathf.Atan2 (aim.y, aim.x);
		transform.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * roll);

		// Get the movement direction.
		Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis ("Vertical"));
		if (move != Vector2.zero)
			move.Normalize();
		MoveDirection = move;

		// Check for shooting.		
		if (Input.GetButton("Fire"))
		{
			if (fireTimer >= 1)
			{
				if (fireMode == 0)
					bulletPool.Create(scrBullet.BehaviourType.STANDARD, transform.TransformPoint(gunOffsets[0]), transform.right, 80, 1, true);
				else
					bulletPool.Create(scrBullet.BehaviourType.STANDARD, transform.TransformPoint(gunOffsets[1]), transform.right, 80, 1, true);

				//audio.PlayOneShot(FireSound);

				fireMode = (fireMode == 0 ? 1 : 0);
				fireTimer = 0;
			}
		}

		if (Input.GetButtonDown ("Bomb"))
		{
			Instantiate(BombExplosionPrefab, transform.position, Quaternion.identity);;
			StartCoroutine(DeployBomb(transform.position));
		}
	}

	IEnumerator DeployBomb(Vector3 position)
	{
		bombDeployed = true;

		for (int i = 0; i < 4; ++i)
		{
			foreach (Collider2D c in Physics2D.OverlapCircleAll(position, (i + 1) / 4.0f * 60))
			{
				if (c.GetComponent<scrCube>() != null ||
				    c.GetComponent<scrEnemy>() != null)
					c.SendMessage("DestroyImmediate");
			}

			yield return new WaitForSeconds(0.3f);
		}

		bombDeployed = false;
	}

	void OnTriggerEnter2D(Collider2D c)
	{
		if (c.gameObject.layer == LayerMask.NameToLayer("EBullet"))
		{
			scrBullet bullet = c.gameObject.GetComponent<scrBullet>();
			damageTimer += bullet.Damage;
		}
	}

	void OnPostRender()
	{
		// Draw a line from the ship to the cursor?
	}
}
