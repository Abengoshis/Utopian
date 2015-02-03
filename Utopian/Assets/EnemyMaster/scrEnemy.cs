using UnityEngine;
using System.Collections;

public class scrEnemy : MonoBehaviour
{
	public float Speed = 1;
	public Vector2 GridPosition = new Vector2(-1, -1);
	public Vector2 NextGridPosition = new Vector2(-1, -1);
	
	private float movement = 0;
	private bool prevVertical = false;

	public bool Pathing { get; private set; }
	private float visionRange = scrNodeMaster.CELL_SIZE * 2;

	// Use this for initialization
	void Start ()
	{

	}
	
	// Update is called once per frame
	void Update ()
	{
		float movementTarget;

		// Check if there is line of sight to the player.
		if (Vector2.Distance(transform.position, scrPlayer.Instance.transform.position) <= visionRange &&
		    !Physics.Linecast(transform.position, scrPlayer.Instance.transform.position, 1 << LayerMask.NameToLayer("Node")))
		{
			// Perform normal behaviour.
			Pathing = false;
		}
		else
		{
			if (!Pathing)
			{
				GridPosition = ((Vector2)transform.position + Vector2.one * scrNodeMaster.GRID_SIZE * scrNodeMaster.CELL_SIZE * 0.5f) / scrNodeMaster.CELL_SIZE;
				Vector2 snapPosition = scrNodeMaster.ToCellSpace(transform.position);

				if (!scrNodeMaster.FreeCells[(int)snapPosition.x, (int)snapPosition.y])
				{
					NextGridPosition = snapPosition;
				}
				else
				{
					// Get the grid position of the centre of the current cell.
					NextGridPosition = new Vector2(snapPosition.x + 0.5f, snapPosition.y + 0.5f);

					// Shift the grid position towards the player.
					NextGridPosition += (Vector2)(scrPlayer.Instance.transform.position - transform.position).normalized * 0.01f;

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
				bool horizontal = transform.position.x != scrPlayer.Instance.transform.position.x;
				bool vertical = transform.position.y != scrPlayer.Instance.transform.position.y;

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
					if (transform.position.x < scrPlayer.Instance.transform.position.x)
						NextGridPosition += new Vector2(1, 0);
					else
						NextGridPosition -= new Vector2(1, 0);
				}
				else if (vertical)
				{
					prevVertical = true;
					if (transform.position.y < scrPlayer.Instance.transform.position.y)
						NextGridPosition += new Vector2(0, 1);
					else
						NextGridPosition -= new Vector2(0, 1);
				}
			}

			transform.position = Vector2.Lerp (scrNodeMaster.ToWorldSpace(GridPosition), scrNodeMaster.ToWorldSpace(NextGridPosition), movement / movementTarget);
		}

		CustomUpdate();
	}

	protected virtual void CustomUpdate()
	{

	}
}
