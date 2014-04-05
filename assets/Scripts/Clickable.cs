using UnityEngine;
using System.Collections;

public class Clickable : MonoBehaviour {

	public float radius = 1f;		// Default clickable radius around the object the script is attached to
	private GameObject player;
	private float range;

	void Update()
	{

		if (Input.GetMouseButtonDown(0)){
			OnClick();
		}
	}
	
	void OnClick()
	{






/*		player = GameObject.FindWithTag("Player");
		range = (Vector2.Distance(player.transform.position, gameObject.transform.position));

		Vector3 mouse = Input.mousePosition;
		mouse.z = -7.5f;
		Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(mouse);

		if(range < 1 ){
			Debug.Log("In range!");
		}

		else if(range > 1 ){
			Debug.Log("Out of range!");
		}*/
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, radius);
	}

}
