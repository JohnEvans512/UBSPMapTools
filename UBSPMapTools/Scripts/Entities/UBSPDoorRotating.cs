//#define DBG_ON
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UBSPEntities
{
	public class UBSPDoorRotating : UBSPBaseActivator
	{
		public enum Axis
		{
			X = 0,
			Y = 1,
			Z = 2
		}
		public Axis axis = Axis.Y;
		public enum DoorState
		{
			Closed = 0,
			OpenCW = 1,
			OpenCCW = 2
		}
		public bool CCW = false;
		public bool autoDir = true;
		public DoorState startState = DoorState.Closed;
		public float angle = 89.0f;
		[Tooltip("Degrees Per Sec")] public float speed = 90.0f;
		[Tooltip("0 - Don't close")] public float closeDelay = 0;
		[HideInInspector] public bool isOpen = false;
		[HideInInspector] public bool moving = false;
		public AudioClip moveSound;
		public AudioClip closeSound;
		[RangeAttribute(0.0f, 1.0f)] public float soundVolume = 0.8f;
		
		public UnityEvent onStartOpen;
		public UnityEvent onFullyOpen;
		public UnityEvent onStartClose;
		public UnityEvent onFullyClosed;

		private Quaternion closedRotation;
		private float multiplier = 1.0f;
		private bool open_dir = true; // CW
		private AudioSource src1 = null;
		private float angle2;
		private float i1 = 0;
		private bool waiting = false;
		private bool zy = false;

		void Start () 
		{
			closedRotation = transform.localRotation;
			MeshFilter mf1 = GetComponentInChildren<MeshFilter>();
			if (mf1.sharedMesh.bounds.max.z - mf1.sharedMesh.bounds.min.z > mf1.sharedMesh.bounds.max.x - mf1.sharedMesh.bounds.min.x) zy = true;
			open_dir = !CCW;
			if (moveSound != null || closeSound != null)
			{
				src1 = gameObject.AddComponent<AudioSource>();
				src1.volume = soundVolume;
				src1.spatialBlend = 1.0f;
				src1.maxDistance = 10.0f;
				src1.minDistance = 1.0f;
				src1.playOnAwake = false;
				src1.loop = false;
			}
			if (startState != DoorState.Closed)
			{
				angle2 = (startState == DoorState.OpenCW) ? angle : -angle;
				transform.localRotation = closedRotation * Quaternion.Euler(0, angle2, 0);
				isOpen = true;
				open_dir = (startState == DoorState.OpenCW);
			}
		}

		public override void activate (Transform actor)
		{
			#if DBG_ON
			Debug.Log("Door "+gameObject.name+" activated by "+actor.gameObject.name);
			#endif
			if (!moving)
			{
				if (isOpen)
				{
					if (moveSound != null)
					{
						src1.clip = moveSound;
						src1.Play();
					}
					StartCoroutine("DoorClose");
				}
				else
				{
					if (actor != null && autoDir && axis == Axis.Y) 
					{
						if (zy)
						{
							if (CCW) open_dir = (transform.InverseTransformPoint(actor.position).x > 0);
							else open_dir = (transform.InverseTransformPoint(actor.position).x < 0);							
						}
						else
						{
							if (CCW) open_dir = (transform.InverseTransformPoint(actor.position).z > 0);
							else open_dir = (transform.InverseTransformPoint(actor.position).z < 0);
						}
					}
					if (moveSound != null)
					{
						src1.clip = moveSound;
						src1.Play();
					}
					StartCoroutine("DoorOpen");
				}
			}
		}
		
		IEnumerator DoorOpen ()
		{
			onStartOpen.Invoke();
			moving = true;
			i1 = 0;
			angle2 = angle;
			if (!open_dir) angle2 = -angle;
			multiplier = 1.0f / angle;
			while (i1 < 1.0f)
			{
				i1 += speed * Time.deltaTime * multiplier;
				if (axis == Axis.Y) transform.localRotation = closedRotation * Quaternion.Euler(0, (-(float)System.Math.Cos(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) + 1.0f) * angle2, 0); // Smooth start
				else if (axis == Axis.X) transform.localRotation = closedRotation * Quaternion.Euler((-(float)System.Math.Cos(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) + 1.0f) * angle2, 0, 0);
				else transform.localRotation = closedRotation * Quaternion.Euler(0, 0, (-(float)System.Math.Cos(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) + 1.0f) * angle2);
				yield return null;
			}
			moving = false;
			isOpen = true;
			onFullyOpen.Invoke();
			if (target != null) target.trigger();
			if (closeDelay > 0 && !waiting) StartCoroutine("CloseWithDelay");
		}
		
		IEnumerator DoorClose ()
		{
			onStartClose.Invoke();
			moving = true;
			i1 = 1.0f;
			angle2 = angle;
			if (!open_dir) angle2 = -angle;
			multiplier = 1.0f / angle;
			while (i1 > 0)
			{
				i1 -= speed * Time.deltaTime * multiplier;
				if (axis == Axis.Y) transform.localRotation = closedRotation * Quaternion.Euler(0, (float)System.Math.Sin(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) * angle2, 0);
				else if (axis == Axis.X) transform.localRotation = closedRotation * Quaternion.Euler((float)System.Math.Sin(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) * angle2, 0, 0);
				else transform.localRotation = closedRotation * Quaternion.Euler(0, 0, (float)System.Math.Sin(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) * angle2);
				yield return null;
			}	
			moving = false;
			isOpen = false;
			if (closeSound != null)
			{
				src1.clip = closeSound;
				src1.Play();
			}
			onFullyClosed.Invoke();
		}

		IEnumerator CloseWithDelay ()
		{
			waiting = true;
			yield return new WaitForSeconds(closeDelay);
			waiting = false;
			if (!moving && isOpen) StartCoroutine("DoorClose");
		}

		public override void trigger ()
		{
			if (!moving)
			{
				if (isOpen)
				{
					if (moveSound != null)
					{
						src1.clip = moveSound;
						src1.Play();
					}
					StartCoroutine("DoorClose");
				}
				else
				{
					if (moveSound != null)
					{
						src1.clip = moveSound;
						src1.Play();
					}
					StartCoroutine("DoorOpen");
				}
			}
		}

		public void breakdoor ()
		{
			gameObject.AddComponent<Rigidbody>().mass = 10.0f;
			Destroy(this);
		}
	}
}
