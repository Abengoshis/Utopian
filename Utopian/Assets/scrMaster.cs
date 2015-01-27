using UnityEngine;
using System.Collections;

public class scrMaster : MonoBehaviour
{
	public static scrMaster Instance { get; private set; }
	public static bool Loading { get; private set; }

	// Use this for initialization
	void Start ()
	{
		Instance = this;

		StartCoroutine(LoadAll());
	}
	
	IEnumerator LoadAll()
	{
		Loading = true;

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
		yield return new WaitForEndOfFrame();
		yield return StartCoroutine(scrNodeMaster.Instance.LoadNodePool());

		// text...
		yield return new WaitForEndOfFrame();
		yield return StartCoroutine(scrNodeMaster.Instance.LoadCubePool());

		// text...
		yield return new WaitForEndOfFrame();
		scrNodeMaster.PrecomputeNodePositions();

		// text...
		yield return new WaitForEndOfFrame();
		scrNode.PrecomputeCubePositions();

		// text...
		yield return new WaitForEndOfFrame();
		// Load player and enemy projectiles.

		// Reenable game scripts.
		scrPlayer.Instance.enabled = true;
		scrNodeMaster.Instance.enabled = true;

		Loading = false;
	}
}
