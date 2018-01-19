using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour
{
	[SerializeField] private GameObject explosion;

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void OnParticleCollision(GameObject other)
	{
		Debug.Log ("Collision! " + other.name);
		if (other.name.Contains ("R2D2"))
		{
			GameObject exp = Instantiate (explosion, other.transform.position, Quaternion.identity);
			Destroy (other);
			Destroy (exp, 2);
		}
		if (other.name.Contains ("Mazinger"))
		{
			GameObject exp = Instantiate (explosion, other.transform.position, Quaternion.identity);
			exp.transform.localScale *= 2;
			Destroy (other);
			Destroy (exp, 2);
		}
	}
}
