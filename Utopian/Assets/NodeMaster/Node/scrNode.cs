using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrNode : MonoBehaviour
{
	public static List<Vector3[]> CubePositions { get; private set; }	// All cube positions for each possible core size, precalculated. 

	/// <summary>
	/// Precomputes all local positions of cubes for each allowed size of core to reduce compuation during the game.
	/// </summary>
	public static void PrecomputeCubePositions()
	{
		CubePositions = new List<Vector3[]>();

		for (int core = 1; core <= CORE_SIZE_MAX; ++core)
		{
			Vector3[] positions = new Vector3[CalculateCubeCount(core)];

			for (int i = 0, cube = 0, shell = core * 3; i < shell * shell; ++i)
			{
				int x = i % shell;
				int y = i / shell;

				if (x >= core && x < core * 2  &&
				    y >= core && y < core * 2)
					continue;

				// Set the position with the i, j coordinates.
				positions[cube] = new Vector3(x, y) - (Vector3)Vector2.one * (shell - 1) * 0.5f;

				Debug.Log (positions[cube]);

				// Push the position out from the radius to give each cube separation from its neighbours, and to round the node.
				positions[cube] += positions[cube] * 0.25f + positions[cube].normalized * core * 0.25f;
			
				++cube;
			}

			CubePositions.Add(positions);
		}
	}

	public static int CalculateCubeCount(int coreSize)
	{
		return (coreSize * 3) * (coreSize * 3) - coreSize * coreSize;;
	}
	
	public const int CORE_SIZE_MAX = 4;
	public const float PULSE_DELAY_MAX = 10.0f;
	public const int LINKS_MAX = 8;	// Number of links possible (also the number of 2d positions in a grid around one position.
	public const int LINK_VERTICES = 16;
	const int LOOPS_PER_FRAME = 50;	// Number of loops allowed per frame of a coroutine before yielding.
	const string DELETED_TAG = "  <td class=\"diff-deletedline\"><div>";
	const string ADDED_TAG =   "  <td class=\"diff-addedline\"><div>";

	public Message Data { get; private set; }
	public bool FullyInfected { get; private set; }
	public bool Infected { get; private set; }
	float infectionPulseDelay = 5.0f;
	float infectionPulseTimer = 0.0f;
	int infectedCubeCount = 0;

	GameObject[] linkedNodes = new GameObject[LINKS_MAX];
	LineRenderer[] links = new LineRenderer[LINKS_MAX];
	float linkExpandDuration = 1.0f;
	float[] linkExpandTimers = new float[LINKS_MAX];
	public int CurrentLinks { get; private set; }

	public LinkedListNode<GameObject> Node { get; private set; }
	public LinkedListNode<GameObject>[] Cubes { get; private set; }	// All cubes of this node.							// if only there was a way to make node a friend class of the node master, then this would be safer. This is all for speed so when the node master destroys the node and wants to add the cube to the cube pool again it doesnt have to search. 
	List<Vector3[]>.Enumerator cubePositionEnumerator;	// Used when constructing the node to grab the cube positions without needing to iterate the list.
	int cubePositionIndex = 0;	// Used when constructing the node to grab cubes from the cube positions.

	string[] words = new string[0];
	int wordIndex = 0;
	float spawnDelay = 4.0f;
	float spawnTimer = 0;

	float expandDuration = 2.0f;
	float expandTimer = 0.0f;

	bool ready = false;

//	public List<scrEnemy> Enemies { get; private set; }

	public GameObject ChildCore { get; private set; }

	public void Init(LinkedListNode<GameObject> node, Message data, int coreSize, bool infected)
	{
		ready = false;
		Node = node;
		Data = data;
		infectionPulseTimer = 0.0f;
		spawnTimer = 0.0f;
		wordIndex = 0;

		// Unlink all nodes.
		CurrentLinks = 0;
		for (int i = 0; i < LINKS_MAX; ++i)
		{
			linkedNodes[i] = null;
			links[i].enabled = false;
			linkExpandTimers[i] = 0.0f;
		}

		//transform.rotation = Random.rotation;
		transform.localScale = Vector3.zero;

		// Get an enumerator to the list item containing the positions this node will use when being built.
		cubePositionEnumerator = CubePositions.GetEnumerator();
		for (int i = 0; i < coreSize; ++i)
			cubePositionEnumerator.MoveNext();
		cubePositionIndex = 0;
		Cubes = new LinkedListNode<GameObject>[cubePositionEnumerator.Current.Length];

		ChildCore.transform.localScale = new Vector3(coreSize, coreSize, coreSize);

		FullyInfected = infected;
		Infected = infected;

		// If fully infected, all cubes are infected.
		if (FullyInfected)
		{
			// Begin reading the text immediately.
			StartCoroutine(Parse ());

			infectionPulseDelay = PULSE_DELAY_MAX * (1 - (coreSize - 1) / CORE_SIZE_MAX);
			infectedCubeCount = Cubes.Length;
			ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreInfected;
		}
		else
		{
			infectedCubeCount = 0;
			ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreUninfected;
		}
	}

	public void MakeReady()
	{
		ready = true;
	}
	
	public void AddCube(LinkedListNode<GameObject> cube)
	{
		// Infect the cube if this node is a primary infection.
		if (Infected)
			cube.Value.GetComponent<scrCube>().InfectImmediate();

		// Position the cube with the precalculated array.
		//cube.Value.transform.position = transform.TransformPoint(cubePositionEnumerator.Current[cubePositionIndex]);
		cube.Value.transform.rotation = transform.rotation;

		// Store the linked list node for this cube.
		Cubes[cubePositionIndex] = cube;

		// Get ready for the next cube.
		++cubePositionIndex;
	}

	public void Infect(int count)
	{
		Infected = true;

		if (infectedCubeCount + count > Cubes.Length)
		{
			count -= infectedCubeCount + count - Cubes.Length;
			FullyInfected = true;

			// Read the text.
			StartCoroutine(Parse ());

			// Set the infected materials.
			ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreInfected;

			scrNodeMaster.Instance.CreateLinks(Node);
		}

		for (int i = infectedCubeCount; i < infectedCubeCount + count; ++i)
		{
			Cubes[i].Value.GetComponent<scrCube>().Infect();
		}

		infectedCubeCount += count;
	}

	void InfectLinkedNodes()
	{
		for (int i = 0; i < CurrentLinks; ++i)
		{
			linkedNodes[i].GetComponent<scrNode>().Infect(Mathf.CeilToInt(infectedCubeCount * 0.1f));
		}
	}

	public void Link(GameObject node)
	{
		// Set the linked gameobject.
		linkedNodes[CurrentLinks] = node;

		// Set up the visual link.
		links[CurrentLinks].enabled = true;
		
		// Set the control point half way between the two points then pushed in a random direction perpendicular to the connecting line.
		float curve = 30.0f;
		Vector3 control = Vector3.Lerp (transform.position, node.transform.position, 0.5f) + curve * Vector3.Cross (node.transform.position - transform.position, Random.rotationUniform.eulerAngles).normalized;
		
		for (int i = 0; i < LINK_VERTICES; ++i)
		{
			float t = (float)i / (LINK_VERTICES - 1);
			float tInv = 1.0f - t;
			
			links[CurrentLinks].SetPosition(i, tInv * tInv * transform.position + 2 * tInv * t * control + t * t * node.transform.position);
		}

		++CurrentLinks;
	}

	IEnumerator Parse()
	{
		int numLoops = 0;

		// Load the page.
		WWW page = new WWW(Data.url);
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

		words = whitespaceRemoved.ToArray();
	}
	
	void SpawnEnemies()
	{
		spawnTimer += Time.deltaTime;
		if (spawnTimer >= spawnDelay)
		{
			// Spawn enemies once the words have been parsed.
			if (words.Length != 0)
			{
				// Choose which enemy to make.

				// Word enemy.
				//scrWordEnemy enemy = ((GameObject)Instantiate(scrEnemyMaster.Instance.WordEnemyPrefab, transform.position, transform.rotation)).GetComponent<scrWordEnemy>();

				++wordIndex;
				if (wordIndex >= words.Length)
					wordIndex = 0;

				//enemy.Init (gameObject, words[wordIndex]);


				//Enemies.Add(enemy);

				++wordIndex;
				if (wordIndex == words.Length)
					wordIndex = 0;
			}

			spawnTimer = 0;
		}
	}

	public void CheckEnemies()
	{
		//for (int i = Enemies.Count - 1; i >= 0; --i)
		//{
		//	if (Enemies[i] == null)
		//	{
			//	Enemies.RemoveAt(i);
		//	}
		//}
	}

	public float GetInfectionAmount()
	{
		if (Cubes != null)
			return (float)infectedCubeCount / Cubes.Length;

		return 0;
	}

	public float GetCorruptionAmount()
	{
		if (Cubes != null)
			return (1.0f - (float)Cubes.Length / Cubes.Length);

		return 0;
	}
	
	// Use this for initialization
	void Start ()
	{
		//Enemies = new List<scrEnemy>();
		ChildCore = transform.Find ("Core").gameObject;


		for (int i = 0; i < LINKS_MAX; ++i)
		{
			GameObject childLink = new GameObject("Link");
			childLink.transform.parent = this.transform;

			LineRenderer line = childLink.AddComponent<LineRenderer>();
			line.material = scrNodeMaster.Instance.MatLink;
			line.SetColors(Color.clear, Color.clear);
			line.SetVertexCount(LINK_VERTICES);
			line.enabled = false;
			links[i] = line;
			linkedNodes[i] = null;
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!ready)
			return;

		if (expandTimer < expandDuration)
		{
			expandTimer += Time.deltaTime;
			if(expandTimer > expandDuration)
				expandTimer = expandDuration;

			// Expand core.
			transform.localScale = Vector3.Lerp (Vector3.zero, Vector3.one, expandTimer / expandDuration);

			// Expand out cubes.
			for (int i = 0; i < Cubes.Length; ++i)
				Cubes[i].Value.transform.position = Vector3.Lerp (transform.position, transform.TransformPoint(cubePositionEnumerator.Current[i]), expandTimer / expandDuration);
		}
		else
		{
			if (FullyInfected)
			{
				//SpawnEnemies();
				//CheckEnemies();

				// Clear redundant links and animate current links.
				float halfLinkExpandDuration = linkExpandDuration * 0.5f;
				for (int i = 0; i < CurrentLinks; ++i)
				{
					if (!linkedNodes[i].activeSelf || linkedNodes[i].GetComponent<scrNode>().FullyInfected)
					{					
						linkedNodes[i] = linkedNodes[CurrentLinks - 1];
						linkedNodes[CurrentLinks - 1] = null;
						
						LineRenderer temp = links[CurrentLinks - 1];
						links[CurrentLinks - 1] = links[i];
						links[i] = temp;
						
						float temp2 = linkExpandTimers[CurrentLinks - 1];
						linkExpandTimers[CurrentLinks - 1] = linkExpandTimers[i];
						linkExpandTimers[i] = temp2;
						
						--i;
						--CurrentLinks;
					}
					else if (linkExpandTimers[i] < linkExpandDuration)
					{
						linkExpandTimers[i] += Time.deltaTime;
						if (linkExpandTimers[i] > linkExpandDuration)
							linkExpandTimers[i] = linkExpandDuration;
						
						links[i].SetColors(Color.Lerp (Color.clear, scrNodeMaster.ColCoreInfected, linkExpandTimers[i] / halfLinkExpandDuration),
						                   Color.Lerp (Color.clear, scrNodeMaster.ColCoreUninfected, (linkExpandTimers[i] - halfLinkExpandDuration) / halfLinkExpandDuration));
					}
				}
				
				for (int i = CurrentLinks; i < LINKS_MAX; ++i)
				{
					if (linkExpandTimers[i] > 0)
					{
						linkExpandTimers[i] -= Time.deltaTime * 3;
						if (linkExpandTimers[i] < 0.0f)
						{
							links[i].SetColors(Color.clear, Color.clear);
							links[i].enabled = false;
							linkExpandTimers[i] = 0.0f;
						}
						
						links[i].SetColors(Color.Lerp (Color.clear, scrNodeMaster.ColCoreInfected, linkExpandTimers[i] / halfLinkExpandDuration),
						                   Color.Lerp (Color.clear, scrNodeMaster.ColCoreUninfected, (linkExpandTimers[i] - halfLinkExpandDuration) / halfLinkExpandDuration));
					}
				}

				infectionPulseTimer += Time.deltaTime;
				if (infectionPulseTimer > infectionPulseDelay)
				{
					infectionPulseTimer = 0;
					InfectLinkedNodes();
				}
			}
			else
			{
				float infectionAmount = GetInfectionAmount();
				if (infectionAmount > 0)
				{
					ChildCore.renderer.material.color = Color.Lerp (scrNodeMaster.ColCoreUninfected, scrNodeMaster.ColCoreInfected, infectionAmount);
				}
			}
		}
	}
}
