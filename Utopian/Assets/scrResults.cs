using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class scrResults : MonoBehaviour
{
	// Quick 'n' dirty global variables because of time pressure.
	public static float TimeStart = 0, TimeEnd = 0;

	public static int RegisteredEditCount = 0;
	public static int AnonymousEditCount = 0;
	public static int BotEditCount = 0;
	public static List<Message> ReversionEdits = new List<Message>();

	public static string Killer = "";	// User that killed the player.
	public static List<string> Users = new List<string>();	// Get the url with http://www.wikipedia.org/wiki/User:" + user + "\" target=\"_blank".

	public static void Clear()
	{
		TimeStart = 0;
		TimeEnd = 0;
		RegisteredEditCount = 0;
		AnonymousEditCount = 0;
		BotEditCount = 0;
		Killer = "";
		Users.Clear();
		ReversionEdits.Clear ();
	}

	public Transform WikiPanel;
	
	void Start()
	{
		Screen.lockCursor = false;

		WikiPanel.Find("Title").GetComponent<Text>().text = "Over the last " + (int)((TimeEnd - TimeStart) / 60) + "m" + (int)((TimeEnd - TimeStart) % 60) + "s:";
		WikiPanel.Find ("TotalEdits").GetComponent<Text>().text = (RegisteredEditCount + AnonymousEditCount + BotEditCount) + " edits were made to Wikipedia articles!";
		WikiPanel.Find ("VandalEdits").GetComponent<Text>().text = ReversionEdits.Count + " edits were reversions, possibly to vandalism!";
		WikiPanel.Find ("TotalUsers").GetComponent<Text>().text = Users.Count + " new users joined Wikipedia!";


		Transform edits = WikiPanel.Find ("TotalEdits");
		edits.Find ("Registered").GetComponent<Text>().text = RegisteredEditCount + " were made by registered users.";
		edits.Find ("Anonymous").GetComponent<Text>().text = AnonymousEditCount + " were made by anonymous users.";
		edits.Find ("Bot").GetComponent<Text>().text = BotEditCount + " were made by bots.";

		PopulateReversions();
		PopulateUsers();
	}

	void Update()
	{

	}

	void PopulateReversions()
	{
		// Populate reversions.
		Transform reversionPanel = WikiPanel.Find ("VandalEdits").Find ("ScrollRect").Find ("ReversionPanel");

		// Get the reversion template.
		Transform reversion = reversionPanel.Find ("Reversion");

		// Duplicate the first reversion to fill the rest of the text items.
		for (int i = 0; i < ReversionEdits.Count; ++i)
		{
			GameObject r = (GameObject)Instantiate(reversion.gameObject, reversion.position + new Vector3(0, -i * (reversion.GetComponent<RectTransform>().sizeDelta.y + 4), 0), reversion.rotation); 
			r.transform.SetParent(reversionPanel);
			r.transform.localScale = Vector3.one;
			r.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 32); 

			// Change reversion content.
			Message m = ReversionEdits[i];
			Text txt = r.GetComponentInChildren<Text>();
			if (m.is_bot)
			{
				txt.text = m.page_title + " was reverted by a bot.";
			}
			else if (m.is_anon)
			{
				txt.text = m.page_title + " was reverted by an anonymous user.";
			}
			else
			{
				txt.text = m.page_title + " was reverted by " + m.user + ".";
			}

			r.GetComponent<Button>().onClick.AddListener(delegate{OpenURL(m.url);});
		}

		// Set the height of the reversion panel to scale with the amount of reversions.
		reversionPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, Mathf.Max ((reversion.GetComponent<RectTransform>().sizeDelta.y + 4) * (ReversionEdits.Count), reversionPanel.parent.GetComponent<RectTransform>().sizeDelta.y));

		// Destroy the template.
		Destroy (reversion.gameObject);
	}

	void PopulateUsers()
	{
		// Populate users.
		Transform userPanel = WikiPanel.Find ("TotalUsers").Find ("ScrollRect").Find ("UserPanel");
		
		// Get the user template.
		Transform user = userPanel.Find ("User");
		
		// Duplicate the first user to fill the rest of the text items.
		for (int i = 0; i < Users.Count; ++i)
		{
			GameObject u = (GameObject)Instantiate(user.gameObject, user.position + new Vector3(0, -i * (user.GetComponent<RectTransform>().sizeDelta.y + 4), 0), user.rotation); 
			u.transform.SetParent(userPanel);
			u.transform.localScale = Vector3.one;
			u.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 32); 
			
			// Change user content.
			u.GetComponentInChildren<Text>().text = Users[i];
			u.GetComponent<Button>().onClick.AddListener(delegate{OpenURL("http://www.wikipedia.org/wiki/User:" + Users[i] + "\" target=\"_blank");});
		}
		
		// Set the height of the user panel to scale with the amount of users.
		userPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, Mathf.Max ((user.GetComponent<RectTransform>().sizeDelta.y + 4) * (Users.Count), userPanel.parent.GetComponent<RectTransform>().sizeDelta.y));
		
		// Destroy the template.
		Destroy (user.gameObject);
	}

	public void OpenURL(string url)
	{
		Application.OpenURL(url);
	}

}
