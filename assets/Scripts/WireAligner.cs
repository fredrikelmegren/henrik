using UnityEngine;
using System.Collections;

public class WireAligner : MonoBehaviour {

	// Update is called once per frame

	public GameObject wireVertical;
	public GameObject wireHorizontal;

	void Start () {
	

	}

	void Update () {
	
		//Do a ray check in all 4 directions
		RaycastHit2D[] right = Physics2D.RaycastAll (gameObject.transform.position, new Vector2(1,0), Mathf.Infinity , 13);
		RaycastHit2D[] left = Physics2D.RaycastAll (gameObject.transform.position, new Vector2(-1,0), Mathf.Infinity, 13);
		RaycastHit2D[] up = Physics2D.RaycastAll (gameObject.transform.position, new Vector2(0,1), Mathf.Infinity, 13);
		RaycastHit2D[] down = Physics2D.RaycastAll (gameObject.transform.position, new Vector2 (0,-1), Mathf.Infinity, 13);

		Debug.DrawRay(gameObject.transform.position, new Vector2(1,0), Color.blue);
		Debug.DrawRay(gameObject.transform.position, new Vector2(-1,0), Color.green);
		Debug.DrawRay(gameObject.transform.position, new Vector2(0,1), Color.red);
		Debug.DrawRay(gameObject.transform.position, new Vector2(0,-1), Color.magenta);

		//Check right raytrace to see if theres a wire there...
		if(right[1].collider.gameObject.GetComponent<WireAligner>() == true){
			Debug.Log("Found wire on the right, checking if it's the same");

			if(right[1].collider.gameObject.CompareTag("Wire Vertical")){
				Debug.Log("Not the same wire, deleting this object and replacing it with new...");
				Destroy(gameObject);
				Instantiate(wireVertical, transform.position, transform.rotation);
				}

			else {
				Debug.Log("Same wire!");
			}
		}

		//Check left raytrace to see if theres a wire there...
		if(left[1].collider.gameObject.GetComponent<WireAligner>() == true ){
			Debug.Log("Found wire on the right, checking if it's the same");
			
			if(left[1].collider.gameObject.CompareTag("Wire Vertical")){
				Debug.Log("Not the same wire, deleting this object and replacing it with new...");
				Destroy(gameObject);
				Instantiate(wireVertical, transform.position, transform.rotation);
			}
			else {
				Debug.Log("Same wire!");
			}
		}

		//Check up raytrace to see if theres a wire there...
		if(up[1].collider.gameObject.GetComponent<WireAligner>() == true){
			Debug.Log("Found wire on the right, checking if it's the same");
			
			if(up[1].collider.gameObject.CompareTag("Wire Horizontal")){
				Debug.Log("Not the same wire, deleting this object and replacing it with new...");
				Destroy(gameObject);
				Instantiate(wireVertical, transform.position, transform.rotation);
			}
			else {
				Debug.Log("Same wire!");
			}
		}

		//Check down raytrace to see if theres a wire there...
		if(down[1].collider.gameObject.GetComponent<WireAligner>() == true){
			Debug.Log("Found wire on the right, checking if it's the same");
			
			if(down[1].collider.gameObject.CompareTag("Wire Horizontal")){
				Debug.Log("Not the same wire, deleting this object and replacing it with new...");
				Destroy(gameObject);
				Instantiate(wireVertical, transform.position, transform.rotation);
			}
			else {
				Debug.Log("Same wire!");
			}
		}

	}
}
