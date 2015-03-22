using UnityEngine;
using System.Collections;

public class scrBoss : scrEnemy
{
	const int LOOPS_PER_FRAME = 100;	// Number of loops allowed per frame of a coroutine before yielding.

	public Message Data { get; private set; }
	public System.TimeSpan ReversionTime { get; private set; }

	public GameObject ChildCore;
	public GameObject ChildShell;

	private scrPathfinder pathfinder;
	private LineRenderer link;

	private float fireRate = 0.0f;
	private float fireTimer = 0.0f;
	private int fireCannon = 0;
	private float rotateSpeed = 0.0f;
	private float orbitTimer = 0.0f;
	private float startAngle = 0.0f;

	// Use this for initialization
	protected override void Start ()
	{
		transform.localScale = Vector3.zero;
		pathfinder = GetComponent<scrPathfinder>();
		pathfinder.Target = scrAICore.Instance.gameObject;
		pathfinder.Resume();
		link = GetComponent<LineRenderer>();

		base.Start();
	}
	
	// Update is called once per frame
	protected override void Update ()
	{
		if (transform.localScale.x == 0.0f) return;

		if (startAngle != 0.0f || Vector3.Distance(transform.position, pathfinder.Target.transform.position) <= 50.0f)
		{
			// Initialisation of orbit.
			if (startAngle == 0.0f)
			{
				Vector3 direction = pathfinder.Target.transform.position - transform.position;
				startAngle = Mathf.Atan2(-direction.y, direction.x) - Mathf.PI * 0.5f;
			}

			pathfinder.Pause();
			transform.position = pathfinder.Target.transform.position + new Vector3(50.0f * Mathf.Sin (startAngle + orbitTimer * Mathf.PI * 2), 50.0f * Mathf.Cos (startAngle + orbitTimer * Mathf.PI * 2), 0.0f);
			orbitTimer += Time.deltaTime * rotateSpeed * 0.001f;


			if (ChildCore.transform.localScale.x > 0.0f)
			{
				ChildCore.transform.localScale = Vector3.one * (ChildCore.transform.localScale.x - Time.deltaTime / transform.localScale.x * 0.05f);
				scrAICore.Instance.Learn (true, Time.deltaTime / transform.localScale.x);

				if (ChildCore.transform.localScale.x <= 0.0f)
				{
					ChildCore.transform.localScale = Vector3.zero;
					link.SetVertexCount(0);
				}
				else
				{
					// Create a curved link like the nodes.
					float curve = 20.0f;
					Vector3 control = Vector3.Lerp (transform.position, pathfinder.Target.transform.position, 0.5f) - curve * Vector3.back;
					for (int i = 0; i < 5; ++i)
					{
						float t = (float)i / (5 - 1);
						float tInv = 1.0f - t;
						link.SetPosition(i, tInv * tInv * transform.position + 2 * tInv * t * control + t * t * pathfinder.Target.transform.position);
					}

					link.SetColors(Color.Lerp (Color.clear, Color.red, Mathf.Abs (orbitTimer) / 0.05f), Color.Lerp (Color.clear, Color.red, Mathf.Abs (orbitTimer) / 0.2f));
				}
			}
			else
			{
				// Fade out link.
			}
			
		}
		else
		{
			pathfinder.Resume();
		}

		// Rotate the shell.
		ChildShell.transform.Rotate (0.0f, rotateSpeed * Time.deltaTime, 0.0f);

		if (Vector2.Distance(transform.position, scrPlayer.Instance.transform.position) < 70)
		{
			fireTimer += Time.deltaTime * fireRate;
			if (fireTimer > 1.0f)
			{
				fireTimer = 0.0f;
				Vector3 position = ChildShell.transform.TransformPoint(0.5f * new Vector3(Mathf.Sin (fireCannon / 8.0f * Mathf.PI * 2),
				                                                       0.0f, Mathf.Cos (fireCannon / 8.0f * Mathf.PI * 2)));
				scrEnemyMaster.BulletPool.Create(new BulletPowerup(Color.red, 40.0f, 10.0f * transform.localScale.x, 0, 0, 0, false, true),
				                                 position, (position - transform.position).normalized, false, true);
				++fireCannon;
				if (fireCannon == 8)
					fireCannon = 0;
			}
		}

		base.Update ();
	}

