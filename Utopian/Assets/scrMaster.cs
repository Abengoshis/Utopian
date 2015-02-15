using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrMaster : MonoBehaviour
{
	public static scrMaster Instance { get; private set; }
	public static bool Loading { get; private set; }

	private GameObject loading;
	private Slider loadingSlider;
	private float loadingAmount;


	public bool Transitioning { get; private set; }	// If between waves or not.
	private Text transitionText, transitionTimeText;
	private Slider waveSlider;
	private string[] transitionPhrases = new string[] { "PURGING BUFFERS", "RETARGETING AI", "PREPARING SCANNER", "STARTING CRAWL" };

	public float TransitionDuration { get; private set; }
	public float TransitionTimer { get; private set; }

	public float WaveDuration { get; private set; }
	public float WaveTimer { get; private set; }


	// Use this for initialization
	void Start ()
	{
		Instance = this;

		GameObject guiCanvas = GameObject.Find ("GUICanvas");

		loading = guiCanvas.transform.Find ("Loading").gameObject;
		loadingSlider = loading.transform.Find ("Slider").GetComponent<Slider>();
		loadingAmount = 0;

		transitionText = guiCanvas.transform.Find ("TransitionText").GetComponent<Text>();
		transitionTimeText = transitionText.transform.Find ("TransitionTime").GetComponent<Text>();
		waveSlider = guiCanvas.transform.Find ("WaveSlider").GetComponent<Slider>();


		Transitioning = true;
		TransitionDuration = 10;
		TransitionTimer = 0;
		WaveDuration = 120;
		WaveTimer = 0;

		StartCoroutine(LoadAll());
	}

	void Update()
	{
		if (Input.GetButton("Cancel"))
		{
			//scrWebSocketClient.Instance.Close();

			//Application.LoadLevel(0);
		}

		if (Loading)
		{
			loading.transform.Find ("Text").transform.eulerAngles = new Vector3(0, 0, Mathf.Sin (Time.time) * 5);
			loadingSlider.value = Mathf.Lerp (loadingSlider.value, loadingAmount, 0.5f);
		}
		else
		{
			if (Transitioning)
			{
				TransitionTimer += Time.deltaTime;
				if (TransitionTimer >= TransitionDuration)
				{
					TransitionTimer = 0;
					Transitioning = false;

					// Hide the transition text.
					transitionText.gameObject.SetActive(false);
				}
				else
				{
					transitionText.text = transitionPhrases[(int)(TransitionTimer / TransitionDuration * transitionPhrases.Length)]; 
				}

				transitionTimeText.text = "Prepare for next wave! <" + ((int)((TransitionDuration - TransitionTimer) * 10) * 0.1f).ToString() + " seconds>";
			}
			else
			{
				WaveTimer += Time.deltaTime;
				if (WaveTimer >= WaveDuration)
				{
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
		yield return StartCoroutine(scrNodeMaster.Instance.LoadNodePool());

		// text...
		loadingAmount += loadingIncrement;
		yield return new WaitForEndOfFrame();
		yield return StartCoroutine(scrNodeMaster.Instance.LoadCubePool());

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

		// Hide the loading screen.
		loading.SetActive(false);
		audio.Play();

		scrResults.Clear ();
		scrResults.TimeStart = Time.time;

		Loading = false;
	}
}
