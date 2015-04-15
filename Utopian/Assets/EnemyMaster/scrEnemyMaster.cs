using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class scrEnemyMaster : MonoBehaviour
{
	public static scrEnemyMaster Instance { get; private set; }
	public static scrBulletPool BulletPool { get; private set; }
	
	public GameObject UserEnemyPrefab;
	public GameObject SnakeEnemyPrefab;
	public GameObject WordEnemyPrefab;

	Queue<Message> messageQueue = new Queue<Message>();

	// Use this for initialization
	void Start ()
	{
		Instance = this;
		BulletPool = GetComponent<scrBulletPool>();

		enabled = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		// Handle generation of user enemies.
		if (messageQueue.Count != 0)
		{
			// Get the message.
			Message message = messageQueue.Dequeue ();

			// Instantiate a user enemy at a free position outside the user's vision.
			scrUserEnemy userEnemy = ((GameObject)Instantiate(UserEnemyPrefab, scrNodeMaster.Instance.GetRandomFreePosition(true), Quaternion.identity)).GetComponent<scrUserEnemy>();
			userEnemy.Name = message.user;
			userEnemy.Init();
		}
	}

	public void ReceiveMessage(Message message)
	{
		messageQueue.Enqueue(message);
	}
}
