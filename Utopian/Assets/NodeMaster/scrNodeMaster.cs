using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrNodeMaster : MonoBehaviour
{
	public static scrNodeMaster Instance { get; private set; }

	const int LOOPS_PER_FRAME = 200;

	#region Pool Variables

	const int TOTAL_NODES = 100;
	const int TOTAL_CUBES = 10000;

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

	public const int GRID_SIZE = 10;
	public const int CELL_SIZE = 40;
	public static bool[,] FreeCells { get; private set; }

	#endregion

	Queue<Message> messageQueue = new Queue<Message>();
	bool creating = false;

	public scrNode NodeBeingUploaded { get; private set; }

	public GameObject NodePrefab;
	public GameObject CubePrefab;
	public Material MatCubeUninfected, MatCubeInfected;
	public Material MatCoreUninfected, MatCoreInfected;
	public Material MatLink;

	public static Color ColCubeUninfected { get; private set; }
	public static Color ColCubeInfected { get; private set; }
	public static Color ColCoreUninfected { get; private set; }
	public static Color ColCoreInfected { get; private set; }

	public GameObject ChildGrid { get; private set; }
	public GameObject ChildSpark { get; private set; }

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
			positions = new Vector3[GRID_SIZE * GRID_SIZE - 4];
			freePositionsCount = 0;
			
			int mid = GRID_SIZE / 2;
			for (int i = 0; i < GRID_SIZE; ++i)
			{
				for (int j = 0; j < GRID_SIZE; ++j)
				{
					if (!((i == mid || i == mid - 1) && (j == mid || j == mid - 1)))
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

		for (int i = 0; i < GRID_SIZE; ++i)
			for (int j = 0; j < GRID_SIZE; ++j)
				FreeCells[i, j] = true;
	}

	#endregion

	public static int ToCellSpace(float value)
	{
		return (int)(value + GRID_SIZE * CELL_SIZE * 0.5f) / CELL_SIZE;
	}

	public static Vector2 ToCellSpace(Vector2 value)
	{
		return new Vector2((int)(value.x + GRID_SIZE * CELL_SIZE * 0.5f) / CELL_SIZE, (int)(value.y + GRID_SIZE * CELL_SIZE * 0.5f) / CELL_SIZE);
	}

	public static float ToWorldSpace(float value)
	{
		return value * CELL_SIZE - GRID_SIZE * CELL_SIZE;
	}

	public static Vector2 ToWorldSpace(Vector2 value)
	{
		return value * CELL_SIZE - Vector2.one * GRID_SIZE * CELL_SIZE * 0.5f;
	}
	// Use this for initialization
	void Start ()
	{
		Instance = this;

		ChildGrid = transform.Find ("Grid").gameObject;
		ChildGrid.transform.localScale = new Vector3(GRID_SIZE * CELL_SIZE, GRID_SIZE * CELL_SIZE, 1);
		ChildGrid.renderer.material.SetInt("_GridSize", GRID_SIZE);
		ChildGrid.renderer.material.SetInt("_CellSize", CELL_SIZE);

		ColCubeUninfected = MatCubeUninfected.GetColor ("_GlowColor");
		ColCubeInfected = MatCubeInfected.GetColor ("_GlowColor");
		ColCoreUninfected = MatCoreUninfected.color;
		ColCoreInfected = MatCoreInfected.color;

		FreeCells = new bool[GRID_SIZE, GRID_SIZE];
		for (int i = 0; i < GRID_SIZE; ++i)
			for (int j = 0; j < GRID_SIZE; ++j)
				FreeCells[i, j] = true;

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
				if (summary == "" || !(summary.Contains("UNDID") || summary.Contains ("UNDO") || summary.Contains("REVERT") || summary.Contains("REVERSION") || summary.Contains("VANDAL") || summary.Contains("SPAM")))
				{
					// If the user is not a bot, create the node.  An edit by a bot that is not a vandalism reversion is unlikely to contain any decent information.
					if (!message.is_bot && !message.is_anon)
						StartCoroutine(Create (message, false));
				}
				else
				{
					// Create an infected node.
					StartCoroutine(Create (message, true));
				}
			}
		}

		// Set the shader's player position uniform.
		ChildGrid.renderer.material.SetFloat("_PlayerX", scrPlayer.Instance.transform.position.x);
		ChildGrid.renderer.material.SetFloat("_PlayerY", scrPlayer.Instance.transform.position.y);

		// Check if the node has finished uploading.
		if (NodeBeingUploaded == null || !NodeBeingUploaded.Uploading)
		{
			// Get the next node to upload.  This should be the earliest node added.
			if (inactiveNodeCount > 0)
			{
				foreach (GameObject n in nodePool)
				{
					if (n.activeSelf)
					{
						NodeBeingUploaded = n.GetComponent<scrNode>();
//						NodeBeingUploaded.BeginUpload();
						break;
					}
				}
			}
			else
			{
				NodeBeingUploaded = null;
			}
		}
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
			if (!infected)
				yield break;
		}

		// Don't create a node if there are no cubes available.
		if (inactiveCubeCount == 0)
		{
			Debug.Log("There are no inactive cubes left to create a node for \"" +  message.page_title + "\".");
			creating = false;
			if (!infected)
				yield break;
		}

		// Set the size of the core based on the change_size of the message.
		int coreSize = Mathf.Clamp (Mathf.CeilToInt(Mathf.Log10 (Mathf.Abs (message.change_size) + 1)), scrNode.CORE_SIZE_MIN, scrNode.CORE_SIZE_MAX);

		// Get the number of cubes there would be around this core.
		int numCubes = scrNode.CubePositions[coreSize - 1].Length;

		// Don't create a node if there aren't enough cubes available.
		if (inactiveCubeCount < numCubes)
		{
			Debug.Log ("There are not enough inactive cubes (" + inactiveCubeCount + "/" + numCubes + ") in the pool to create a node for \"" + message.page_title + "\".");
			creating = false;
			if (!infected)
				yield break;
		}

		// If not creating but got this far, node is infected and must replace an existing one,
		if (creating == false)
		{
			creating = true;

			// Loop through the active nodes until one is found that is completely out of the player's view.
			LinkedListNode<GameObject> n = nodePool.Last;
			for (int i = 0; i < TOTAL_NODES - inactiveNodeCount; ++i)
			{
				// Don't replace the node being uploaded.
				if (NodeBeingUploaded != null && n.Value == NodeBeingUploaded.gameObject)
					continue;

				Vector2 position = n.Value.transform.position;
				Vector2 topLeft = Camera.main.ScreenToWorldPoint(Vector2.zero);
				Vector2 bottomRight = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

				// Find out if the node is out of view.
				if (position.x + CELL_SIZE * 0.5f < topLeft.x || position.x - CELL_SIZE * 0.5f > bottomRight.x ||
				    position.y + CELL_SIZE * 0.5f < topLeft.y || position.y - CELL_SIZE * 0.5f > bottomRight.y)
				{
					// Convert the node.
					scrNode nScript = n.Value.GetComponent<scrNode>();
					nScript.ConvertToInfected(message);
					break;
				}

				// Move back one node.
				n = n.Previous;
			}

			yield break;
		}

		// All checks have passed - a node can be made.  Get the first inactive node in the node pool.
		LinkedListNode<GameObject> node = nodePool.First;

		// Activate, position and initialise the node.
		ActivateNode(node);
		node.Value.transform.position = PopRandomFreePosition();
		scrNode nodeScript = node.Value.GetComponent<scrNode>();
		nodeScript.Init(node, message, coreSize, infected);
		FreeCells[ToCellSpace(nodeScript.transform.position.x), ToCellSpace(nodeScript.transform.position.y)] = false;

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
		FreeCells[ToCellSpace(nodeScript.transform.position.x), ToCellSpace(nodeScript.transform.position.y)] = true;

		// Clear and reset the nodes cubes and make them available to future nodes.
		for (int i = 0, numLoops = 0; i < nodeScript.Cubes.Length; ++i)
		{
			LinkedListNode<GameObject> cube = nodeScript.Cubes[i];
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
		Bounds nodeBounds = new Bounds(node.Value.transform.position, new Vector3(CELL_SIZE * 2, CELL_SIZE * 2, CELL_SIZE * 2));	// Instead, could look for nodes at the adjacent positions of positions array, derp.
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

	public void DeactivateNode(LinkedListNode<GameObject> node)
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

	public void DeactivateCube(LinkedListNode<GameObject> cube)
	{
		cube.Value.SetActive(false);
		cubePool.Remove(cube);
		cubePool.AddFirst(cube);
		++inactiveCubeCount;
	}

	public Vector3 GetRandomFreePosition(bool excludeUserVision)
	{
		if (excludeUserVision)
		{
			// Loop through all free positions, starting at a random position, and get the first one found to be out of view.
			int index = Random.Range (0, freePositionsCount);
			for (int i = 0; i < freePositionsCount; ++i)
			{
				Vector2 p = positions[index];
				Vector2 topLeft = Camera.main.ScreenToWorldPoint(Vector2.zero);
				Vector2 bottomRight = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
				
				// Find out if the position is out of view.
				if (p.x + CELL_SIZE * 0.5f < topLeft.x || p.x - CELL_SIZE * 0.5f > bottomRight.x ||
				    p.y + CELL_SIZE * 0.5f < topLeft.y || p.y - CELL_SIZE * 0.5f > bottomRight.y)
					return p;

				++index;
				if (index == freePositionsCount)
					index = 0;
			}
		}
		
		return positions[Random.Range(0, freePositionsCount)];
	}

	Vector3 PopRandomFreePosition()
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
