using UnityEngine;
using System.Collections;

public class DoorTrigger : MonoBehaviour {

	public Door connectedDoor;
	

	void OnTriggerStay2D (Collider2D coll)
	{
		if(coll.tag == "Player"){
			connectedDoor.DoorOpen();
		}
	
	}

	void OnTriggerExit2D (Collider2D coll)
	{
		if(coll.tag == "Player"){
			connectedDoor.DoorClose();
		}
		
	}
	
}
