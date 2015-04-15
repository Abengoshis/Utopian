using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrMaster : MonoBehaviour
{
	public bool Tutorial;

	public static scrMaster Instance { get; private set; }
	public static bool Loading { get; private set; }
	public static bool GameOver { get; private set; }

	private GameObject loading;
	private Slider loadingSlider;
	private float loadingAmount;


	public bool Transitioning { get; private set; }	// If between waves or not.
	private Text transitionText, transitionTimeText;
	private Slider waveSlider;
	private string[] transitionPhrases = new string[] { "PURGING BUFFERS", "RETARGETING AI", "STARTING CRAWL" };

	public float TransitionDuration { get; private set; }
	public float TransitionTimer { get; private set; }

	public float WaveDuration { get; private set; }
	public float WaveTimer { get; private set; }
	public int Wave { get; private set; }


	//public AudioClip TextSound;
	public AudioClip TextBlipSound;


	// Use this for initialization
	void Start ()
	{
		Instance = this;

		GameObject guiCanvas = GameObject.Find ("GUICanvas");

		loading = guiCanvas.transform.Find ("Loading").gameObject;
		loading.gameObject.SetActive(true);
		loadingSlider = loading.transform.Find ("Slider").GetComponent<Slider>();
		loadingAmount = 0;

		transitionText = guiCanvas.transform.Find ("TransitionText").GetComponent<Text>();
		transitionTimeText = transitionText.transform.Find ("TransitionTime").GetComponent<Text>();
		waveSlider = guiCanvas.transform.Find ("WaveSlider").GetComponent<Slider>();

		if (!Tutorial)
		{
			Transitioning = true;
			TransitionDuration = 10;
			TransitionTimer = 0;
			WaveDuration = 180;
			WaveTimer = 0;
			Wave = 0;
		}
		else
			Transitioning = false;

		StartCoroutine(LoadAll());
	}

	void Update()
	{
		if (Input.GetButton("Cancel"))
		{
			if (scrWebSocketClient.Instance != null)
				scrWebSocketClient.Instance.Close();
				
			ItsGameOverMan();
		}

		if (GameOver)
			return;

		if (Loading)
		{
			if (!loading.GetComponentInChildren<scrScrollText>().Running)
			{
				loading.GetComponentInChildren<scrScrollText>().Restart();
			}

			loadingSlider.value = Mathf.Lerp (loadingSlider.value, loadingAmount, 0.5f);
		}
		else
		{
			scrResults.TimeElapsed += Time.deltaTime;

			if (Transitioning)
			{
				TransitionTimer += Time.deltaTime;
				if (TransitionTimer >= TransitionDuration)
				{
					TransitionTimer = 0;
					Transitioning = false;

					// Hide the transition text.
					transitionText.gameObject.SetActive(false);

					// Inject infected nodes based on the wave number.
					scrNodeMaster.Instance.InjectInfectedMessages(100);
				}
				else
				{
					int whole = (int)(TransitionTimer / TransitionDuration * transitionPhrases.Length);
					float part = (TransitionTimer / TransitionDuration * transitionPhrases.Length) - whole;

					int substringLength = (int)(part * 55);// Where the magic number = number of characters to fill within the time. The text speed essentially.

					string phrase = transitionPhrases[whole];
					transitionText.text = phrase.Substring(0, (int)Mathf.Min (substringLength, phrase.Length));

					if (substringLength > phrase.Length)
					{
						audio.Stop ();
					}
					else
					{
						if (!audio.isPlaying)
							audio.Play ();
					}
				}

				transitionTimeText.text = "Prepare for next wave! <" + (TransitionDuration - TransitionTimer).ToString("F1") + " seconds>";
				if (TransitionTimer % 0.4f < 0.2f)
					transitionTimeText.color = Color.white;
				else
					transitionTimeText.color = Color.grey;
			}
			else if (!Tutorial)
			{
				WaveTimer += Time.deltaTime;
				if (WaveTimer >= WaveDuration)
				{
					++Wave;
					WaveTimer = 0;
					Transitioning = true;

					// Show the transition text.
					transitionText.gameObject.SetActive(true);

					// Reset enemies, nodes and ai core.
					StartCoroutine(scrNodeMaster.Instance.Purge());

				}
				else
				{

				}

				waveSlider.value = WaveTimer / WaveDuration;
			}
		}
	}

	IEnumerator LoadAll()
	{
		Loading = true;
		float loadingIncrement = 1.0f / 5.0f;

		// Connecting to wikimon websocket.
		// text...
		yield return new WaitForEndOfFrame();
		while (scrWebSocketClient.Instance != null && !scrWebSocketClient.Instance.Connected)
		{
			if (scrWebSocketClient.Instance.Failed)
			{
				// text...
				yield break;
			}
			yield return new WaitForEndOfFrame();
		}

		// Pools.
		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		yield return StartCoroutine(scrNodeMaster.Instance.LoadNodePool(Tutorial));

		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		yield return StartCoroutine(scrNodeMaster.Instance.LoadCubePool(Tutorial));

		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		scrNodeMaster.PrecomputeNodePositions();

		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		scrNode.PrecomputeCubePositions();

		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		// Load player and enemy projectiles.

		// Reenable game scripts.
		scrPlayer.Instance.enabled = true;
		scrNodeMaster.Instance.enabled = true;
		scrEnemyMaster.Instance.enabled = true;
		scrMusicMaster.Instance.enabled = true;

		scrFadeTransition.Instance.FadeIn();

		// Hide the loading screen.
		loading.SetActive(false);

		scrResults.Clear ();
		scrResults.TimeElapsed = 0.0f;

		Loading = false;
	}

	public void ItsGameOverMan()
	{
		scrNodeMaster.Instance.Purge();

		// Disable system scripts.
		scrPlayer.Instance.enabled = false;
		scrNodeMaster.Instance.enabled = false;
		scrEnemyMaster.Instance.enabled = false;
		scrMusicMaster.Instance.enabled = false;

		scrFadeTransition.Instance.FadeOut(5.0f);

		if (Tutorial)
			Invoke("GoToMenu", 5.0f);
		else
			Invoke("GoToResults", 5.0f);
	}

	void GoToMenu()
	{
		Application.LoadLevel("Menu");
	}

	void GoToResults()
	{
		Application.LoadLevel("Results");
	}
}