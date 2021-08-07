//#define DBG_ON
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UBSPEntities
{
	public class UBSPDoorSliding : UBSPBaseActivator
	{
		public enum Axis
		{
			X = 0,
			Y = 1,
			Z = 2
		}
		public Axis axis = Axis.X;
		public float speed = 1.0f;
		public bool autoDistance = true;
		public float distance = 1.0f;
		public bool reverse = false;
		public float leap = 0.01f;
		public bool startOpen = false;
		[Tooltip("0 - Don't close")] public float closeDelay = 0;
		public AudioClip moveSound;
		public AudioClip closeSound;
		[RangeAttribute(0.0f, 1.0f)] public float soundVolume = 0.8f;
		
		public UnityEvent onStartOpen;
		public UnityEvent onFullyOpen;
		public UnityEvent onStartClose;
		public UnityEvent onFullyClosed;

		private Vector3 closedPos;
		private Vector3 openPos;
		private float move_distance;
		private bool isOpen;
		private bool moving = false;
		private float multiplier;
		private float i1;
		private AudioSource src1 = null;
		private bool waiting = false;

		void Start () 
		{
			closedPos = transform.localPosition;
			openPos = new Vector3(0, 0, 0);
			move_distance = distance;
			MeshFilter mf1 = GetComponentInChildren<MeshFilter>();
			if (mf1 != null)
			{
				if (axis == Axis.X)
				{
					if (autoDistance) move_distance = mf1.sharedMesh.bounds.max.x - mf1.sharedMesh.bounds.min.x;
					if (reverse) openPos = closedPos - transform.right.normalized * (move_distance - leap);
					else openPos = closedPos + transform.right.normalized * (move_distance - leap);
				}
				else if (axis == Axis.Y) 
				{
					if (autoDistance) move_distance = mf1.sharedMesh.bounds.max.y - mf1.sharedMesh.bounds.min.y;
					if (reverse) openPos = closedPos - transform.up.normalized * (move_distance - leap);
					else openPos = closedPos + transform.up.normalized * (move_distance - leap);
				}
				else 
				{
					if (autoDistance) move_distance = mf1.sharedMesh.bounds.max.z - mf1.sharedMesh.bounds.min.z;
					if (reverse) openPos = closedPos - transform.forward.normalized * (move_distance - leap);
					else openPos = closedPos + transform.forward.normalized * (move_distance - leap);
				}
			}
			if (startOpen)
			{
				transform.localPosition = openPos;
				isOpen = true;
			}
			else
			{
				isOpen = false;
			}
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
			multiplier = (move_distance - leap > 0) ? (1.0f / (move_distance - leap)) : 1.0f;
			while (i1 < 1.0f)
			{
				i1 += speed * Time.deltaTime * multiplier;
				transform.localPosition = Vector3.Lerp(closedPos, openPos, -(float)System.Math.Cos(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) + 1.0f);
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
			i1 = 0;
			multiplier = (move_distance - leap > 0) ? (1.0f / (move_distance - leap)) : 1.0f;
			while (i1 < 1.0f)
			{
				i1 += speed * Time.deltaTime * multiplier;
				transform.localPosition = Vector3.Lerp(openPos, closedPos, -(float)System.Math.Cos(Mathf.Clamp01(i1) * (Mathf.PI * 0.5f)) + 1.0f);
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