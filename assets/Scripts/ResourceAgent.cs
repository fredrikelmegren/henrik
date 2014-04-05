using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceAgent : MonoBehaviour {

	public float resources;					// How much resources it's carrying
	public float teleportSpeed; 			// Time in seconds before teleporting to next tile
//	public Component moveAlongComponent;	// Which tile it will move along - Not used yet.
	public Vector3 currentTilePosition;
	private Vector3 previousTilePosition;
	private Vector3 moveToTile;
	private LayerMask mask = 1 << 16;
	private Vector3 heading;

	void Start () {

		moveToTile = transform.position;
		StartCoroutine(AgentMovement());
	
	}
	

	public IEnumerator AgentMovement () {

		RaycastHit2D[] rightRay = Physics2D.RaycastAll (transform.position, new Vector2(1,0) , 1 , mask);
		RaycastHit2D[] leftRay = Physics2D.RaycastAll (transform.position, new Vector2(-1,0), 1, mask);
		RaycastHit2D[] upRay = Physics2D.RaycastAll (transform.position, new Vector2(0,1), 1, mask);
		RaycastHit2D[] downRay = Physics2D.RaycastAll (transform.position, new Vector2(0,-1), 1, mask);
		Collider2D[] currentTile = Physics2D.OverlapPointAll (transform.position, mask);

		List<Vector2> allWires = new List<Vector2>(); 

		foreach(Collider2D hit in currentTile) {

			currentTilePosition = hit.transform.position;

		}

		foreach(RaycastHit2D hit in rightRay){

			if(hit.collider.gameObject.GetComponent<Wire>().sendRight && hit.collider.gameObject.transform.position != currentTilePosition
			   && hit.collider.gameObject.transform.position != previousTilePosition){

				allWires.Add (hit.collider.gameObject.transform.position);
			}
		}

		foreach(RaycastHit2D hit in leftRay){
			
			if(hit.collider.gameObject.GetComponent<Wire>().sendLeft && hit.collider.gameObject.transform.position != currentTilePosition
			   && hit.collider.gameObject.transform.position != previousTilePosition){
				allWires.Add (hit.collider.gameObject.transform.position);
			}
		}

		foreach(RaycastHit2D hit in upRay){
			
			if(hit.collider.gameObject.GetComponent<Wire>().sendUp && hit.collider.gameObject.transform.position != currentTilePosition
			   && hit.collider.gameObject.transform.position != previousTilePosition){
				allWires.Add (hit.collider.gameObject.transform.position);
			}
		}

		foreach(RaycastHit2D hit in downRay){
			
			if(hit.collider.gameObject.GetComponent<Wire>().sendDown && hit.collider.gameObject.transform.position != currentTilePosition
			   && hit.collider.gameObject.transform.position != previousTilePosition){
				allWires.Add (hit.collider.gameObject.transform.position);
			}
		}

		if (allWires.Count == 0) {

						moveToTile = previousTilePosition;

				} else {

						moveToTile = allWires [Random.Range (0, allWires.Count)];

				}


		previousTilePosition = currentTilePosition;
		// heading = moveToTile - previousTilePosition;

		// Debug.Log ("Heading: " + heading);

		yield return new WaitForSeconds(0.5f);
		StartCoroutine(AgentMovement());

	}

	void Update (){

		transform.position = Vector3.MoveTowards(transform.position, moveToTile, 2f * Time.deltaTime);

	}
	

}
