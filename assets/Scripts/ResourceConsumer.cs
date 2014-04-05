using UnityEngine;
using System.Collections;

public class ResourceConsumer : MonoBehaviour {

	public ResourceAgent resourceAgent;
	public float storedResources;
	public float bucketSize;
	public float checkLayer;


	void Update () {

		Collider2D[] currentTile = Physics2D.OverlapPointAll (gameObject.transform.position);

		foreach(Collider2D c in currentTile){

			if(c.gameObject.layer == checkLayer && storedResources < bucketSize){

				storedResources = storedResources + c.collider2D.gameObject.GetComponent<ResourceAgent>().resources;
				Debug.Log("Recived: " + c.gameObject.name);
				Destroy(c.gameObject);
			}

			else if (storedResources >= bucketSize)
			{
				storedResources = Mathf.Clamp(storedResources, 0, bucketSize);
			}
		}
			
	}
	
}
