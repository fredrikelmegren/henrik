using UnityEngine;
using System.Collections;

public class Use : MonoBehaviour {

	public Component[] onOff;	//List of components that should turn on and off

	public void Used () {

		if(onOff[0] == enabled ){
			foreach(Component script in onOff){
				Debug.Log("Click!" + script);
			}
		}
	
	}

}
