using UnityEngine;
using System.Collections;

public class scrSnakeEnemy : MonoBehaviour
{
	public string Word;
	public GameObject SegmentPrefab;

	public GameObject[] Segments { get; private set; }

	private Vector2[] nextPositions;
	private Vector2[] prevPositions;
	private float spacingTimer = 0.0f;
	private float spacingDelay = 0.1f;

	private Vector2 prevHeadPosition;

	// Use this for initialization
	void Start ()
	{
		// Create a segment for each character, which will trail after the head.
		Segments = new GameObject[Word.Length];
		prevPositions = new Vector2[Word.Length];
		nextPositions = new Vector2[Word.Length];
		for (int i = 0; i < Word.Length; ++i)
		{
			Segments[i] = (GameObject)Instantiate(SegmentPrefab, transform.position, Quaternion.identity);
			prevPositions[i] = transform.position;
			nextPositions[i] = transform.position;
			Segments[i].transform.Find ("Text").GetComponent<TextMesh>().text = Word[i].ToString();
		}
		prevHeadPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		//if (!Pathing)
		{
			//transform.position += (scrPlayer.Instance.transform.position - transform.position).normalized * Speed * Time.deltaTime;
		}

		// Only shift segments if the head is moving.
		if ((Vector2)transform.position - prevHeadPosition != Vector2.zero)
			spacingTimer += Time.deltaTime;
		prevHeadPosition = transform.position;

		if (spacingTimer >= spacingDelay)
		{
			spacingTimer = 0;
			prevPositions[0] = nextPositions[0];
			nextPositions[0] = transform.position;
			for (int i = 1; i < Segments.Length; ++i)
			{
				prevPositions[i] = nextPositions[i];
				nextPositions[i] = prevPositions[i - 1];
			}
		}

		for (int i = 0; i < Segments.Length; ++i)
		{
			Segments[i].transform.position = Vector2.Lerp (prevPositions[i], nextPositions[i], spacingTimer / spacingDelay);
		}
	}
}
