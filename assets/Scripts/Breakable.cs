using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Breakable : MonoBehaviour {

	private float force;
	public List <GameObject> debris;
	public float breakLimit;
	private Vector2 debrisForce;

	void Start(){


	}

	// Use this for initialization
	void OnCollisionEnter2D (Collision2D coll) {

		force = coll.rigidbody.mass * coll.relativeVelocity.magnitude;

		foreach (GameObject debrisobject in debris) {

			if (force >= breakLimit ) {

				GameObject debrisInstantiate = (GameObject) Instantiate (debrisobject, gameObject.transform.position, Random.rotation);
				debrisInstantiate.transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
				debrisForce = new Vector2 ((Random.Range (100, -10)), (Random.Range (100, -100)));
				debrisInstantiate.rigidbody2D.AddForce(debrisForce);
				Destroy (gameObject);

			}
		}
	
	}
	
}