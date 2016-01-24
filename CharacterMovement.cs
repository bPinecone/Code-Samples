using UnityEngine;
using System.Collections;

//Author: Brian Herman
//Last Updated: 5/13/2014
//Summary: Movement script for OcculusVR Unity-based game. Had 1 week to get the game done.

public class CharacterMovement : MonoBehaviour {

	enum RunState : byte {Walking,Sliding};	

	public float moveSpeed = 10f;
	private float secMoveSpeed, defMoveSpeed;
	public float rotateSpeed = 40f;
	//public GameObject walls;
	public PhysicMaterial walking,sliding;

	private float offsetSpeed = 1f;
	private RunState runState = RunState.Walking;
	private bool grounded = false;
	private float vertical,horizontal,rVertical, rHorizontal;
	private Transform OVRCameraTransform;




	void Start () {
		OVRCameraTransform = transform.FindChild ("OVRCameraController").camera.transform;
		defMoveSpeed = moveSpeed;
		secMoveSpeed = moveSpeed*2f;
	}

	void InputManagement(){
		if (Input.GetKeyDown(KeyCode.Space)) 
		{
			Debug.Log("Space Pressed");
			//if(rigidbody.useGravity == false){
			//	rigidbody.useGravity = true;
			
			//}
			GetComponent<TetherScript>().isTethered = !GetComponent<TetherScript>().isTethered;
		}
		vertical = Input.GetAxis("Vertical");
		horizontal = Input.GetAxis("Horizontal");
		//rVertical = Input.GetAxis("RVertical");
		rHorizontal = Input.GetAxis("RHorizontal");

		if (Input.GetAxis("LeftTrigger")>0){
			runState = RunState.Sliding;
		}
		else {
			runState = RunState.Walking;
		}
	}

	// Update is called once per frame
	void Update () {

		InputManagement();

		//RunState Management
		if(runState == RunState.Walking)
		{
			moveSpeed = secMoveSpeed;
			collider.material = walking;
		}
		else
		{ 
			moveSpeed = defMoveSpeed;
			collider.material = sliding;
		}


		if(vertical != 0)
			rigidbody.velocity += OVRCameraTransform.forward * vertical * Time.deltaTime * moveSpeed * 1.5f;

		if(horizontal != 0)
			rigidbody.velocity += OVRCameraTransform.right * rHorizontal * Time.deltaTime * moveSpeed * 1.5f;

		//Debug.Log(Input.GetAxis("RHorizontal"));
		if (rHorizontal != 0) {
			transform.RotateAround (transform.position, transform.up, rHorizontal * (2*Mathf.PI)* rotateSpeed * Time.deltaTime * offsetSpeed);
			offsetSpeed += .01f;
			//transform.FindChild("OVRCameraController").transform.RotateAround(transform.position, transform.up, Input.GetAxis ("RHorizontal") * rotateSpeed * Time.deltaTime);
		}
		else{
			offsetSpeed = 1f;
		}

		if(grounded){
			rigidbody.velocity = new Vector3(Mathf.Clamp(rigidbody.velocity.x,-60,60),rigidbody.velocity.y,Mathf.Clamp(rigidbody.velocity.z,-60,60));
		}
		else
		{
			rigidbody.velocity += -(Vector3.up*.5f);
		}

		if(runState == RunState.Walking){
			if(grounded){
				rigidbody.velocity = new Vector3(rigidbody.velocity.x * .97f, rigidbody.velocity.y, rigidbody.velocity.z * .97f);
			}
		}
		//rigidbody.velocity = new Vector3 (rigidbody.velocity.x, rigidbody.velocity.y-rigidbody.velocity.y*.6, rigidbody.velocity.z)//rigidbody.velocity.x * .99f, rigidbody.velocity.y*.99f, rigidbody.velocity.z * .99f);

		//Debug.Log (rigidbody.velocity);
		
	}

	void OnCollisionEnter(Collision c){
		if (c.transform.tag == "Walls"){
			grounded = true;
			Debug.Log("Touching Ground!");
		}
	}
	void OnCollisionExit(Collision c){
		if (c.transform.tag == "Walls"){
			grounded = false;
			Debug.Log("Not Touching Ground!");
		}
	}
}
