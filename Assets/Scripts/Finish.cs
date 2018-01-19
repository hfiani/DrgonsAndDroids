using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
		if (other.name.Equals ("Dragon_baby"))
		{
			Time.timeScale = 0;
			GameObject.Find ("Logo").GetComponent<Image> ().enabled = true;
		}
		else if (other.name.Contains ("R2D2") || other.name.Contains ("Mazinger"))
		{
			transform.position = initPos;
			transform.rotation = initRot;
		}
	}
}
