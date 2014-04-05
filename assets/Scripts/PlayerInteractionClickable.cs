using UnityEngine;
using System.Collections;

public class PlayerInteractionClickable : MonoBehaviour {

	public float radius = 10f;	//Default clickable radius around the object the script is attached to

	void Update() 
	{
			
		if (Input.GetMouseButtonDown(0)){
		Debug.Log("Left mouse button clicked");
		Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius, 9);
			int i = 0;
			while(i < hitColliders.Length){
				Debug.Log(i);
				i++;
			}
		}
	}

	void OnDrawGizmosSelected()
	{
	Gizmos.color = Color.cyan;
	Gizmos.DrawWireSphere(transform.position, radius);
	}
}
