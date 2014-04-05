using UnityEngine;
using System.Collections;

public class ActionInformer : MonoBehaviour {

	private GameObject player;
	private float range;
	
	void Update () {

		Vector3 mouse = Input.mousePosition;
		mouse.z = -7.5f;
		Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(mouse);
		
		RaycastHit2D hit = Physics2D.Raycast (mousePositionInWorld, Vector2.zero);
		
		player = GameObject.FindWithTag("Player");
		range = (Vector2.Distance(player.transform.position, hit.collider.gameObject.transform.position));
		
		if (Input.GetMouseButtonDown(0)){
			
			if(hit.collider != null && range < 1 )
			{
				this.guiText.text = hit.collider.gameObject.name;
			}
			
			else if (hit.collider) {
				this.guiText.text = ("Nothing to interact with!");
			}
			
			else if (range > 1){
				this.guiText.text = ("Out of range!");
			}
			
		}
	
	}
}
