using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class scrPlayer : MonoBehaviour
{
	public static scrPlayer Instance { get; private set; }

	public Vector2 SimulatedCursorPosition { get; private set; }
	Vector2 simulatedCursorOffset = Vector2.zero;
	float simulatedCursorRadius = 25.0f;

	// --
	float acceleration = 60.0f;
	float topSpeed = 60.0f;
	bool  boosting = false;

	public Vector2 MoveDirection { get; private set; }
	public Vector2 AimDirection { get; private set; }

	void Start ()
	{
		Instance = this;
		SimulatedCursorPosition = transform.position;

		MoveDirection = Vector2.zero;
		AimDirection = Vector2.zero;

		Screen.lockCursor = true;

		// Disable. Reenabled by the master.
		enabled = false;
	}
	
	void Update ()
	{
		ProcessInput ();
	}

	void FixedUpdate()
	{
		// Add acceleration in the target move direction.
		rigidbody.AddForce(MoveDirection * acceleration, ForceMode.Acceleration);
		if (rigidbody.velocity.magnitude > topSpeed)
			rigidbody.velocity = rigidbody.velocity.normalized * topSpeed;
	}

	void ProcessInput()
	{
		simulatedCursorOffset += new Vector2(Input.GetAxis ("Mouse X") / Screen.width, Input.GetAxis ("Mouse Y") / Screen.height) * 700;
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
	}

	void OnPostRender()
	{
		// Draw a line from the ship to the cursor?
	}
}
