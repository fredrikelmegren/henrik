using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour
{

	public float walkForce = 365f;			// Amount of force added to move the player left and right.
	public float sprintForce = 365f;		// Force to be used when sprinting.
	public float walkSpeed = 5f;			// Standard moving speed of a character
	public float sprintSpeed = 8f;			// Fastest player can run while holding sprint button.
	public float drag = 1f;					// Makes the character slow down when not receiving input.
	public float reachRadius = 1f;			// Distance a player can click on stuff
	public GameObject ball;
	public Transform fireFrom;
	
	private Animator anim;					// Reference to the player's animator component.

	private GameObject player;
	private float range;

	public GameObject wire;

    static Transform ballContainer;

	void Awake()
	{
		// Setting up references.
		anim = GetComponent<Animator>();
        ballContainer = new GameObject( "Balls" ).transform;
	}


	void Update()
	{
		// Mouse Click code

		Vector3 mouse = Input.mousePosition;
		mouse.z = -7.5f;
		Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(mouse);

		// Code for tile placement with mouse
		Vector3 mousePositionInWorldRounded = mousePositionInWorld;
		mousePositionInWorldRounded.x = Mathf.RoundToInt(mousePositionInWorldRounded.x);
		mousePositionInWorldRounded.y = Mathf.RoundToInt(mousePositionInWorldRounded.y);
		mousePositionInWorldRounded.z = 0;
		

		RaycastHit2D hit = Physics2D.Raycast (mousePositionInWorld, Vector2.zero);

        if ( hit )
        {
            player = GameObject.FindWithTag( "Player" );
            range = (Vector2.Distance( player.transform.position, hit.collider.gameObject.transform.position ));
        }
		
		if (Input.GetMouseButtonDown(0))
        {
            ((GameObject)Instantiate( ball, fireFrom.transform.position, Quaternion.identity )).transform.parent = ballContainer;
		}


			
//			if(hit.collider != null && range < 1 )
//			{
//				Debug.Log ("Target Position: " + hit.collider.gameObject.name);
//				// hit.collider.gameObject.GetComponent<Use>() .Used ();
//			}
//
//			else if (hit.collider == null ) {
//				Debug.Log("Nothing to interact with!");
//			}
//
//			else if (range > 1){
//				Debug.Log ("Out of range!");
//			}
//			
//		}

		if (Input.GetMouseButtonDown(2)){
			if(hit.collider.gameObject.GetComponent<Wire>() == false )
			Instantiate(wire, mousePositionInWorldRounded, Quaternion.identity);

		else if (hit.collider.gameObject.GetComponent<Wire>() == true) {

				Destroy(hit.collider.gameObject);
		}

		}

	}

	void FixedUpdate ()
	{
		// Cache the horizontal/vertical input.
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");

		rigidbody2D.drag = drag;

		float maxSpeed = walkSpeed;
		float moveForce = walkForce;


		if(Input.GetKey(KeyCode.LeftShift)) {
			maxSpeed = sprintSpeed;
			moveForce = sprintForce;
		}

		// The Speed animator parameter is set to the absolute value of the horizontal input.

		// If the player is changing direction (h has a different sign to velocity.x) or hasn't reached maxSpeed yet...
		if(h * rigidbody2D.velocity.x < maxSpeed)
			// ... add a force to the player.
			rigidbody2D.AddForce(Vector2.right * h * moveForce);

		if(v * rigidbody2D.velocity.y < maxSpeed)
			// ... add a force to the player.
			rigidbody2D.AddForce(Vector2.up * v * moveForce);

		// Movement code

		if (h > 0)
		{
			anim.SetInteger("direction", 3);	// Right
		}
		else if (h < 0)
		{
			anim.SetInteger("direction", 1);	// Left
		}
		else if (v > 0)
		{
			anim.SetInteger("direction", 2);	// Up
		}
		else if (v < 0)
		{
			anim.SetInteger("direction", 0);	// Down
		}

	}
	
	
}
