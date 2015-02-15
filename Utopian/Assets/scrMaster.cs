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


	// Use this for initialization
	void Start ()
	{
		Instance = this;

		loading = GameObject.Find ("GUICanvas").transform.Find ("Loading").gameObject;
		loadingSlider = loading.transform.Find ("Slider").GetComponent<Slider>();
		loadingAmount = 0;

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
