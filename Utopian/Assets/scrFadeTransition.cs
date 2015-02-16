using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class scrFadeTransition : MonoBehaviour
{
	public static scrFadeTransition Instance { get; private set; }

	public Image FadeImage;
	public bool Fading { get; private set; }

	void Start ()
	{
		Instance = this;
	}

	public void FadeIn(float duration = 5.0f)
	{
		StartCoroutine(Fade (Color.black, Color.clear, duration));
	}

	public void FadeOut(float duration = 5.0f)
	{
		StartCoroutine(Fade (Color.clear, Color.black, duration));
	}

	IEnumerator Fade(Color a, Color b, float duration)
	{
		// Wait until not fading.
		while (Fading == true)
			yield return new WaitForEndOfFrame();

		Fading = true;
		for (float t = 0; t < duration; t += Time.deltaTime)
		{
			FadeImage.color = Color.Lerp (a, b, t / duration);
			yield return new WaitForEndOfFrame();
		}
		Fading = false;
	}
}