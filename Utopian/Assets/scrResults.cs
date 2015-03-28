using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


// This class could be made much better. Future me, start by removing all the .Find(...)'s.
public class scrResults : MonoBehaviour
{
	// Quick 'n' dirty global variables because of time pressure.
	public static float TimeElapsed = 0;

	public static int RegisteredEditCount = 0;
	public static int AnonymousEditCount = 0;
	public static int BotEditCount = 0;
	public static List<Message> ReversionEdits = new List<Message>();

	public static string Killer = "";	// User that killed the player.
	public static List<string> Users = new List<string>();	// Get the url with http://www.wikipedia.org/wiki/User:" + user + "\" target=\"_blank".

	public static int Score = 0;

	bool animationFinished = false;

	public static void Clear()
	{
		Score = 0;
		TimeElapsed = 0;
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

		WikiPanel.Find("Title").GetComponent<scrScrollText>().text = "Over the last " + (int)(TimeElapsed / 60) + "m" + (int)(TimeElapsed % 60) + "s:";
		WikiPanel.Find ("TotalEdits").GetComponent<scrScrollText>().text = (RegisteredEditCount + AnonymousEditCount + BotEditCount) + " edits were made to Wikipedia articles!";

		if (scrMainMenu.RidiculousMode)
			WikiPanel.Find ("VandalEdits").GetComponent<scrScrollText>().text = ReversionEdits.Count + " edits were reversions (vandalism shown in red)!";
		else
			WikiPanel.Find ("VandalEdits").GetComponent<scrScrollText>().text = ReversionEdits.Count + " edits were reversions of vandalism!";


		WikiPanel.Find ("TotalUsers").GetComponent<scrScrollText>().text = Users.Count + " new users joined Wikipedia!";


		Transform edits = WikiPanel.Find ("TotalEdits").Find ("Values");
		edits.Find ("Registered").GetComponent<Text>().text = RegisteredEditCount + " were made by registered users.";
		edits.Find ("Anonymous").GetComponent<Text>().text = AnonymousEditCount + " were made by anonymous users.";
		edits.Find ("Bot").GetComponent<Text>().text = BotEditCount + " were made by bots.";

		PopulateReversions();
		PopulateUsers();

		WikiPanel.Find ("TotalEdits").Find ("Values").gameObject.SetActive(false);
		WikiPanel.Find ("VandalEdits").Find ("Values").gameObject.SetActive(false);
		WikiPanel.Find ("TotalUsers").Find ("Values").gameObject.SetActive(false);

		StartCoroutine(RunTextAnimations());
	}

	void Update()
	{
		if (animationFinished)
		{
			if (Input.GetButtonDown("Cancel"))
			{
				Application.LoadLevel(0);
			}
		}

		if (Input.anyKeyDown)
		{

		}
	}
	
	/// <summary>
	/// Helper method for RunTextAnimations().
	/// </summary>
	void RunScrollText(Transform objectWithScrollText)
	{
		scrScrollText scroll = objectWithScrollText.GetComponent<scrScrollText>();
		if (scroll != null)
			scroll.Run ();
	}

	IEnumerator RunTextAnimations()
	{
		yield return new WaitForSeconds(1.0f);
		RunScrollText(WikiPanel.Find ("Title"));
		yield return new WaitForSeconds(1.0f);

		RunScrollText(WikiPanel.Find ("TotalEdits"));
		yield return new WaitForSeconds(2.1f);
		WikiPanel.Find ("TotalEdits").Find ("Values").gameObject.SetActive(true);
		audio.PlayOneShot(audio.clip);
		yield return new WaitForSeconds(0.5f);

		RunScrollText(WikiPanel.Find ("VandalEdits"));
		yield return new WaitForSeconds(2.5f);
		audio.PlayOneShot(audio.clip);
		WikiPanel.Find ("VandalEdits").Find ("Values").gameObject.SetActive(true);
		yield return new WaitForSeconds(0.5f);


		RunScrollText(WikiPanel.Find ("TotalUsers"));
		yield return new WaitForSeconds(1.6f);
		audio.PlayOneShot(audio.clip);
		WikiPanel.Find ("TotalUsers").Find ("Values").gameObject.SetActive(true);
		yield return new WaitForSeconds(0.5f);

		animationFinished = true;
	}

	void PopulateReversions()
	{
		// Populate reversions.
		Transform reversionPanel = WikiPanel.Find ("VandalEdits").Find("Values").Find ("ScrollRect").Find ("ReversionPanel");

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

			if (!string.IsNullOrEmpty(m.summary) && m.summary.ToUpper().Contains("VANDAL") || m.summary.ToUpper().Contains("SPAM"))
			{
				txt.color = Color.red;
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
		Transform userPanel = WikiPanel.Find ("TotalUsers").Find ("Values").Find ("ScrollRect").Find ("UserPanel");
		
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
			string username = Users[i];
			u.GetComponentInChildren<Text>().text = username;
			u.GetComponent<Button>().onClick.AddListener(delegate{OpenURL("http://www.wikipedia.org/w/index.php?title=User_talk:" + username + "&action=edit&section=new");});
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
