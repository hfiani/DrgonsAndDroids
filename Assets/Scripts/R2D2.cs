using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class R2D2 : MonoBehaviour
{
	[SerializeField] Transform Door;
	[SerializeField] bool MovementEnabled = true;

	Animator anim;
	NavMeshAgent navmesh;
	Transform Contoller;

	int destination_index = -1;
	int destination_number = 0;

	float t;
	int rand;

	// Use this for initialization
	void Start ()
	{
		if (MovementEnabled)
		{
			anim = transform.GetChild (0).GetComponent<Animator> ();
			navmesh = transform.GetComponent<NavMeshAgent> ();
			Contoller = GameObject.Find (name + "_Controller").transform;
			destination_number = Contoller.childCount;
			t = Time.time;
			rand = Random.Range (0, 5);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (MovementEnabled && Vector3.Distance (navmesh.destination, transform.position) < 5)
		{
			if (t == 0)
			{
				t = Time.time;
				rand = Random.Range (0, 5);
			}
			if (t + rand - Time.time < 0)
			{
				t = 0;
				destination_index++;
				if (destination_index >= destination_number)
				{
					destination_index = 0;
				}
				navmesh.SetDestination (Contoller.GetChild (destination_index).position);
				anim.SetBool ("Walk", true);
			}
			else
			{
				anim.SetBool ("Walk", false);
			}
		}
	}

	void OnDestroy()
	{
		if (Door != null)
		{
			Door.GetChild (0).localRotation = Quaternion.Euler (0, 90, 0);
			Door.GetChild (1).localRotation = Quaternion.Euler (0, -90, 0);
		}
	}
}