	public void Init(Message data)
	{
		Data = data;

		Transform ChildInfo = transform.Find ("Info");
		ChildInfo.Find ("PageTitle").GetComponent<TextMesh>().text = Data.page_title;
		ChildInfo.Find ("User").GetComponent<TextMesh>().text = "User: " + Data.user;
		ChildInfo.Find ("ChangeSize").GetComponent<TextMesh>().text = "Altered: " + (Data.change_size > 0 ? ("+" + Data.change_size.ToString()) : Data.change_size.ToString()) + " Bytes";
		ChildInfo.Find ("Time").GetComponent<TextMesh>().text = "Edited Time: " + Data.time;

		StartCoroutine(Parse());
	}

	void SetValuesBasedOnTime()
	{
		if (ReversionTime.TotalMinutes < 10.0f)
		{
			transform.localScale = Vector3.one * 0.5f;
			rotateSpeed = 20.0f;
			fireRate = 3.0f;
		}
		else if (ReversionTime.TotalMinutes < 30.0f)
		{
			transform.localScale = Vector3.one * 0.6f;
			rotateSpeed = -20.0f;
			fireRate = 2.0f;
		}
		else if (ReversionTime.TotalHours < 1.0f)
		{
			transform.localScale = Vector3.one * 0.7f;
			rotateSpeed = 10.0f;
			fireRate = 1.0f;
		}
		else if (ReversionTime.TotalHours < 12.0f)
		{
			transform.localScale = Vector3.one * 0.8f;
			rotateSpeed = -10.0f;
			fireRate = 0.5f;
		}
		else if (ReversionTime.TotalDays < 1.0f)
		{
			transform.localScale = Vector3.one * 0.9f;
			rotateSpeed = 5.0f;
			fireRate = 0.3f;
		}
		else
		{
			rotateSpeed = -5.0f;
			fireRate = 0.1f;
		}
		DamageToDestroy = transform.localScale.x * 200.0f;

		pathfinder.Speed = Mathf.Abs (rotateSpeed) * 0.5f;
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
		foreach (string line in lines)
		{
			if (string.IsNullOrEmpty(timesRaw[0]) &&
			    line.Contains("Revision as of"))
			{
				int index = line.IndexOf("Revision as of");
				int end = line.IndexOf("</a>", index + 15);
				timesRaw[0] = line.Substring(index + 15, end - index - 15);
			}

			if (string.IsNullOrEmpty(timesRaw[1]) &&
			         line.Contains("Latest revision as of"))
			{
				int index = line.IndexOf("Latest revision as of");
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
			if (string.IsNullOrEmpty(timesRaw[i]))
			{
				Debug.Log ("time raw" + i);
				continue;
			}

			int hour = int.Parse(timesRaw[i].Substring(0, 2));
			int minute = int.Parse(timesRaw[i].Substring(3, 2));
			int day = 0;
			if (timesRaw[i][8] == ' ')
				day = int.Parse(timesRaw[i].Substring(7, 1));
			else
				day = int.Parse(timesRaw[i].Substring(7, 2));
			int month = 0;
			int monthStartIndex = day < 10 ? 9 : 10;
			switch(timesRaw[i].Substring(monthStartIndex, timesRaw[i].IndexOf(' ', monthStartIndex) - monthStartIndex))
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
			int year = int.Parse(timesRaw[i].Substring(timesRaw[i].IndexOf(' ', monthStartIndex) + 1, 4));
			if (month == 0)
				Debug.Log (page.text);
			timesParsed[i] = new System.DateTime(year, month, day, hour, minute, 0);
		}

		Debug.Log (Data.page_title + " has been completed.");

		ReversionTime = timesParsed[1] - timesParsed[0];
		SetValuesBasedOnTime();

		transform.Find("Time").GetComponent<TextMesh>().text = ReversionTime.Days + "d " + ReversionTime.Hours + "h " + ReversionTime.Minutes + "m ";
	}
}
