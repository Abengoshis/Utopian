﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrNode : MonoBehaviour
{
	public static List<Vector3[]> CubePositions { get; private set; }	// All cube positions for each possible core size, precalculated. 

	/// <summary>
	/// Precomputes all local positions of cubes for each allowed size of core to reduce computation during the game.
	/// </summary>
	public static void PrecomputeCubePositions()
	{
		if (CubePositions != null)
			return;

		CubePositions = new List<Vector3[]>();

		for (int core = 1; core <= CORE_SIZE_MAX; ++core)
		{
			int cubeCount = CalculateCubeCount(core);
			List<Vector3> positions = new List<Vector3>();

			for (int i = 0, cube = 0, shell = core * 3; i < shell * shell; ++i)
			{
				int x = i % shell;
				int y = i / shell;

				if (x >= core && x < core * 2  &&
				    y >= core && y < core * 2)
					continue;

				// Set the position with the i, j coordinates.
				positions.Add(new Vector3(x, y) * 2 - (Vector3)Vector2.one * (shell - 1));


				// Push the position out from the radius to give each cube separation from its neighbours.
				//positions[cube] += positions[cube] * 0.5f;
			
				++cube;
			}

			// Reorder the positions so that the outermost ones are first.
			Vector3[] reorderedPositions = new Vector3[cubeCount];
			for (int i = 0; i < cubeCount; ++i)
			{
				Vector3 furthestPosition = Vector3.zero;
				float furthestDistance = 0;
				for (int j = 0; j < positions.Count; ++j)
				{
					float distance = positions[j].magnitude;
					if (distance > furthestDistance)
					{
						furthestDistance = distance;
						furthestPosition = positions[j];
					}
				}
				positions.Remove(furthestPosition);
				reorderedPositions[i] = furthestPosition;
			}

			CubePositions.Add(reorderedPositions);
		}
	}

	public static int CalculateCubeCount(int coreSize)
	{
		return (coreSize * 3) * (coreSize * 3) - (coreSize) * (coreSize);
	}

	public const int CORE_SIZE_MIN = 2;
	public const int CORE_SIZE_MAX = 4;
	public const float PULSE_DELAY_MAX = 10.0f;
	public const int LINKS_MAX = 8;	// Number of links possible (also the number of 2d positions in a grid around one position.
	public const int LINK_VERTICES = 8;
	const int LOOPS_PER_FRAME = 50;	// Number of loops allowed per frame of a coroutine before yielding.
	const string DELETED_TAG = "  <td class=\"diff-deletedline\"><div>";
	const string ADDED_TAG =   "  <td class=\"diff-addedline\"><div>";

	public Message Data { get; private set; }

	public System.TimeSpan ReversionTime { get; private set; }
	float bossMakeDuration = 10.0f;	// After this time, a boss will spawn at the infected node.
	float bossMakeTimer = 0.0f;

	public bool FullyInfected { get; private set; }
	public bool Infected { get; private set; }
	public bool Blocked { get { return !FullyInfected && Data.change_size <= 0; } }
	public bool Uploading { get; private set; }
	float infectionPulseDelay = 5.0f;
	float infectionPulseTimer = 0.0f;
	public int InfectedCubeCount = 0;	// Echh...if only friend classes existed in C#. Almost all of these public variables are due to rushing.

	GameObject[] linkedNodes = new GameObject[LINKS_MAX];
	LineRenderer[] links = new LineRenderer[LINKS_MAX];
	float linkExpandDuration = 1.0f;
	float[] linkExpandTimers = new float[LINKS_MAX];
	public int CurrentLinks { get; private set; }

	public LinkedListNode<GameObject> Node { get; private set; }
	public LinkedListNode<GameObject>[] Cubes { get; private set; }	// All cubes of this node.							// if only there was a way to make node a friend class of the node master, then this would be safer. This is all for speed so when the node master destroys the node and wants to add the cube to the cube pool again it doesnt have to search. 
	List<Vector3[]>.Enumerator cubePositionEnumerator;	// Used when constructing the node to grab the cube positions without needing to iterate the list.
	int cubePositionIndex = 0;	// Used when constructing the node to grab cubes from the cube positions.
	int totalCubeCount = 0;
	public int CellX { get; private set; }
	public int CellY { get; private set; }

	string[] words = new string[0];
	int wordIndex = 0;
	float spawnDelay = 10.0f;
	float spawnDelayPerCharacter = 3.0f;
	float spawnTimer = 0;

	float expandDuration = 2.0f;
	float expandTimer = 0.0f;

	bool ready = false;
	public bool VisibleToPlayer { get; private set; }

//	public List<scrEnemy> Enemies { get; private set; }

	public GameObject ChildCore { get; private set; }
	public GameObject ChildInfo { get; private set; }
	public GameObject[] ChildData { get; private set; }
	public GameObject ExplosionPrefab;

	public AudioClip UploadCubeSound;

	public void Init(LinkedListNode<GameObject> node, Message data, int coreSize, bool infected)
	{
		if (coreSize == 0)
			Debug.Log ("WHAT");

		ready = false;
		Node = node;
		Data = data;
		Uploading = false;

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
		expandTimer = 0;

		//transform.rotation = Random.rotation;
		//transform.localScale = Vector3.zero;

		// Get an enumerator to the list item containing the positions this node will use when being built.
		cubePositionEnumerator = CubePositions.GetEnumerator();
		for (int i = 0; i < coreSize; ++i)
			cubePositionEnumerator.MoveNext();
		cubePositionIndex = 0;
		Cubes = new LinkedListNode<GameObject>[cubePositionEnumerator.Current.Length];

		ChildCore.transform.localScale = new Vector3(coreSize, coreSize, coreSize);
		ChildInfo.transform.rotation = Quaternion.identity;

		// Set the distance of the infos based on the core size.
		ChildData[0].transform.localPosition = new Vector3(0, coreSize * 4, 0);
		ChildData[1].transform.localPosition = new Vector3(coreSize * 4, 0, 0);
		ChildData[2].transform.localPosition = new Vector3(0, -coreSize * 4, 0);
		ChildData[3].transform.localPosition = new Vector3(-coreSize * 4, 0, 0);

		// Set the data of the infos.
		ChildData[0].GetComponent<TextMesh>().text = Data.page_title;
		ChildData[1].GetComponent<TextMesh>().text = "User: " + Data.user;
		ChildData[2].GetComponent<TextMesh>().text = "Altered: " + (Data.change_size > 0 ? ("+" + Data.change_size.ToString()) : Data.change_size.ToString()) + " Bytes";
		ChildData[3].GetComponent<TextMesh>().text = "Edited Time: " + Data.time;

		FullyInfected = infected;
		Infected = infected;

		// If fully infected, all cubes are infected.
		if (FullyInfected)
		{
			// Begin reading the text immediately.
			StartCoroutine(Parse ());

			infectionPulseDelay = PULSE_DELAY_MAX * (1 - (coreSize - 1) / CORE_SIZE_MAX);
			InfectedCubeCount = Cubes.Length;
			ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreInfected;
		}
		else
		{
			InfectedCubeCount = 0;
			ChildCore.renderer.material = Blocked ? scrNodeMaster.Instance.MatCoreBlocked : scrNodeMaster.Instance.MatCoreUninfected;
		}

		totalCubeCount = Cubes.Length;

		GetComponent<BoxCollider2D>().size = new Vector2(coreSize * 4.5f, coreSize * 4.5f);
		ChildInfo.GetComponent<CircleCollider2D>().radius = coreSize * 0.5f;
	}

	public void MakeReady()
	{
		CellX = scrNodeMaster.ToCellSpace(transform.position.x);
		CellY = scrNodeMaster.ToCellSpace(transform.position.y);
		ready = true;
	}
	
	public void AddCube(LinkedListNode<GameObject> cube)
	{
		// Initialise the cube.
		scrCube cubeScript = cube.Value.GetComponent<scrCube>();
		cubeScript.Init (cube, this, transform.position, FullyInfected ? scrCube.DataState.INFECTED : Blocked ? scrCube.DataState.BLOCKED : scrCube.DataState.CLEAN);

		// Store the linked list node for this cube.
		Cubes[cubePositionIndex] = cube;

		// Get ready for the next cube.
		++cubePositionIndex;
	}

	public void RemoveCube(LinkedListNode<GameObject> cube)
	{
		for (int i = 0; i < Cubes.Length; ++i)
		{
			if (Cubes[i] == cube)
			{
				Cubes[i] = null;

				bool cubeInfected = cube.Value.GetComponent<scrCube>().State == scrCube.DataState.INFECTED;

				scrNodeMaster.Instance.DeactivateCube(cube);
				--totalCubeCount;

				bool changeState = false;
				if (cubeInfected)	// Destroy an infected cube, check for disinfection.
				{
					--InfectedCubeCount;

					// No longer infected.
					if (InfectedCubeCount == 0)
					{
						Infected = false;
						changeState = true;
					}
				}
				else // Destroyed a non-infected cube, check for full infection.
				{
					if (InfectedCubeCount == totalCubeCount && InfectedCubeCount != 0)
					{
						Infect (0);
						changeState = true;
					}
				}

				if (changeState)
				{
					if (!Infected)
					{
						if (Blocked)
						{
							ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreBlocked;
							scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.BLOCKED;
						}
						else
						{
							ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreUninfected;
							scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.CLEAN;
						}
					}
				}
				else // Partially infected.
				{
					ChildCore.renderer.material.color = Color.Lerp (Blocked ? scrNodeMaster.ColCoreBlocked : scrNodeMaster.ColCoreUninfected, scrNodeMaster.ColCoreInfected, GetInfectionAmount());
				}
				return;
			}
		}
	}

	public void ConvertToInfected(Message message)
	{
		Data = message;

		// Infect all cubes immediately.
		foreach (LinkedListNode<GameObject> cube in Cubes)
		{
			if (cube != null)
				cube.Value.GetComponent<scrCube>().InfectImmediate();
		}
		InfectedCubeCount = totalCubeCount;

		Infect (0);
	}

	public void Infect(int count)
	{
		Infected = true;

		for (int i = 0, infect = 0; i < Cubes.Length && infect < count; ++i)
		{
			if (Cubes[i] != null && Cubes[i].Value.GetComponent<scrCube>().State != scrCube.DataState.INFECTED)
			{
				Cubes[i].Value.GetComponent<scrCube>().Infect();
				++infect;
			}
		}

		if (InfectedCubeCount == totalCubeCount)
		{
			FullyInfected = true;
			
			// Read the text.
			StartCoroutine(Parse ());
			
			// Set the infected materials.
			ChildCore.renderer.material = scrNodeMaster.Instance.MatCoreInfected;
			
			scrNodeMaster.Instance.CreateLinks(Node);

			scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.INFECTED;
		}
		else
		{
			// Partially infected.
			if (Blocked)
				scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.INFECTING_BLOCKED;
			else
				scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.INFECTING_CLEAN;

			ChildCore.renderer.material.color = Color.Lerp (Blocked ? scrNodeMaster.ColCoreBlocked : scrNodeMaster.ColCoreUninfected, scrNodeMaster.ColCoreInfected, GetInfectionAmount());
		}
	}

	void InfectLinkedNodes()
	{
		for (int i = 0; i < CurrentLinks; ++i)
		{
			// Get the chess distance to the nodes. (REDUNDANT NOW CAN ONLY INFECT ADJACENT NODES!)
			//int chess = Mathf.Max ((int)Mathf.Abs ((linkedNodes[i].transform.position.x - transform.position.x) / scrNodeMaster.CELL_SIZE),
			                   //    (int)Mathf.Abs (linkedNodes[i].transform.position.y - transform.position.y) / scrNodeMaster.CELL_SIZE);

			linkedNodes[i].GetComponent<scrNode>().Infect(Mathf.CeilToInt((InfectedCubeCount * 0.05f)));
			links[i].SetColors(scrNodeMaster.ColCoreInfected,
			                   Color.Lerp(linkedNodes[i].GetComponent<scrNode>().Blocked ? scrNodeMaster.ColCoreBlocked : scrNodeMaster.ColCoreUninfected,
			                              scrNodeMaster.ColCoreInfected, linkedNodes[i].GetComponent<scrNode>().GetInfectionAmount()));
		}
	}

	public void Link(GameObject node)
	{
		if (CurrentLinks >= linkedNodes.Length)
			return;

		// Set the linked gameobject.
		linkedNodes[CurrentLinks] = node;

		// Set up the visual link.
		links[CurrentLinks].enabled = true;
		
		// Set the control point half way between the two points then pushed in a random direction perpendicular to the connecting line.
		float curve = 30.0f;
		Vector3 control = Vector3.Lerp (transform.position, node.transform.position, 0.5f) + curve * Vector3.back;
		
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

		// Get the time between revisions.
		string[] timesRaw = new string[2];

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

			if (string.IsNullOrEmpty(timesRaw[0]) &&
			    line.Contains("Revision as of "))
			{
				int index = line.IndexOf("Revision as of ");
				int end = line.IndexOf("</a>", index + 15);
				timesRaw[0] = line.Substring(index + 15, end - index - 15);
    		}
			else if (string.IsNullOrEmpty(timesRaw[1]) &&
			         line.Contains("Latest revision as of "))
			{
				int index = line.IndexOf("Latest revision as of ");
				int end = line.IndexOf("</a>", index + 22);
				timesRaw[1] = line.Substring(index + 22, end - index - 22);
			}


			if (++numLoops > LOOPS_PER_FRAME)
			{
				numLoops = 0;
				yield return new WaitForEndOfFrame();
			}
		}

		// Parse the times.
		System.DateTime[] timesParsed = new System.DateTime[2];
		for (int i = 0; i < 2; ++i)
		{
			int hour = int.Parse(timesRaw[i].Substring(0, 2));
			int minute = int.Parse(timesRaw[i].Substring(3, 2));
			int day = int.Parse(timesRaw[i].Substring(7, 2));
			int month = 0;
			switch(timesRaw[i].Substring(10, timesRaw[i].IndexOf(' ', 10) - 10))
			{
			case "January":
				month = 1;
				break;
			case "February":
				month = 2;
				break;
			case "March":
				month = 3;
				break;
			case "April":
				month = 4;
				break;
			case "May":
				month = 5;
				break;
			case "June":
				month = 6;
				break;
			case "July":
				month = 7;
				break;
			case "August":
				month = 8;
				break;
			case "September":
				month = 9;
				break;
			case "October":
				month = 10;
				break;
			case "November":
				month = 11;
				break;
			case "December":
				month = 12;
				break;
			}
			int year = int.Parse(timesRaw[i].Substring(timesRaw[i].LastIndexOf(' ') + 1));
			timesParsed[i] = new System.DateTime(year, month, day, hour, minute, 0);
		}

		ReversionTime = timesParsed[1] - timesParsed[0];

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

		foreach (string titleWord in Data.page_title.Split(split, System.StringSplitOptions.RemoveEmptyEntries))
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

				// First, choose if a random enemy will be made or a specific one. Specific enemies depend on word length.

				// Choose an enemy based on word type.
				if (scrEnemyMaster.Instance.Connectives.Contains(words[wordIndex])) // Connective enemy.
				{

				}

				// Choose an enemy based on word length.
				if (words[wordIndex].Length > 4) // 6+ letter enemy.
				{
					scrSnakeEnemy snake = ((GameObject)Instantiate (scrEnemyMaster.Instance.SnakeEnemyPrefab, transform.position, Quaternion.identity)).GetComponent<scrSnakeEnemy>();
					snake.Name = words[wordIndex];
					snake.Init ();
				}
				else // Character enemy.
				{
					scrWordEnemy word = ((GameObject)Instantiate (scrEnemyMaster.Instance.WordEnemyPrefab, transform.position, Quaternion.identity)).GetComponent<scrWordEnemy>();
					word.Name = words[wordIndex];
					word.Init();
				}

				++wordIndex;
				if (wordIndex == words.Length)
					wordIndex = 0;

				// Set the spawn delay based on the amount of letters in the word.
				spawnDelay = (spawnDelayPerCharacter * words[wordIndex].Length) / (scrMaster.Instance.Wave + 1);
			}

			spawnTimer = 0;
		}
	}

	void SpawnBoss()
	{
		string bossTime = ReversionTime.Days + "d " + ReversionTime.Hours + "h " + ReversionTime.Minutes + "m ";

		
	}

	public float GetInfectionAmount()
	{
		if (Cubes != null)
			return (float)InfectedCubeCount / totalCubeCount;

		return 0;
	}

	// Use this for initialization
	void Start ()
	{
		//Enemies = new List<scrEnemy>();
		ChildCore = transform.Find ("Core").gameObject;
		ChildInfo = transform.Find ("Info").gameObject;
		ChildData = new GameObject[4];
		ChildData[0] = ChildInfo.transform.Find ("PageTitle").gameObject;
		ChildData[1] = ChildInfo.transform.Find ("User").gameObject;
		ChildData[2] = ChildInfo.transform.Find ("ChangeSize").gameObject;
		ChildData[3] = ChildInfo.transform.Find ("Time").gameObject;

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

		VisibleToPlayer = !(scrPlayer.Instance.CellX > CellX + 2 || scrPlayer.Instance.CellX < CellX - 2 ||
		                    scrPlayer.Instance.CellY > CellY + 1 || scrPlayer.Instance.CellY < CellY - 1);

		if (VisibleToPlayer)
			ChildCore.transform.Rotate(0, 0, Time.deltaTime * 20);

		if (expandTimer < expandDuration)
		{
			expandTimer += Time.deltaTime;
			if(expandTimer > expandDuration)
				expandTimer = expandDuration;

			// Expand core.
			//transform.localScale = Vector3.Lerp (Vector3.zero, Vector3.one, expandTimer / expandDuration);

			// Expand out cubes.
			for (int i = 0; i < Cubes.Length; ++i)
			{
				if (Cubes[i] != null)
					Cubes[i].Value.transform.position = Vector3.Lerp (transform.position, transform.TransformPoint(cubePositionEnumerator.Current[i]), expandTimer / expandDuration);
			}
		}
		else
		{
			// Also check for destruction, but otherwise keep rotating.
			if (VisibleToPlayer)
			{
				float dRot = 30 / ChildCore.transform.localScale.x * Time.deltaTime;
				ChildInfo.transform.Rotate(Vector3.forward, dRot);
				for (int i = 0; i < Cubes.Length; ++i)
				{
					if (Cubes[i] != null)
					{
						Cubes[i].Value.transform.RotateAround(transform.position, Vector3.forward, dRot);
					}
				}
			}

			// Destroy if necessary.
			if (totalCubeCount == 0)
			{
				if (Uploading)
				{
					Uploading = false;
					scrNodeMaster.SelectNewNode = true;
				}

				GameObject explosion = (GameObject)Instantiate (ExplosionPrefab, transform.position, Quaternion.identity);
				explosion.particleSystem.startColor = Color.Lerp (ChildCore.renderer.material.color, Color.white, 0.5f);

				scrNodeMaster.CellStates[CellX, CellY] = scrNodeMaster.CellState.FREE;
				scrNodeMaster.Instance.DeactivateNode(Node);
				return;
			}

			if (FullyInfected)
			{
				SpawnEnemies();

				bossMakeTimer += Time.deltaTime;
				if (bossMakeTimer >= bossMakeDuration)
				{
					SpawnBoss();
					foreach (LinkedListNode<GameObject> c in Cubes)
					{
						if (c != null && c.Value != null && c.Value.activeSelf)
							c.Value.GetComponent<scrCube>().DestroyImmediate();
					}
				}

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
						                   Color.Lerp (Color.clear,linkedNodes[i].GetComponent<scrNode>().Blocked ? scrNodeMaster.ColCoreBlocked : scrNodeMaster.ColCoreUninfected,
						            				   (linkExpandTimers[i] - halfLinkExpandDuration) / halfLinkExpandDuration));
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
						                   Color.Lerp (Color.clear, scrNodeMaster.ColCoreInfected, (linkExpandTimers[i] - halfLinkExpandDuration) / halfLinkExpandDuration));
					}
				}

				infectionPulseTimer += Time.deltaTime;
				if (infectionPulseTimer > infectionPulseDelay)
				{
					infectionPulseTimer = 0;
					InfectLinkedNodes();
				}
			}
		}
	}

	public IEnumerator Upload()
	{		
		Uploading = true;

		audio.Play();

		// "Upload" all cubes over time.
		for (int i = 0; i < Cubes.Length; ++i)
		{
			if (Cubes[i] != null)
			{
				--totalCubeCount;
				Cubes[i].Value.GetComponent<scrCube>().Upload();
				Cubes[i] = null;
				audio.PlayOneShot(UploadCubeSound);
				yield return new WaitForSeconds(0.5f);
			}
		}

		// Give the audio a buffer time in case a cube sound is still playing.
		yield return new WaitForSeconds(0.6f);

		scrNodeMaster.SelectNewNode = true;
		Uploading = false;
	}

	/*void OnGUI()
	{
		string text = Data.time + System.Environment.NewLine + "PAGE: " + Data.page_title + System.Environment.NewLine + "USER: " + Data.user +
					  System.Environment.NewLine + "EDIT: " + Data.change_size.ToString() + " bytes";

		scrGUI.Instance.DrawOutlinedText(text, Camera.main.WorldToViewportPoint(transform.position), Color.white, Color.black, 1);

	}*/
}
