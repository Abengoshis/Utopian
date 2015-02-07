using UnityEngine;
using System.Collections;

public class scrPathfinder : MonoBehaviour
{
	public float Speed = 1;
	public Vector2 GridPosition = new Vector2(-1, -1);
	public Vector2 NextGridPosition = new Vector2(-1, -1);
	
	private float movement = 0;
	private bool prevVertical = false;

	private bool justStarted = true;
	public bool Pathing { get; private set; }
	public GameObject Target;

	// Use this for initialization
	void Start ()
	{
		Pause ();
	}

	public void Resume()
	{
		if (!Pathing)
		{
			Pathing = true;
			justStarted = true;
		}
	}

	public void Pause()
	{
		Pathing = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!Pathing)
			return;

		float movementTarget;

		if (justStarted)
		{
			justStarted = false;
			GridPosition = ((Vector2)transform.position + Vector2.one * scrNodeMaster.GRID_SIZE * scrNodeMaster.CELL_SIZE * 0.5f) / scrNodeMaster.CELL_SIZE;
			Vector2 snapPosition = scrNodeMaster.ToCellSpace(transform.position);

			if (gameObject.layer != LayerMask.NameToLayer("Cube") && !scrNodeMaster.FreeCells[(int)snapPosition.x, (int)snapPosition.y])
			{
				NextGridPosition = snapPosition;
			}
			else
			{
				// Get the grid position of the centre of the current cell.
				NextGridPosition = new Vector2(snapPosition.x + 0.5f, snapPosition.y + 0.5f);

				// Shift the grid position towards the player.
				NextGridPosition += (Vector2)(Target.transform.position - transform.position).normalized * 0.01f;

				// Round the grid positions towards the next best vertex.
				NextGridPosition = new Vector2(Mathf.Round (NextGridPosition.x), Mathf.Round(NextGridPosition.y));
			}

			movement = 0;
			movementTarget = Vector2.Distance(transform.position, scrNodeMaster.ToWorldSpace(NextGridPosition));
			Pathing = true;
		}
		else
		{
			movementTarget = scrNodeMaster.CELL_SIZE;
		}

		movement += Speed * Time.deltaTime;

		// Check if reached the next position.
		if (movement > movementTarget)
		{
			// Prepare for the next grid movement.
			movement = 0;
			GridPosition = NextGridPosition;
		
			// Check vertical and horizontal with the player.
			bool horizontal = transform.position.x != Target.transform.position.x;
			bool vertical = transform.position.y != Target.transform.position.y;

			// If both vertical and horizontal movement will be needed to get to the player, choose one or the other.
			if (vertical && horizontal)
			{
				if (prevVertical)
					vertical = false;
				else
					horizontal = false;
			}

			// Set the next grid position to the closest grid position towards the player.
			if (horizontal)
			{
				prevVertical = false;
				if (transform.position.x < Target.transform.position.x)
					NextGridPosition += new Vector2(1, 0);
				else
					NextGridPosition -= new Vector2(1, 0);
			}
			else if (vertical)
			{
				prevVertical = true;
				if (transform.position.y < Target.transform.position.y)
					NextGridPosition += new Vector2(0, 1);
				else
					NextGridPosition -= new Vector2(0, 1);
			}
		}

		if (movementTarget != 0)
			transform.position = Vector2.Lerp (scrNodeMaster.ToWorldSpace(GridPosition), scrNodeMaster.ToWorldSpace(NextGridPosition), movement / movementTarget);
	}
}
