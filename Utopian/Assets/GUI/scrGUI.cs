using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrGUI : MonoBehaviour
{
	public static scrGUI Instance { get; private set; }
	public static Canvas GUICanvas { get; private set; }

	public GUISkin Skin;

	GameObject[] feedItems;


	// Use this for initialization
	void Start ()
	{
		Instance = this;
		GUICanvas = this.GetComponent<Canvas>();

		feedItems = new GameObject[10];
		feedItems[0] = transform.Find ("FeedItem").gameObject;
		for (int i = 1; i < feedItems.Length; ++i)
		{
			feedItems[i] = (GameObject)Instantiate(feedItems[0], feedItems[i - 1].transform.position - new Vector3(0, 18, 0), Quaternion.identity);
			feedItems[i].transform.SetParent(transform);
			feedItems[i].transform.localScale = feedItems[0].transform.localScale;
			feedItems[i].GetComponent<Text>().color = Color.white * (1 - (float)i / feedItems.Length);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		transform.Find ("Cursor").position = Camera.main.WorldToScreenPoint(scrPlayer.Instance.SimulatedCursorPosition);


	}

	public void AddToFeed(string text)
	{
		for (int i = feedItems.Length - 1; i > 0 ;--i)
		{
			feedItems[i].GetComponent<Text>().text = feedItems[i - 1].GetComponent<Text>().text;
		}
		feedItems[0].GetComponent<Text>().text = text;
	}

	public void DrawOutlinedText(string text, Vector3 viewportPosition, Color interior, Color outline, float thickness)
	{
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
