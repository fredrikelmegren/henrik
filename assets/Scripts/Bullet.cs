using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

	private float currentVelocity;
	private float previousVelocity;
	private float vel;

	// Use this for initialization
	void Start () {
		gameObject.rigidbody2D.AddForce(new Vector2(1000,0));
	}

	void Update () {
	
	}
}
