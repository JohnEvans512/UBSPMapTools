/*
Copyright (c) 2021 John Evans

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#define RB_INTERACTION // Push rigid bodies that we bump into, and apply weight when standing on top of them.
//#define USE_SOUNDS // Play Sfx for footsteps jump and landing.
#define USE_CROUCH // "Crouch" button needs to be set up in the settings.
#define USE_MOUSE_SMOOTHING
#define ESC_TO_QUIT
#define HANDLE_CURSOR // Hide cursor when running compiled project.
#define USE_GRAB_ACTIVATE // Activate activators, and pick up small rigidbody objects, "Activate" button needs to be set up in the settings.
//#define USE_SPRINT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UBSPEntities
{
public class UBSPPlayer : MonoBehaviour
{
	public Camera playerCamera;
	public float baseMoveSpeed = 4.2f;
	public float rotationSpeed = 3.0f;
	#if USE_MOUSE_SMOOTHING
	[RangeAttribute(0.0f, 1.0f)] public float mouseSmoothing = 0.2f;
	private Quaternion targetRotation;
	#endif
	
	#if USE_SOUNDS
	public AudioClip footstepL;
	public AudioClip footstepR;
	public AudioClip jumpSound;
	public AudioClip landingSound;
	[RangeAttribute(0.0f, 1.0f)] public float sfxVolume = 0.5f;
	private AudioSource player_sound_src;
	private bool sfx_playing = false;
	private bool skip_fs = false;
	private float ltm = 1.0f;
	#endif
	[HideInInspector] public CharacterController controller;
	private float moveSpeed = 4.2f;
	private float frame_time = 0.0f;
	private float rotation_input;
	private float look_input;
	private float look_y = 0.0f;
	private float camera_offset_y = 0.72f;
	private float gravity = 0.0f;
	private float crouch_value = -0.01f;
	private float crouch_value_s = 0;
	private Vector3 move_input;
	private Vector3 move_vector;
	private Vector3 inertia_vector;
	private Vector3 move_vector_add;
	private Quaternion player_rotation;
	private Vector3 CamOffsetFull;
	private bool grounded = true;
	private bool land = false;
	private float fall_velocity = 0.0f;
	private int fall_damage = 0;
	private float spring_value = 0.0f;
	private float land_lerp = 0.0f;
	private float cam_spring = 0.0f;
	private float inputMagnitude;
	private float time_sin;
	private RaycastHit hit;
	private RaycastHit hit_surface;
	private const float playerHeight = 1.8f;
	private const float cameraOffset = 0.72f;
	#if USE_GRAB_ACTIVATE
	private bool has_object = false;
	private Rigidbody object_rb = null;
	private float contact_distance = 0;
	#endif
	private Vector3 center_offset;
	private Vector3 vector_down;
	private Vector3 vector_up;
	private Vector3 vector_zero;
	private Vector3 surface_normal;
	private Vector3 tangent;
	#if RB_INTERACTION
	private bool apply_weight;
	private Vector3 apply_weight_point;
	private Rigidbody apply_weight_rb;
	#endif
	
	private float spring_reset_value = 0;

	void Start ()
	{
		CamOffsetFull = new Vector3(0, 0, 0);
		move_input = new Vector3(0, 0, 0);
		move_vector = new Vector3(0, 0, 0);
		inertia_vector = new Vector3(0, 0, 0);
		move_vector_add = new Vector3(0, 0, 0);
		center_offset = new Vector3(0, 0, 0);
		vector_up = new Vector3(0, 1.0f, 0);
		vector_down = new Vector3(0, -1.0f, 0);
		vector_zero = new Vector3(0, 0, 0);
		surface_normal = new Vector3(0, 1.0f, 0);
		player_rotation = transform.rotation;
		controller = GetComponent<CharacterController>();
		controller.skinWidth = 0.03f;
		controller.minMoveDistance = 0;
		controller.height = 1.8f;
		controller.radius = 0.35f;
		#if HANDLE_CURSOR
		if (!Application.isEditor)
		{
			Cursor.visible = false;
		}
		#endif
		
		#if USE_SOUNDS
		GameObject sound_src_obj = new GameObject("PlayerSoundSRC");
		sound_src_obj.transform.position = transform.position + vector_down * 0.8f;
		sound_src_obj.transform.parent = transform;
		player_sound_src = sound_src_obj.AddComponent<AudioSource>();
		player_sound_src.volume = sfxVolume;
		player_sound_src.spatialBlend = 1.0f;
		player_sound_src.minDistance = 0.7f;
		player_sound_src.maxDistance = 5.0f;
		#endif
	}

	void FixedUpdate ()
	{
		#if USE_GRAB_ACTIVATE
		if (has_object)
		{
			object_rb.velocity = (playerCamera.transform.position + (playerCamera.transform.forward * contact_distance) - object_rb.position) * 30.0f;	
		}
		#endif

		#if RB_INTERACTION
		if (apply_weight)
		{
			apply_weight_rb.AddForceAtPosition(vector_down * 800.0f, apply_weight_point, ForceMode.Force); // player_weight_in_kilograms * 9.81
		}
		#endif
	}

	void Update ()
	{
		frame_time = Time.deltaTime;
		move_input.x = Input.GetAxis("Horizontal");
		move_input.z = Input.GetAxis("Vertical");		
		rotation_input = Input.GetAxis("Mouse X") * rotationSpeed;
		look_input = Input.GetAxis("Mouse Y") * rotationSpeed * 0.9f;
		look_y -= look_input;
		look_y = Mathf.Clamp(look_y, -90.0f, 90.0f);
		player_rotation *= Quaternion.Euler(0, rotation_input, 0);
		inputMagnitude = move_input.magnitude;
		time_sin = (float)System.Math.Sin(Time.time * moveSpeed * 2.0f);
		moveSpeed = (crouch_value > 0.5f) ? baseMoveSpeed * 0.35f : baseMoveSpeed;
		#if USE_SPRINT
		if (Input.GetButton("Sprint")) moveSpeed *= 1.7f;
		#endif
		if (controller.isGrounded) // Controller minMoveDistance must be 0 for this to work.
		{
			if(!grounded)
			{
				if (fall_velocity < 0) fall_velocity = -fall_velocity;
				fall_damage = (int)((Mathf.Clamp(fall_velocity, 15.0f, 30.0f) - 15.0f) * 10.0f);
				if (fall_damage > 0)
				{
					// Fall Damage handling goes here.
				}
				if (!land)
				{
					spring_value = fall_velocity * 0.2f;
					land = true;
					land_lerp = 0;
				}
				grounded = true;
				#if USE_SOUNDS
				if (fall_velocity * ltm > 7.0f)
				{
					if (landingSound != null)
					{
						player_sound_src.PlayOneShot(landingSound);
					}
					skip_fs = true;
				}
				ltm = 1.0f;
				#endif
			}
			inertia_vector = player_rotation * move_input;
			if (gravity < 0.3f) gravity = -0.5f;
			if (Physics.Raycast(transform.position, vector_down, out hit_surface, 1.5f))
			{
				surface_normal = hit_surface.normal;
				move_vector = ProjectOnPlane(inertia_vector, surface_normal) * moveSpeed * frame_time;
				if (hit_surface.normal.y < 0.7f)
				{
					tangent = ProjectOnPlane(new Vector3(surface_normal.x, 0, surface_normal.z), surface_normal);
					move_vector += tangent.normalized * 10.0f * frame_time * (1.0f - surface_normal.y);
				}				
				if (move_vector.y > 0) move_vector.y = 0;
				#if RB_INTERACTION
				if (hit_surface.rigidbody != null)
				{
					apply_weight_rb = hit_surface.rigidbody;
					apply_weight_point = hit_surface.point;
					apply_weight = true;
				}
				else
				{
					apply_weight = false;
				}
				#endif
			}
			else
			{
				move_vector = inertia_vector * moveSpeed * frame_time;
			}
			if(Input.GetButtonDown("Jump"))
			{
				if (surface_normal.y > 0.7f)
				{
					gravity = 5.8f; // Controls height of the jump.
					inertia_vector *= 1.42f; // Bunny hop acceleration.
					spring_reset_value = (cam_spring > 0) ? cam_spring / gravity : 0;
					#if USE_SOUNDS
					if (jumpSound != null)
					{
						player_sound_src.PlayOneShot(jumpSound);
					}
					ltm = 2.0f;
					#endif
				}
			}
			if (land)
			{
				if (land_lerp < 1.25f)
				{
					land_lerp += frame_time * Mathf.Clamp(3.0f / spring_value + 0.35f, 1.0f, 3.0f);
				}
				else
				{
					land_lerp = 0;
					land = false;
				}
				cam_spring = SpringCurve(land_lerp) * -0.1f * spring_value;
			}
			#if USE_SOUNDS
			if (inputMagnitude > 0.3f && crouch_value < 0.5f)
			{
				if (time_sin > 0.9f)
				{
					if (!sfx_playing && !skip_fs)
					{
						if (footstepL != null)
						{
							player_sound_src.PlayOneShot(footstepL);
							sfx_playing = true;
						}
					}
					else
					{
						skip_fs = false;
						sfx_playing = true;
					}
				}
				else if (time_sin < -0.9f)
				{
					if (!sfx_playing && !skip_fs)
					{
						if (footstepR != null)
						{
							player_sound_src.PlayOneShot(footstepR);
							sfx_playing = true;
						}
					}
					else
					{
						skip_fs = false;
						sfx_playing = true;
					}
				}
				else
				{
					sfx_playing = false;
				}
			}
			#endif
		}
		else // In air
		{
			grounded = false;
			inertia_vector = Vector3.Lerp(inertia_vector, vector_zero, frame_time * 0.5f);
			move_vector = (inertia_vector + player_rotation * move_input * 0.3f) * moveSpeed * frame_time;
			if (crouch_value > 0.1f || crouch_value < 0.9f) gravity -= 17.0f * frame_time; // Freeze gravity while crouching, for crouch jump to work.
			fall_velocity = -controller.velocity.y;
			if (gravity > 0)
			{
				cam_spring = gravity * spring_reset_value;
				land_lerp = cam_spring;
			}
			else
			{
				land = false;
			}
		}
		#if USE_CROUCH
		if (Input.GetButton("Crouch"))
		{
			if (crouch_value < 1.0f)
			{
				crouch_value += frame_time * 3.0f;
				crouch_value_s = EaseInOut(Mathf.Clamp01(crouch_value));
				center_offset.y = crouch_value_s * -0.5f;
				controller.height = playerHeight - crouch_value_s;
				controller.center = center_offset;
				camera_offset_y = cameraOffset - crouch_value_s * 0.9f;
			}
		}
		else
		{
			if (crouch_value > 0)
			{
				RaycastHit hit_up;
				if (!Physics.SphereCast(playerCamera.transform.position + vector_down * 0.25f, 0.3f, vector_up, out hit_up, 0.3f)) // Check if there is a space for player to raise
				{
					crouch_value -= frame_time * 2.5f;
					crouch_value_s = EaseInOut(Mathf.Clamp01(crouch_value));
					center_offset.y = crouch_value_s * -0.5f;
					controller.height = playerHeight - crouch_value_s;
					controller.center = center_offset;
					camera_offset_y = cameraOffset - crouch_value_s * 0.9f;
				}
			}
		}
		#endif
		move_vector_add.x = 0;
		move_vector_add.y = gravity;
		move_vector_add.z = 0;
		CamOffsetFull.x = 0;
		CamOffsetFull.y = 0;
		CamOffsetFull.z = 0;
		CamOffsetFull.y += (camera_offset_y + cam_spring) + time_sin * 0.035f * inputMagnitude;
		controller.Move(move_vector + move_vector_add * frame_time);
		#if USE_MOUSE_SMOOTHING
		targetRotation = player_rotation * Quaternion.Euler(look_y, 0, 0);
		playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, frame_time * (1.0f - mouseSmoothing) * 50.0f);
		#else
		playerCamera.transform.rotation = player_rotation * Quaternion.Euler(look_y, 0, 0);
		#endif
		playerCamera.transform.position = transform.position + CamOffsetFull;
		#if USE_GRAB_ACTIVATE
		if (!has_object)
		{
			if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Activate"))
			{
				if (Physics.Raycast(playerCamera.transform.position + playerCamera.transform.forward * 0.35f, playerCamera.transform.forward, out hit, 1.5f)) // A little hack so we don't raycast player collider
				{
					UBSPBaseActivator activator = hit.collider.GetComponentInChildren<UBSPBaseActivator>();
					if (activator != null)
					{
						activator.activate(transform);
					}
					else
					{
						object_rb = hit.collider.attachedRigidbody;
						if (object_rb != null && object_rb.mass < 50.0f)
						{
							has_object = true;
							object_rb.freezeRotation = true;
							object_rb.interpolation = RigidbodyInterpolation.Interpolate;
							contact_distance = Vector3.Distance(playerCamera.transform.position, object_rb.transform.position);
						}
					}
				}
			}
			else
			{
				if (Input.GetButton("Fire1") || Input.GetButton("Activate"))
				{
					if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 1.5f))
					{
						if (hit.collider.tag == "activator")
						{
							hit.collider.SendMessage("activate2");
						}
					}
				}	
			}
		}
		else
		{
			if (Input.GetButtonDown("Fire1"))
			{
				has_object = false;
				object_rb.freezeRotation = false;
				object_rb.interpolation = RigidbodyInterpolation.None;
				object_rb.AddForce(playerCamera.transform.forward * 7.0f, ForceMode.Impulse);
				object_rb = null;
			}
			else if (Input.GetButtonDown("Activate"))
			{
				has_object = false;
				object_rb.freezeRotation = false;
				object_rb.interpolation = RigidbodyInterpolation.None;
				object_rb = null;
			}
		}
		#endif

		#if ESC_TO_QUIT
		if (Input.GetKeyDown("escape"))
		{
			Application.Quit();
		}
		#endif
	}
	#if RB_INTERACTION
    void OnControllerColliderHit(ControllerColliderHit hit) // Push rigid bodies, that we bump into.
	{
		Rigidbody body = hit.collider.attachedRigidbody;
		if (body == null || body.isKinematic) return;
		if (hit.moveDirection.y < -0.3F) return;
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
		body.velocity = Vector3.ClampMagnitude((pushDir * 10.0f) / body.mass, 2.0f);
    }
	#endif
	Vector3 ProjectOnPlane (Vector3 vector, Vector3 normal) // Faster and unsafe version of standard function, surface normal is never zero anyway.
	{
		return vector - normal * ((vector.x * normal.x + vector.y * normal.y + vector.z * normal.z) / (normal.x * normal.x + normal.y * normal.y + normal.z * normal.z));
	}
	
	float SpringCurve (float value1) // Simulates amortization behaviour while landing, Input: 0 - 1.25, Output: 0 - 1.0
	{
		if (value1 < 0.5f) return (float)System.Math.Sin(value1 * Mathf.PI);
		return ((float)System.Math.Sin((0.5f + (value1 - 0.5f) * 1.3333333f) * Mathf.PI) + 1.0f) * 0.5f;
	}
	
	float EaseInOut(float value1) // Smooth crouch/uncrouch.
	{
		value1 *= 2.0f;
		if (value1 < 1.0f) return 0.5f * value1 * value1;
		value1 = 2.0f - value1;
		return 1.0f - 0.5f * value1 * value1;
	}
}
}