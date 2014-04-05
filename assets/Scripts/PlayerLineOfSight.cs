using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PlayerLineOfSight : MonoBehaviour {

    public List<GameObject> TestWalls;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

    void OnDrawGizmos()
    {
        foreach ( GameObject wall in TestWalls )
        {
            Gizmos.DrawLine( transform.position, wall.transform.position + new Vector3( 0.5f, 0.5f, 0 ) );
            Gizmos.DrawLine( transform.position, wall.transform.position - new Vector3( 0.5f, 0.5f, 0 ) );
            Gizmos.DrawLine( transform.position, wall.transform.position + new Vector3( 0.5f, -0.5f, 0 ) );
            Gizmos.DrawLine( transform.position, wall.transform.position + new Vector3( -0.5f, 0.5f, 0 ) );

            
            for ( int i = 0; i < 4; ++i )
            {


            }
        }
    }
}
