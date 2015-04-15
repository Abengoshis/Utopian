using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrAICore : MonoBehaviour
{
	public static scrAICore Instance { get; private set; }

	public float Intelligence { get; private set; }
	public float Corruption { get; private set; }
	const int CORRUPTION_MAX = 100;
	const int INTELLIGENCE_MAX = 500;

	Slider corruptionSlider;
	Slider intelligenceSlider;


	public GameObject ChildArm { get; private set; }
	public GameObject ChildFocus { get; private set; }
	public bool ArmLocked { get; private set; }

	void Start ()
	{
		Instance = this;

		corruptionSlider = GameObject.Find ("GUICanvas").transform.Find ("CorruptionSlider").GetComponent<Slider>();
		intelligenceSlider = GameObject.Find ("GUICanvas").transform.Find ("IntelligenceSlider").GetComponent<Slider>();
	
		Corruption = 0;
		Intelligence = 0;

		ChildArm = transform.Find ("Arm").gameObject;
		ChildFocus = transform.Find ("Focus").gameObject;
		ArmLocked = true;
	}

	void Update ()
	{
		if (scrNodeMaster.Instance.NodeBeingUploaded != null)
		{
			Vector3 direction = scrNodeMaster.Instance.NodeBeingUploaded.transform.position - transform.position;
			float rotation = Mathf.Rad2Deg * Mathf.Atan2 (direction.y, direction.x);
			if (rotation < 0)
				rotation += 360;

			// Has rotated to the right direction?
			if (Mathf.Abs (Mathf.DeltaAngle(transform.eulerAngles.z, rotation)) < 2)
			{
				transform.eulerAngles = new Vector3(0, 0, rotation);

				if (Vector3.Distance(ChildFocus.transform.position, scrNodeMaster.Instance.NodeBeingUploaded.transform.position) < 0.1f) 
				{

					ChildFocus.transform.position = scrNodeMaster.Instance.NodeBeingUploaded.transform.position;
					if (!ArmLocked)
					{

						ArmLocked = true;
						ChildFocus.audio.pitch = 1;

						if (scrNodeMaster.Instance.NodeBeingUploaded.gameObject.activeSelf && !scrNodeMaster.Instance.NodeBeingUploaded.Uploading)
							StartCoroutine(scrNodeMaster.Instance.NodeBeingUploaded.Upload());
					}
				}
				else
				{
					ArmLocked = false;

					Vector3 prevPos = ChildFocus.transform.position;

					// Move focus up arm until it reaches the node to upload.
					ChildFocus.transform.position = Vector3.MoveTowards(ChildFocus.transform.position, scrNodeMaster.Instance.NodeBeingUploaded.transform.position, 80 * Time.deltaTime);

					// Check if moving away or towards the core.
					if (ChildFocus.transform.position.magnitude > prevPos.magnitude)
						ChildFocus.audio.pitch = 1.2f;
					else
						ChildFocus.audio.pitch = 0.8f;
				}
			}
			else
			{
				ArmLocked = false;

				// Rotate so that the arms align with the node.
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0.0f, 0.0f, rotation), 80 * Time.deltaTime);
			}
		}
	}

	public void Learn(bool infected, float amount = 1.0f)
	{
		Intelligence += amount;
		intelligenceSlider.value = (float)Intelligence / INTELLIGENCE_MAX;
		if (infected)
		{
			Corruption += amount;
			corruptionSlider.value = (float)Corruption / CORRUPTION_MAX;
		}

		// Placeholder ending for research.
		if (Intelligence == INTELLIGENCE_MAX || Corruption == CORRUPTION_MAX)
		{
			Application.LoadLevel("Results");
		}
	}

}
