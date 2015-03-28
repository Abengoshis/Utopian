﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebSocketSharp;

public class GeoData
{
	public string areacode;
	public string city;
	public string country_code;
	public string country_name;
	public string ip;
	public double latitude;
	public double longitude;
	public string metro_code;
	public string region_code;
	public string region_name;
	public string zipcode;
}

// Record for useful parts of a message. Not all messages contain the same stuff, so converting directly to an object like I do with the GeoData is probably a bad idea.
public struct Message
{
	public string page_title;
	public string summary;
	public string url;
	public int change_size;
	public string user;
	public bool is_anon;
	public bool is_bot;
	public string time;

	// The geographical data, if it exists.
	public GeoData geo;
}



public class scrWebSocketClient : MonoBehaviour
{
	public bool Connected { get; private set; }
	public bool Failed { get; private set; }

	public static scrWebSocketClient Instance { get; private set; }

	WebSocket client;

	// Stack of messages read by the client.
	Queue<Message> messagesAccumulated = new Queue<Message>();

	void Start ()
	{
		Instance = this;

		Connected = false;
		Failed = false;

		/* Create the WebSocket client, set up its events, then connect it. */

		client = new WebSocket("ws://wikimon.hatnote.com/en/");
		
		client.OnOpen += (sender, e) => 
		{
			Debug.Log ("Connection established.");
			Connected = true;
		};

		client.OnError += (sender, e) => 
		{
			Debug.Log ("WebSocket Error: " + e.Message);
			Failed = true;
		};

		client.OnClose += (sender, e) => 
		{
			Debug.Log ("Connection terminated.");
			Connected = false;
			Failed = true;
		};

		client.OnMessage += (sender, e) =>
		{
			if (e.Type == Opcode.Text)
			{
				ReadMessage(e.Data);
				return;
			}

			if (e.Type == Opcode.Binary)
			{
				return;
			}
		};

		client.Connect();
	}

	public void Close()
	{
		client.Close();
	}

	void OnDestroy()
	{
		client.Close();
	}

	void Update()
	{
		// Dump a message into the feed every frame.
		while (messagesAccumulated.Count != 0)
		{
			Message m = messagesAccumulated.Dequeue();
			if (m.page_title != null)
				scrNodeMaster.Instance.ReceiveMessage(m);
			else
				scrEnemyMaster.Instance.ReceiveMessage(m);
		}

	}
	
	void ReadMessage(string data)
	{
		try
		{
			Dictionary<string, object> messageData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

			// Check for new users.
			if ((string)messageData["page_title"] == "Special:Log/newusers")
			{
				Message message = new Message();
				message.user = (string)messageData["user"];
				messagesAccumulated.Enqueue(message);
				scrResults.Users.Add (message.user);
			}
			else
			{
				// Filter to only "Main" value of "ns", which are normal page edits. 
				if ((string)messageData["ns"] == "Main" && (string)messageData["action"] == "edit")
				{
					// Create a message from the message data.
					Message message = new Message();
					message.page_title = (string)messageData["page_title"];
					message.summary = (string)messageData["summary"];
					message.url = (string)messageData["url"];
					message.change_size = System.Convert.ToInt32(messageData["change_size"]);
					message.user = (string)messageData["user"];
					message.is_anon = (bool)messageData["is_anon"];
					message.is_bot = (bool)messageData["is_bot"];

					// Get the time the message was sent.
					message.time = System.DateTime.Now.ToString("HH:mm");

					if (messageData.ContainsKey("geo_ip"))
					{
						message.geo = JsonConvert.DeserializeObject<GeoData>(messageData["geo_ip"].ToString());
					}
					else
					{
						message.geo = null;
					}

					messagesAccumulated.Enqueue (message);

					if (message.is_bot)
						++scrResults.BotEditCount;
					else if (message.is_anon)
						++scrResults.AnonymousEditCount;
					else
						++scrResults.RegisteredEditCount;
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.Log(e.Message);
		}
	}
}
