using UnityEngine;
using System.Collections;

//Author: Brian Herman
//Last Updated: 5/13/2014
//Summary: Rope swinging script for OcculusVR Unity-based game. Had 1 week to get the game done.
//Inspiration taken from Spiderman64

public class TetherScript : MonoBehaviour {

	public int numSegments = 10;
	public float maxRopeDistance = 2f;
	public float maxThrowDistance = 80f;
	public bool isTethered;
	public Material[] m;
	public Vector3 p;
	public GameObject pointerObj;

	public float tetherLocationX = 550f;
	public float tetherLocationY = 600f;

	//Vector3 p;
	private LineRenderer rope;
	private Ray cameraDir;
	private RaycastHit hit;
	private int layermask;
	// Use this for initialization
	void Start () {
		rope = GetComponent<LineRenderer>();
		rope.enabled = true;
		rope.SetVertexCount(numSegments + 1);
		//p = new Vector3(0,10,0);
		rope.SetWidth (.5f, .5f);

		layermask = 1<<4; //Create the Layermask for the water.
		layermask = ~layermask;
		//maxRopeDistance = Vector3.Distance(transform.position, p);
	}
	
	// Update is called once per frame
	void Update () {
		pointerObj.transform.position = transform.position;
		cameraDir = transform.FindChild("OVRCameraController").FindChild("CameraLeft").camera.ScreenPointToRay(new Vector3(tetherLocationX,tetherLocationY,0));

		//Debug.DrawRay(cameraDir.origin,cameraDir.direction,Color.green);

		if(Physics.Raycast(cameraDir,out hit,camera.farClipPlane,layermask)){
			if(!isTethered){
				pointerObj.transform.position = hit.point;
				pointerObj.renderer.material = m[0];
				float distance = Vector3.Distance(pointerObj.transform.position,transform.position);
				if(distance < 8){
					pointerObj.renderer.material.color = new Color(pointerObj.renderer.material.color.r, pointerObj.renderer.material.color.g,1f/(8.1f - distance));
				}
					else
					pointerObj.renderer.material.color = new Color(pointerObj.renderer.material.color.r, pointerObj.renderer.material.color.g,pointerObj.renderer.material.color.b,1f);

			}
			else{ 
				pointerObj.transform.position = p;
				pointerObj.renderer.material = m[1];
			}
			if(Vector3.Distance(transform.position,hit.point) < maxThrowDistance){
				pointerObj.renderer.material.color = Color.green;
				pointerObj.light.color = Color.green;
				if(Input.GetAxis("RightTrigger")>0){
					if(!isTethered){
						isTethered = true;
						p = hit.point;
					}
				}
			
			}
			else {
				pointerObj.renderer.material.color = Color.red;
				pointerObj.light.color = Color.red;

			}
		}

		if(Input.GetAxis("RightTrigger") == 0){
			isTethered = false;
		}

		Vector3 previousPosition = transform.position;
		float orbitSpeed;
		if(isTethered){
			pointerObj.renderer.material.color = Color.white;
			pointerObj.light.color = Color.black;

			RenderGraphics(p);

			float totalDist = Vector3.Distance(transform.position,p);
			Vector3 updir = p - transform.position;
			if( totalDist > maxRopeDistance)
			{
				updir.Normalize();
				updir *= .5f;
				rigidbody.velocity += updir;
				//Debug.DrawRay(transform.position,updir, Color.green);
				orbitSpeed = 3f;
			}
			else
			{
				updir.Normalize();
				updir *= .1f;
				rigidbody.velocity += updir;
				orbitSpeed = 15f;
			}

			orbitSpeed *= updir.magnitude;

			transform.RotateAround (p, p - transform.position, orbitSpeed * Time.deltaTime);
			Vector3 orbitDesiredPosition = (transform.position - p).normalized * maxRopeDistance + p;
			rigidbody.velocity = Vector3.Slerp(rigidbody.velocity, orbitDesiredPosition-transform.position, Time.deltaTime * orbitSpeed);
			
			//Rotation
			Vector3 relativePos = transform.position - previousPosition;
			//Quaternion rotation = Quaternion.LookRotation(relativePos);
			//transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 10f * Time.deltaTime);
			previousPosition = transform.position;

			//transform.LookAt(p);
		}
		else
		{
			rope.enabled = false;
		}
	}

	void RenderGraphics(Vector3 t){
		if(!rope.enabled) rope.enabled = true;
			rope.material = m[1];
			Vector3 Direction = transform.position - t;
			//Direction.Normalize();
			Vector3 prevPos = transform.position;
			rope.SetPosition(0,prevPos);
			for(int i = 1; i < numSegments; i++)
			{
				Vector3 temp = prevPos - Direction;
				rope.SetPosition(i,temp);
				prevPos = temp;
			}
			rope.SetPosition(numSegments, t);
	}
}
