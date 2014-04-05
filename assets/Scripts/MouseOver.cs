using UnityEngine;
using System.Collections;

public class MouseOver : MonoBehaviour {
	
	// Update is called once per frame
	void Update () {

		Vector3 mouse = Input.mousePosition;
		mouse.z = -7.5f;
		Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(mouse);

		RaycastHit2D hit = Physics2D.Raycast (mousePositionInWorld, Vector2.zero, 10);

		this.guiText.text = hit.collider.gameObject.name;

	}
}
