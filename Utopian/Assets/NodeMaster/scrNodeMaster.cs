﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrNodeMaster : MonoBehaviour
{
	public static scrNodeMaster Instance { get; private set; }

	const int LOOPS_PER_FRAME = 50;

	#region Pool Variables

	const int TOTAL_NODES = 80;
	const int TOTAL_CUBES = 16000;

	// These static pools will be loaded when the player plays their first game.
	static LinkedList<GameObject> nodePool;
	static bool nodePoolLoaded = false;
	static LinkedList<GameObject> cubePool;
	static bool cubePoolLoaded = false;
	static Vector3[] positions;
	static bool positionsLoaded = false;

	// These keep track of how many nodes are available.
	static int inactiveNodeCount = 0;
	static int inactiveCubeCount = 0;
	static int freePositionsCount = 0;
	
	public const int GRID_SIZE = 20;
	public const int CELL_SIZE = 20;

	#endregion

	Queue<Message> messageQueue = new Queue<Message>();
	bool creating = false;

	public GameObject NodePrefab;
	public GameObject CubePrefab;
	public Material MatCubeUninfected, MatCubeInfected;
	public Material MatCoreUninfected, MatCoreInfected;
	public Material MatLink;

	public static Color[] ColCubeUninfected { get; private set; }
	public static Color[] ColCubeInfected { get; private set; }
	public static Color ColCoreUninfected { get; private set; }
	public static Color ColCoreInfected { get; private set; }

	public GameObject ChildGrid { get; private set; }

	#region Pool Functions

	public IEnumerator LoadNodePool()
	{
		if (!nodePoolLoaded)
		{
			nodePool = new LinkedList<GameObject>();
			inactiveNodeCount = TOTAL_NODES;

			int numLoops = 0;
			bool newBatch = true;
			LinkedListNode<GameObject> deactivateStart = null;
			for (int i = 0; i < inactiveNodeCount; ++i)
			{
				nodePool.AddLast((GameObject)Instantiate (NodePrefab));

				if (newBatch)
				{
					deactivateStart = nodePool.Last;
					newBatch = false;
				}

				if (++numLoops == LOOPS_PER_FRAME * 5 || i == inactiveNodeCount - 1)
				{
					yield return new WaitForSeconds(0.5f);
					while (deactivateStart != null)
					{
						deactivateStart.Value.SetActive(false);
						deactivateStart = deactivateStart.Next;
					}
					numLoops = 0;
					newBatch = true;
				}
			}

			nodePoolLoaded = true;
		}
	}

	public IEnumerator LoadCubePool()
	{
		if (!cubePoolLoaded)
		{
			cubePool = new LinkedList<GameObject>();
			inactiveCubeCount = TOTAL_CUBES;

			int numLoops = 0;
			bool newBatch = true;
			LinkedListNode<GameObject> deactivateStart = null;
			for (int i = 0; i < inactiveCubeCount; ++i)
			{
				cubePool.AddLast ((GameObject)Instantiate (CubePrefab));

				if (newBatch)
				{
					deactivateStart = cubePool.Last;
					newBatch = false;
				}

				if (++numLoops == LOOPS_PER_FRAME * 5 || i == inactiveCubeCount - 1)
				{
					yield return new WaitForSeconds(0.5f);
					while (deactivateStart != null)
					{
						deactivateStart.Value.SetActive(false);
						deactivateStart = deactivateStart.Next;
					}
					numLoops = 0;
					newBatch = true;
				}
			}

			cubePoolLoaded = true;
		}
	}

	public static void PrecomputeNodePositions()
	{
		if (!positionsLoaded)
		{
			positions = new Vector3[GRID_SIZE * GRID_SIZE - 5];
			freePositionsCount = 0;
			
			int mid = GRID_SIZE / 2;
			for (int i = 0; i < GRID_SIZE; ++i)
			{
				for (int j = 0; j < GRID_SIZE; ++j)
				{
					if (!((i == mid || i == mid - 1 || i == mid + 1) && j == mid) &&
					    !((j == mid || j == mid - 1 || j == mid + 1) && i == mid))
					{
						positions[freePositionsCount] = new Vector3(i * CELL_SIZE - GRID_SIZE * CELL_SIZE * 0.5f + CELL_SIZE * 0.5f,
						                                            j * CELL_SIZE - GRID_SIZE * CELL_SIZE * 0.5f + CELL_SIZE * 0.5f);
						++freePositionsCount;
					}
				}
			}

			positionsLoaded = true;
		}
	}

	public void ClearPools()
	{
		inactiveNodeCount = nodePool.Count;
		foreach (GameObject node in nodePool)
			node.SetActive(false);
		
		inactiveCubeCount = cubePool.Count;
		foreach (GameObject cube in cubePool)
			cube.SetActive(false);
		
		freePositionsCount = positions.Length;
	}

	#endregion

	// Use this for initialization
	void Start ()
	{
		Instance = this;

		ChildGrid = transform.Find ("Grid").gameObject;
		ChildGrid.transform.localScale = new Vector3(GRID_SIZE * CELL_SIZE, GRID_SIZE * CELL_SIZE, 1);
		ChildGrid.renderer.material.SetInt("_GridSize", GRID_SIZE);
		ChildGrid.renderer.material.SetInt("_CellSize", CELL_SIZE);

		ColCubeUninfected = new Color[2];
		ColCubeInfected = new Color[2];
		ColCubeUninfected[0] = MatCubeUninfected.color;
		ColCubeUninfected[1] = MatCubeUninfected.GetColor ("_GlowColor");
		ColCubeInfected[0] = MatCubeInfected.color;
		ColCubeInfected[1] = MatCubeInfected.GetColor ("_GlowColor");
		ColCoreUninfected = MatCoreUninfected.color;
		ColCoreInfected = MatCoreInfected.color;

		// Disable. Reenabled by the master.
		enabled = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		// If not busy creating a node, check for nodes to make.
		if (!creating)
		{
			// Generate nodes as long as the queue contains messages.
			if (messageQueue.Count != 0)
			{
				// Get the message.
				Message message = messageQueue.Dequeue ();

				// Check the message for certain criteria to determine whether or not to make an infected or uninfected node.
				string summary = message.summary != null ? message.summary.ToUpper() : "";
				if (summary == "" || !(summary.Contains("REVERSION") || summary.Contains("VANDAL") || summary.Contains("SPAM")))
				{
					// If the user is not a bot, create the node.  An edit by a bot that is not a vandalism reversion is unlikely to contain any decent information.
					if (!message.is_bot)
						StartCoroutine(Create (message, false));
				}
				else
				{
					// Create an infected node.
					StartCoroutine(Create (message, false));
				}
			}
		}

		// Set the shader's player position uniform.
		ChildGrid.renderer.material.SetFloat("_PlayerX", scrPlayer.Instance.transform.position.x);
		ChildGrid.renderer.material.SetFloat("_PlayerY", scrPlayer.Instance.transform.position.y);


	}

	public void ReceiveMessage(Message message)
	{
		messageQueue.Enqueue(message);
	}

	public IEnumerator Create(Message message, bool infected)
	{
		creating = true;

		// Don't create a node if there are no nodes available.
		if (inactiveNodeCount == 0)
		{
			Debug.Log("There are no inactive nodes left to create a node for \"" +  message.page_title + "\".");
			creating = false;
			yield break;
		}

		// Don't create a node if there are no cubes available.
		if (inactiveCubeCount == 0)
		{
			Debug.Log("There are no inactive cubes left to create a node for \"" +  message.page_title + "\".");
			creating = false;
			yield break;
		}

		// Set the size of the core based on the change_size of the message.
		int coreSize = Mathf.Min (Mathf.CeilToInt(Mathf.Log10 (Mathf.Abs (message.change_size) + 2) * 3), scrNode.CORE_SIZE_MAX);

		// Get the number of cubes there would be around this core.
		int numCubes = scrNode.CubePositions[coreSize - 1].Length;

		// Don't create a node if there aren't enough cubes available.
		if (inactiveCubeCount < numCubes)
		{
			Debug.Log ("There are not enough inactive cubes (" + inactiveCubeCount + "/" + numCubes + ") in the pool to create a node for \"" + message.page_title + "\".");
			creating = false;
			yield break;
		}

		Debug.Log ("Creating node.");

		// All checks have passed - a node can be made.  Get the first inactive node in the node pool.
		LinkedListNode<GameObject> node = nodePool.First;

		// Activate, position and initialise the node.
		ActivateNode(node);
		node.Value.transform.position = GetRandomFreePosition();
		scrNode nodeScript = node.Value.GetComponent<scrNode>();
		nodeScript.Init(node, message, coreSize, infected);

		// Assign cubes to the node.
		LinkedListNode<GameObject> cube = cubePool.First;
		for (int i = 0, numLoops = 0; i < numCubes; ++i)
		{
			// Get the next cube before the cube is activated.
			LinkedListNode<GameObject> next = cube.Next;

			// Activate the cube and add it to the node.
			ActivateCube(cube);
			nodeScript.AddCube(cube);

			// Move to the next cube in the pool.
			cube = next;

			if (++numLoops == LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}

		// Query surrounding nodes for links.
		CreateLinks (node);

		// Release the node! Go! Go free!!
		nodeScript.MakeReady();

		creating = false;
	}

	public IEnumerator Destroy(LinkedListNode<GameObject> node)
	{
		scrNode nodeScript = node.Value.GetComponent<scrNode>();

		// Clear and reset the nodes cubes and make them available to future nodes.
		for (int i = 0, numLoops = 0; i < nodeScript.Cubes.Length; ++i)
		{
			LinkedListNode<GameObject> cube = nodeScript.Cubes[i];
			cube.Value.GetComponent<scrCube>().Reset ();
			DeactivateCube(cube);
			if (++numLoops == LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}

		DeactivateNode(node);
	}

	public void CreateLinks(LinkedListNode<GameObject> node)
	{
		scrNode nodeScript = node.Value.GetComponent<scrNode>();
		Bounds nodeBounds = new Bounds(node.Value.transform.position, new Vector3(CELL_SIZE * 2, CELL_SIZE * 2, CELL_SIZE * 2));	// Instead, could look for nodes at the adjacent positions of positions array.
		LinkedList<GameObject>.Enumerator activeNode = nodePool.GetEnumerator();
		for (int i = 0; i < inactiveNodeCount; ++i)
			activeNode.MoveNext();
		while (activeNode.MoveNext() && nodeScript.CurrentLinks != scrNode.LINKS_MAX)
		{
			// Don't link to fully infected nodes.
			if (nodeScript.FullyInfected ^ activeNode.Current.GetComponent<scrNode>().FullyInfected)
			{
				if (nodeBounds.Contains(activeNode.Current.transform.position))
				{
					// Determine which order to link the nodes.
					if (nodeScript.FullyInfected)
						nodeScript.Link (activeNode.Current);
					else
						activeNode.Current.GetComponent<scrNode>().Link (node.Value);
				}
			}
		}
	}

	void ActivateNode(LinkedListNode<GameObject> node)
	{
		node.Value.SetActive(true);
		nodePool.Remove (node);
		nodePool.AddLast(node);
		--inactiveNodeCount;
	}

	void DeactivateNode(LinkedListNode<GameObject> node)
	{
		node.Value.SetActive(false);
		nodePool.Remove (node);
		nodePool.AddFirst(node);
		++inactiveNodeCount;
	}

	void ActivateCube(LinkedListNode<GameObject> cube)
	{
		cube.Value.SetActive(true);
		cubePool.Remove (cube);
		cubePool.AddLast(cube);
		--inactiveCubeCount;
	}

	void DeactivateCube(LinkedListNode<GameObject> cube)
	{
		cube.Value.SetActive(false);
		cubePool.Remove(cube);
		cubePool.AddFirst(cube);
		++inactiveCubeCount;
	}

	Vector3 GetRandomFreePosition()
	{
		// Get a random free position.
		int index = Random.Range (0, freePositionsCount);
		Vector3 position = positions[index];

		// Swap the random position with the first non-free position.
		--freePositionsCount;
		positions[index] = positions[freePositionsCount];
		positions[freePositionsCount] = position;

		return position;
	}
}
