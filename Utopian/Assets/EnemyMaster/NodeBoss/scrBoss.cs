using UnityEngine;
using System.Collections;

public class scrBoss : MonoBehaviour
{
	const int LOOPS_PER_FRAME = 100;	// Number of loops allowed per frame of a coroutine before yielding.

	public Message Data { get; private set; }
	public System.TimeSpan ReversionTime { get; private set; }

	// Use this for initialization
	void Start ()
	{

	}
	
	// Update is called once per frame
	void Update ()
	{
	
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
		
		ReversionTime = timesParsed[1] - timesParsed[0];

		transform.Find("Time").GetComponent<TextMesh>().text = ReversionTime.Days + "d " + ReversionTime.Hours + "h " + ReversionTime.Minutes + "m ";
	}
}
