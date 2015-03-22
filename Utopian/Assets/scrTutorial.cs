using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scrTutorial : MonoBehaviour
{
	public scrScrollText TutorialText;

	private string[] phrases = new string[] { "CONTROLS",
		"Use W,A,S,D to move and the LMB to shoot.",
		"OBJECTIVES - EDITS",
		"Live Wikipedia edits from registered users show up as DATA CLUSTERS.",
		"These can be seen on the FEED and MAP on the left of the screen.",
		"During the game, the CENTRAL AI selects DATA CLUSTERS at random and downloads them in sequence.",
		"By downloading data, the AI's INTELLIGENCE increases.",
		"The game is split into WAVES. At the end of each WAVE, the AI purges everything from the grid to make way for new data.",
		"There are three types of DATA CLUSTER: CLEAN (cyan), WASTE (grey), and INFECTED (orange).",
		"CLEAN clusters contribute to the AI's INTELLIGENCE. INFECTED clusters CORRUPT the AI, and can also increase INTELLIGENCE.  WASTE clusters do not contribute anything.",
		"INFECTED clusters form when an edit is a REVERSION. These infect nearby clusters over time and release infected ENEMIES based on the words in the edit.",
		"The time it took the reverted edit to be identified and reverted manifests itself as a separate enemy which will try to upload itself to the AI directly.",
		"OBJECTIVES - USERS",
		"Whenever a USER registers to Wikipedia, they appear as a USER ENEMY somewhere on the grid.",
		"USER ENEMIES have unique weapons determined by the username of the USER. Destroying a USER ENEMY grants the player limited use of its weapon.",
		"The goal of the game is to increase the AI's INTELLIGENCE while keeping its CORRUPTION as low as possible.",
		"GOOD LUCK!"
	};
	private float[] phraseDurations = new float[] { 5, 6, 4, 5, 5, 7, 5, 9, 8, 10, 10, 10, 4, 7, 9, 7, 3 };
	private float phraseTimer = 0.0f;
	private int phrase = 0;

	void Start ()
	{
		TutorialText.ChangeText(phrases[0]);
	}

	void Update ()
	{
		phraseTimer += Time.deltaTime;
		if (phraseTimer >= phraseDurations[phrase])
		{
			phraseTimer = 0;
			++phrase;

			if (phrase == phrases.Length)
			{
				scrMaster.Instance.ItsGameOverMan();
			}
			else
			{
				Message[] testMessages;
				switch(phrase)
				{
				case 3:

					for (int i = 0; i < 6; ++i)
					{
						Message message = new Message();
						message.change_size = 100 * (i + 1);
						message.page_title = "Title of Page Edited";
						message.user = "Username of Editor";
						message.time = "Time Edited";
						scrNodeMaster.Instance.ReceiveMessage(message);
					}
					break;
				case 7:
					StartCoroutine(scrNodeMaster.Instance.Purge());
					break;
				case 9:
					testMessages = new Message[3];
					testMessages[0].page_title = "This Cluster is Clean";
					testMessages[0].user = "CoolGuy93";
					testMessages[0].time = "4:13";
					testMessages[0].change_size = 100;
					testMessages[1].page_title = "This Cluster is Waste";
					testMessages[1].user = "BoringDude123";
					testMessages[1].time = "6:12";
					testMessages[1].change_size = -400;
					testMessages[2].page_title = "This Cluster is Infected";
					testMessages[2].user = "HooliganBro2";
					testMessages[2].time = "10:25";
					testMessages[2].change_size = 300;
					testMessages[2].summary = "VANDAL";
					for (int i = 0; i < testMessages.Length; ++i)
					{
						scrNodeMaster.Instance.ReceiveMessage(testMessages[i]);
					}
					break;
				case 12:
					StartCoroutine(scrNodeMaster.Instance.Purge());
					break;
				case 13:
					string[] usernames = new string[] {
						"RandomPerson1993",
						"IRegistered",
						"UserGuy",
						"ArticleEditor4",
						"Super_Cool_New_User_Who_Makes_Good_Edits",
						"JustRegistered612",
						"Abengoshis",
						"UtopianIndustries_Employee1025",
						"Cybersystems_Shmybersystems",
						"Secret_Bombs",
						"GreatScott413"};
					for (int i = 0; i < usernames.Length; ++i)
					{
						Message message = new Message();
						message.user = usernames[i];
						scrEnemyMaster.Instance.ReceiveMessage(message);
					}
					break;
				}
			}

			TutorialText.ChangeText(phrases[phrase]);
		}
	}
}
