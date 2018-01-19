using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Script of the dragon. no idea why i named this Finish
public class Finish : MonoBehaviour
{
	Vector3 initPos;
	Quaternion initRot;

	// Use this for initialization
	void Start ()
	{
		initPos = transform.position;
		initRot = transform.rotation;
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void OnTriggerEnter(Collider other)
	{
		// if the big trigger box of the baby dragon is within our range, we won!
		if (other.name.Equals ("Dragon_baby"))
		{
			Time.timeScale = 0;
			GameObject.Find ("Logo").GetComponent<Image> ().enabled = true;
		}
		// if R2D2 hits us, go back to the initial position
		else if (other.name.Contains ("R2D2") || other.name.Contains ("Mazinger"))
		{
			transform.position = initPos;
			transform.rotation = initRot;
		}
	}
}
