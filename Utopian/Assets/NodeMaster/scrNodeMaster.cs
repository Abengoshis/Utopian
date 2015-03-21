using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrNodeMaster : MonoBehaviour
{
	public static scrNodeMaster Instance { get; private set; }
	public static bool SelectNewNode = true;

	const int LOOPS_PER_FRAME = 100;

	const string DELETED_TAG = "  <td class=\"diff-deletedline\"><div>";
	const string ADDED_TAG =   "  <td class=\"diff-addedline\"><div>";
	const string WIKITAG_TAG = "<span class=\"mw-tag-marker";

	#region Pool Variables

	const int TOTAL_NODES = 100;
	const int TOTAL_CUBES = 5000;

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
	public const int CELL_SIZE = 60;

	public enum CellState
	{
		FREE = 0,
		BLOCKED = 1,
		CLEAN = 2,
		INFECTED = 3,
		INFECTING_CLEAN = 4,
		INFECTING_BLOCKED = 5
	}
	public static CellState[,] CellStates;

	#endregion

	Queue<Message> infectedBufferQueue = new Queue<Message>();	// Buffer of infected cubes accumulated between transitions.
	Queue<Message> messageQueue = new Queue<Message>();
	bool creating = false;
	string[] creatingWords;
	Message creatingMessage;

	public scrNode NodeBeingUploaded { get; private set; }

	public GameObject NodePrefab;
	public GameObject CubePrefab;

	public Material MatCubeBlocked, MatCubeUninfected, MatCubeInfected;
	public Material MatCoreBlocked, MatCoreUninfected, MatCoreInfected;
	public Material MatLink;

	public static Color ColCubeBlocked { get; private set; }
	public static Color ColCubeUninfected { get; private set; }
	public static Color ColCubeInfected { get; private set; }

	public static Color ColCoreBlocked { get; private set; }
	public static Color ColCoreUninfected { get; private set; }
	public static Color ColCoreInfected { get; private set; }

	public GameObject Grid;
	public GameObject NodeBossPrefab;

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

			//nodePoolLoaded = true;  The gameobjects unfortunately get destroyed, so keeping them throughout levels is impossible.
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

		//	cubePoolLoaded = true;
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

	public IEnumerator Purge()
	{
		NodeBeingUploaded = null;

		float loops = 0;

		// Expand a square from the centre.
		float dRadius = CELL_SIZE;
		float radius = 0;
		while (inactiveCubeCount != cubePool.Count)
		{
			radius += dRadius;
			foreach (Collider2D c in Physics2D.OverlapAreaAll(new Vector2(-radius, -radius), new Vector2(radius, radius), (1 << LayerMask.NameToLayer("Cube")) | (1 << LayerMask.NameToLayer("Enemy")) ))
			{
				if (c != null && 
				    (c.GetComponent<scrCube>() != null ||
				    c.GetComponent<scrEnemy>() != null))
				{
					c.SendMessage("DestroyImmediate", SendMessageOptions.RequireReceiver);
				}

				if (++loops == 30)
				{
					loops = 0;
					break;
				}
			}
			yield return new WaitForEndOfFrame();
		}
		
		freePositionsCount = positions.Length;

		for (int i = 0; i < GRID_SIZE; ++i)
			for (int j = 0; j < GRID_SIZE; ++j)
				CellStates[i, j] = CellState.FREE;

		NodeBeingUploaded = null;
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

	void SpawnBoss(Message message)
	{
		messageQueue.Dequeue();
		scrGUI.Instance.AddToFeed(message.page_title, Color.red);
		scrBoss boss = ((GameObject)Instantiate(NodeBossPrefab, GetRandomFreePosition(true), Quaternion.identity)).GetComponent<scrBoss>();
		boss.Init(message);
		scrResults.ReversionEdits.Add(message);
	}

	// Use this for initialization
	void Start ()
	{
		Instance = this;
		SelectNewNode = true;

		Grid.transform.localScale = new Vector3(GRID_SIZE * CELL_SIZE, GRID_SIZE * CELL_SIZE, 1);
		Grid.renderer.material.SetInt("_GridSize", GRID_SIZE);
		Grid.renderer.material.SetInt("_CellSize", CELL_SIZE);

		ColCubeBlocked = MatCubeBlocked.GetColor("_GlowColor");
		ColCubeUninfected = MatCubeUninfected.GetColor ("_GlowColor");
		ColCubeInfected = MatCubeInfected.GetColor ("_GlowColor");

		ColCoreBlocked = MatCoreBlocked.color;
		ColCoreUninfected = MatCoreUninfected.color;
		ColCoreInfected = MatCoreInfected.color;

		CellStates = new CellState[GRID_SIZE, GRID_SIZE];
		for (int i = 0; i < GRID_SIZE; ++i)
			for (int j = 0; j < GRID_SIZE; ++j)
				CellStates[i, j] = CellState.FREE;

		// Disable. Reenabled by the master.
		enabled = false;
	}

	// Update is called once per frame
	void Update ()
	{
		// Set the shader's player position uniform.
		Grid.renderer.material.SetFloat("_PlayerX", scrPlayer.Instance.transform.position.x);
		Grid.renderer.material.SetFloat("_PlayerY", scrPlayer.Instance.transform.position.y);

		// Generate nodes as long as the queue contains messages.
		if (messageQueue.Count != 0)
		{
			// Get the message.
			Message message = messageQueue.Peek ();

			// Check the message for certain criteria to determine whether or not to make an infected or uninfected node.
			string summary = message.summary != null ? message.summary.ToUpper() : "";

//			if (summary.Contains("VANDAL") || summary.Contains("SPAM"))
//			{
//				SpawnBoss(message);
//			}
			if (summary.Length == 0 || !(summary.Contains("VANDAL") || summary.Contains("SPAM") || summary.Contains("UNDID") || summary.Contains ("UNDO") || summary.Contains("REVERT") || summary.Contains("REVERSION")))
			{
				// Remove the message if it isn't infected so it's more likely infected nodes accumulate.
				//messageQueue.Dequeue();

				// If the user is not a bot and not anonymous, create the node.  An edit by a bot that is not a vandalism reversion is unlikely to contain any decent information.
				if (message.is_bot || message.is_anon)
				{
					scrGUI.Instance.AddToFeed(message.page_title, new Color(0.1f, 0.1f, 0.1f));
					messageQueue.Dequeue();
				}
				else if (!creating)
				{
					messageQueue.Dequeue();
					StartCoroutine(Create (message, false));
				}
			}
			else
			{
				// Create an infected node.
				if (!creating)
				{
					messageQueue.Dequeue();

					scrResults.ReversionEdits.Add(message);

					StartCoroutine(Create (message, true));
				}
			}
		}

		// Check if the node has finished uploading.
		if (SelectNewNode)
		{
			// Get the next node to upload.  This should be the earliest node added that isn't inactive.
			if (inactiveNodeCount > 0)
			{
				foreach (GameObject n in nodePool)
				{
					if (n.activeSelf)
					{
						NodeBeingUploaded = n.GetComponent<scrNode>();
						SelectNewNode = false;
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

	public void InjectInfectedMessages(int max)
	{
		for (int i = 0; i < max && infectedBufferQueue.Count != 0; ++i)
		{
			messageQueue.Enqueue(infectedBufferQueue.Dequeue());
		}
	}

	IEnumerator Parse()
	{
		int numLoops = 0;
		
		// Load the page.
		WWW page = new WWW(creatingMessage.url);
		while (!page.isDone)
		{
			if (++numLoops == LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}
		
		numLoops = 0;
		
		// Get the deleted and added content.
		string[] lines = page.text.Split('\n');
		List<string> deleted = new List<string>();
		List<string> added = new List<string>();
		foreach (string line in lines)
		{
			if (line.StartsWith(DELETED_TAG))
				deleted.Add (line);
			else if (line.StartsWith(ADDED_TAG))
				added.Add (line);
//			
//			if (line.Contains(WIKITAG_TAG))
//			{
//				if (line.Contains("blanking") || line.Contains("vandal") || line.Contains("repeating") ||
//				    line.Contains ("shouting") || line.Contains("nonsense") || line.Contains("spam"))
//				{
//					creatingMessage.summary = "vandal";
//					scrResults.ReversionEdits.Add(creatingMessage);
//					scrGUI.Instance.AddToFeed(creatingMessage.page_title, Color.red);
//					scrBoss boss = ((GameObject)Instantiate(NodeBossPrefab, transform.position, Quaternion.identity)).GetComponent<scrBoss>();
//					boss.Init(creatingMessage);
//					scrResults.ReversionEdits.Add(creatingMessage);
//					creating = false;
//					yield break;
//				}
//			}
			
			if (++numLoops > LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}
		
		string concat = "";
		foreach (string line in deleted)
			concat += line + System.Environment.NewLine;
		foreach (string line in added)
			concat += line + System.Environment.NewLine;
		
		numLoops = 0;
		
		// Strip tags.
		char[] stripped = new char[concat.Length];
		int length = 0;
		
		bool tagAngle = false;	// <...>
		bool tagCurly = false;	// {...}
		bool tagSquare = false;	// [...]
		bool tagApsSc = false;	// &...;
		bool tagHash = false;	// #... 
		
		for (int i = 0; i < stripped.Length; ++i)
		{
			char c = concat[i];
			switch (c)
			{
			case '<':
				tagAngle = true;
				break;
			case '>':
				tagAngle = false;
				break;
			case '{':
				tagCurly = true;
				break;
			case '}':
				tagCurly = false;
				break;
			case '[':
				tagSquare = true;
				break;
			case ']':
				tagSquare = false;
				break;
			case '&':
				tagApsSc = true;
				break;
			case ';':
				tagApsSc = false;
				break;
			case '#':
				tagHash = true;
				break;
			case ' ':
				tagHash = false;
				break;
			}
			
			if (!tagAngle && !tagCurly && !tagSquare && !tagApsSc && !tagHash && (char.IsLetter(c) || char.IsWhiteSpace(c)))
				stripped[length++] = c;
			else
				stripped[length++] = ' ';
			
			if (++numLoops == LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}
		
		// Split into words.  If there are more words in deleted than added, use deleted and vice versa.
		char[] split = new char[] {' '};
		string[] strippedWords = (new string(stripped, 0, length)).Split(split, System.StringSplitOptions.RemoveEmptyEntries);
		List<string> whitespaceRemoved = new List<string>();
		
		foreach (string titleWord in creatingMessage.page_title.Split(split, System.StringSplitOptions.RemoveEmptyEntries))
			whitespaceRemoved.Add(titleWord);
		
		numLoops = 0;
		
		for (int i = 0; i < strippedWords.Length; ++i)
		{
			for (int j = 0; j < strippedWords[i].Length; ++j)
			{
				if (!char.IsWhiteSpace(strippedWords[i][j]))
				{
					whitespaceRemoved.Add(strippedWords[i]);
					break;
				}
			}
			
			if (++numLoops == LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}
		
		creatingWords = whitespaceRemoved.ToArray();
	}

	public IEnumerator Create(Message message, bool infected)
	{
		creating = true;

		if (message.is_anon)
			message.user = "Anonymous";

		creatingMessage = message;

		yield return StartCoroutine(Parse ());

		// Check if vandal tags detected.
		if (creating == false)
		{
			SpawnBoss(message);
			yield break;
		}

		if (scrMaster.Instance.Transitioning)
		{
			if (infected)
			{
				infectedBufferQueue.Enqueue(message);
			}
			else
			{
				scrGUI.Instance.AddToFeed(message.page_title, new Color(0.1f, 0.1f, 0.1f));
			}
			creating = false;
			yield break;
		}

		if (freePositionsCount == 0)
		{
		//	Debug.Log("There are no free positions left to create a node for \"" +  message.page_title + "\".");
			creating = false;
			if (!infected)
				yield break;
		}

		// Don't create a node if there are no nodes available.
		if (inactiveNodeCount == 0)
		{
		//	Debug.Log("There are no inactive nodes left to create a node for \"" +  message.page_title + "\".");
			creating = false;
			if (!infected)
				yield break;
		}

		// Don't create a node if there are no cubes available.
		if (inactiveCubeCount == 0)
		{
		//	Debug.Log("There are no inactive cubes left to create a node for \"" +  message.page_title + "\".");
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
				scrNode nScript = n.Value.GetComponent<scrNode>();

				// Don't replace nodes in view, fully infected nodes, the node being uploaded, or nodes that arent being infected.
				if (!(nScript.VisibleToPlayer || nScript.FullyInfected || !nScript.Infected || NodeBeingUploaded != null && n.Value == NodeBeingUploaded.gameObject))
				{
					// Convert the node.
					nScript.ConvertToInfected(message);
					break;
				}

				// Move back one node.
				n = n.Previous;

				yield return new WaitForEndOfFrame();
			}

			scrGUI.Instance.AddToFeed(message.page_title, ColCoreInfected);

			yield return new WaitForSeconds(3.0f);	// 3 Second delay between node creation.

			creating = false;

			yield break;
		}

		// All checks have passed - a node can be made.  Get the first inactive node in the node pool.
		LinkedListNode<GameObject> node = nodePool.First;

		// Activate, position and initialise the node.
		ActivateNode(node);
		node.Value.transform.position = PopRandomFreePosition();
		scrNode nodeScript = node.Value.GetComponent<scrNode>();
		nodeScript.Init(node, message, coreSize, infected, creatingWords);
		// Set the cell's state to either infected or, if contribution is positive, clean else blocked.
		CellStates[ToCellSpace(nodeScript.transform.position.x), ToCellSpace(nodeScript.transform.position.y)] = infected ? CellState.INFECTED : nodeScript.Data.change_size > 0 ? CellState.CLEAN : CellState.BLOCKED;

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

		scrGUI.Instance.AddToFeed(message.page_title, infected ? ColCoreInfected : (message.change_size > 0 ? Color.cyan : Color.grey));

		creating = false;
	}

	public IEnumerator Destroy(LinkedListNode<GameObject> node)
	{
		scrNode nodeScript = node.Value.GetComponent<scrNode>();
		CellStates[ToCellSpace(nodeScript.transform.position.x), ToCellSpace(nodeScript.transform.position.y)] = CellState.FREE;

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
		if (node.Value.activeSelf)
			Debug.Log ("Trying to activate an already active node!");

		node.Value.SetActive(true);
		nodePool.Remove (node);
		nodePool.AddLast(node);
		--inactiveNodeCount;
	}

	public void DeactivateNode(LinkedListNode<GameObject> node)
	{
		// Free the position.
		for (int i = freePositionsCount; i < positions.Length; ++i)
		{
			if (positions[i] == node.Value.transform.position)
			{
				positions[i] = positions[freePositionsCount];
				positions[freePositionsCount] = node.Value.transform.position;
				++freePositionsCount;
				break;
			}
		}

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
		
		Vector3 pos = positions[Random.Range(0, freePositionsCount)];
		pos.z = 0;
		return pos;	
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
