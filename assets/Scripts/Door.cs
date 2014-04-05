using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour {

	private Animator anim;		// Reference to animation component
	private bool closed = true;	// Is the door closed by default?
	private int intBool = 0;

	void Start ()
	{
		if(closed == true)
		{
			intBool = 1;
		}
		anim = GetComponent<Animator>();
		gameObject.collider2D.enabled = closed;
		anim.SetInteger("Door", intBool);
	}

	public void DoorOpen(){

		anim.SetInteger("Door", 2);
		
	}

	public void DoorClose(){
		
		gameObject.collider2D.enabled = closed;
		anim.SetInteger("Door", 3);
		
	}

	public void DoorStayOpen(){

		gameObject.collider2D.enabled = !closed;
		
	}

}