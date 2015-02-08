using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class scrAICore : MonoBehaviour
{
	public static scrAICore Instance { get; private set; }

	public int Intelligence { get; private set; }
	public int Corruption { get; private set; }
	const int CORRUPTION_MAX = 1000;
	const int INTELLIGENCE_MAX = 1000;

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
		ArmLocked = false;
	}

	void Update ()
	{
		if (scrNodeMaster.Instance.NodeBeingUploaded != null)
		{
			Vector3 direction = scrNodeMaster.Instance.NodeBeingUploaded.transform.position - transform.position;
			float rotation = Mathf.Rad2Deg * Mathf.Atan2 (direction.y, direction.x);
			if (rotation < 0)
				rotation += 360;

			// Has rotated to the right direction? (Do this with smoothstep and work out timer in future). In fact do all of this with a timer you dumpass!
			if ((int)(ChildArm.transform.eulerAngles.z * 10) / 10.0f == (int)(rotation * 10) / 10.0f)
			{
				ChildArm.transform.eulerAngles = new Vector3(0, 0, rotation);

				if (Vector3.Distance(ChildFocus.transform.position, scrNodeMaster.Instance.NodeBeingUploaded.transform.position) < 0.1f) 
				{
					ChildFocus.transform.position = scrNodeMaster.Instance.NodeBeingUploaded.transform.position;

					if (!ArmLocked)
					{
						ArmLocked = true;

						if (!scrNodeMaster.Instance.NodeBeingUploaded.Uploading)
							StartCoroutine(scrNodeMaster.Instance.NodeBeingUploaded.Upload());
					}
				}
				else
				{
					ArmLocked = false;

					// Move focus up arm until it reaches the node to upload.
					ChildFocus.transform.position = Vector3.MoveTowards(ChildFocus.transform.position, scrNodeMaster.Instance.NodeBeingUploaded.transform.position, 20 * Time.deltaTime);
				}
			}
			else
			{
				ArmLocked = false;

				// Rotate so that the arms align with the node.
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, rotation), 10 * Time.deltaTime);
			}
		}
	}

	public void Learn(bool infected)
	{
		++Intelligence;
		intelligenceSlider.value = (float)Intelligence / INTELLIGENCE_MAX;
		if (infected)
		{
			++Corruption;
			corruptionSlider.value = (float)Corruption / CORRUPTION_MAX;
		}
	}

}
