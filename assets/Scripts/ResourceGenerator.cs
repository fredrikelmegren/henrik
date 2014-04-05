using UnityEngine;
using System.Collections;

public class ResourceGenerator : MonoBehaviour {

	public float outputRate;				// How often a resource agent will be sent, per second
	public float outputValue;				// How much resource the agent will carry
	public float firstOutputDelay;			// How long time in secs will it take until first agent is spawned
	public float finiteResource;			// How much resource this source can give out by its own
	public float maxResources;				// The max amount of resources this gameObject can hold
	public GameObject resource;				// What kind of gameObject should be generated as a gameObject

    static Transform resourceContainer;

	void Start(){

		StartCoroutine(GenerateResource());
        if ( !resourceContainer )
        {
            resourceContainer = new GameObject( "Energy" ).transform;
        }
	}

	public IEnumerator GenerateResource (){

		if (finiteResource > 0){
			InvokeRepeating("InstanceResource", 0.0001f, 0);
			yield return new WaitForSeconds(outputRate);
			StartCoroutine(GenerateResource());
		}
		else
		{
			outputRate = 0;
		}

	}

	void InstanceResource () {

        GameObject spawnedResource = (GameObject)Instantiate( resource, gameObject.transform.position, Quaternion.identity );
        spawnedResource.transform.parent = resourceContainer;
		spawnedResource.GetComponent<ResourceAgent>().resources = outputValue;
		finiteResource = finiteResource - outputValue;
		finiteResource = Mathf.Clamp (finiteResource, 0f, maxResources);
	
	}
	
}
	