using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class scrGUI : MonoBehaviour
{
	public static scrGUI Instance { get; private set; }
	public static Canvas GUICanvas { get; private set; }
	public static Canvas WorldCanvas { get; private set; }

	public GUISkin Skin;

	Text[] feedItems;

	GameObject powerupItemTemplate;
	List<Text> powerupItems = new List<Text>();


	// Use this for initialization
	void Start ()
	{
		Instance = this;
		GUICanvas = this.GetComponent<Canvas>();

		feedItems = new Text[20];
		feedItems[0] = transform.Find ("FeedItem").gameObject.GetComponent<Text>();
		for (int i = 1; i < feedItems.Length; ++i)
		{
			feedItems[i] = ((GameObject)Instantiate(feedItems[0].gameObject, feedItems[i - 1].transform.position - new Vector3(0, 18, 0), Quaternion.identity)).GetComponent<Text>();
			feedItems[i].transform.SetParent(transform);
			feedItems[i].transform.localScale = feedItems[0].transform.localScale;
		}

		powerupItemTemplate = transform.Find ("PowerupItem").gameObject;
		powerupItemTemplate.SetActive(false);
	}
	
	// Update is called once per frame
	void Update ()
	{
		transform.Find ("Cursor").position = Camera.main.WorldToScreenPoint(scrPlayer.Instance.SimulatedCursorPosition);


	}

	public void AddToFeed(string text, Color colour)
	{
		for (int i = feedItems.Length - 1; i > 0 ;--i)
		{
			feedItems[i].GetComponent<Text>().text = feedItems[i - 1].GetComponent<Text>().text;
			feedItems[i].GetComponent<Text>().color = feedItems[i - 1].GetComponent<Text>().color * (1 - (float)i / (feedItems.Length * 10));
		}
		feedItems[0].GetComponent<Text>().text = text;
		feedItems[0].GetComponent<Text>().color = colour;
	}

	public void EnqueuePowerup(string text, Color colour)
	{
		Text p = ((GameObject)Instantiate(powerupItemTemplate)).GetComponent<Text>();
		p.gameObject.SetActive(true);
		p.transform.parent = transform;
		p.text = text;
		p.color = colour;
		if (powerupItems.Count != 0)
			p.transform.position = powerupItems[powerupItems.Count - 1].transform.position - new Vector3(0, 18, 0);
		else
			p.transform.position = powerupItemTemplate.transform.position;
		p.transform.localScale = powerupItemTemplate.transform.localScale;
		powerupItems.Add(p);
	}

	public void DequeuePowerup()
	{
		Destroy (powerupItems[0].gameObject);
		powerupItems.RemoveAt(0);
		for (int i = 0; i < powerupItems.Count; ++i)
			powerupItems[i].transform.position = powerupItemTemplate.transform.position - new Vector3(0, 18 * i, 0);
	}

	public void DrawOutlinedText(string text, Vector3 viewportPosition, Color interior, Color outline, float thickness)
	{
		if (viewportPosition.x < -0.5f || viewportPosition.x > 1.5f || viewportPosition.y < -0.5f || viewportPosition.x > 1.5f)
			return;

		float screenScale = Screen.height / 720.0f;
		GUI.skin = scrGUI.Instance.Skin;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
		                           new Vector3(screenScale, screenScale, 1));
		
		
		Vector3 screenPosition = viewportPosition;
		screenPosition.x *= 1280;
		screenPosition.y *= 720;
		Rect position = new Rect(screenPosition.x - 128, 720 - screenPosition.y - 128, 256, 256);
		
		// Draw the offset outline labels.
		GUI.color = outline;
		position.x += thickness;
		GUI.Label(position, text);
		position.x += 2 * thickness;
		GUI.Label(position, text);
		position.x -= thickness;
		position.y -= thickness;
		GUI.Label(position, text);
		position.y += 2 * thickness;
		GUI.Label(position, text);
		position.y -= thickness;
		
		// Draw the central label.
		GUI.color = interior;
		GUI.Label(position, text);
	}
}
