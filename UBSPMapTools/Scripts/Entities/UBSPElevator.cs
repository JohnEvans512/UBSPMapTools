using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UBSPEntities
{
	public class UBSPElevator : UBSPBaseActivator
	{
		public Transform[] levels;
		public int startLevel = 0;
		public float speed = 1.0f;
		public bool directControl = false;
		public AudioClip moveSound;
		public AudioClip stopSound;
		[RangeAttribute(0.0f, 1.0f)] public float soundVolume = 0.8f;
		public UnityEvent onStart;
		public UnityEvent onReached;
		[HideInInspector] public int current_level;
		[HideInInspector] public int target_level;
		private AudioSource src1 = null;
		private Vector3 current_pos;
		private Vector3 target_pos;
		private bool moving = false;
		private float soundPitch;
		private bool has_sound = false;
		
		void Start () 
		{
			if (levels[startLevel] != null)
			{
				transform.position = levels[startLevel].position;
				current_level = startLevel;
				target_level = 0;
			}
			if (moveSound != null || stopSound != null)
			{
				src1 = gameObject.AddComponent<AudioSource>();
				src1.volume = soundVolume;
				src1.spatialBlend = 1.0f;
				src1.maxDistance = 10.0f;
				src1.minDistance = 1.0f;
				src1.playOnAwake = false;
				src1.loop = false;
				has_sound = true;
			}
		}
		
		public override void activate (Transform actor)
		{
			if (directControl) trigger();
		}
		
		public override void trigger ()
		{
			if (!moving && levels.Length > 1)
			{
				if (current_level == 0)
				{
					target_level = 1;
					StartCoroutine("ElevatorMove");
					onStart.Invoke();
				}
				else
				{
					target_level = 0;
					StartCoroutine("ElevatorMove");
					onStart.Invoke();
				}
			}
		}
		
		public void gotolevel (int level)
		{
			target_level = level;
			StartCoroutine("ElevatorMove");
			onStart.Invoke();
		}
		
		IEnumerator ElevatorMove ()
		{
			if (has_sound)
			{
				src1.clip = moveSound;
				src1.loop = true;
				soundPitch = 0.8f;
				src1.pitch = soundPitch;
				src1.Play();
			}
			float i1 = 0;
			current_pos = levels[current_level].position;
			target_pos = levels[target_level].position;		
			float multiplier = 1.0f / Vector3.Distance(current_pos, target_pos);
			moving = true;
			while (i1 < 1.0f)
			{
				i1 += speed * Time.deltaTime * multiplier;
				transform.position = Vector3.Lerp(current_pos, target_pos, EaseInOut(Mathf.Clamp01(i1)));
				if (has_sound)
				{
					soundPitch = Mathf.Clamp01(1.3f - Mathf.Abs(i1 - 0.5f));
					src1.pitch = soundPitch;
				}
				yield return null;
			}
			current_level = target_level;
			moving = false;
			if (has_sound)
			{
				src1.Stop();
				src1.pitch = 1.0f;
				src1.loop = false;
			}
			if (stopSound != null)
			{
				src1.clip = stopSound;
				src1.Play();
			}
			if (target != null) target.trigger();
			onReached.Invoke();
		}
		
		float EaseInOut(float value1)
		{
			value1 *= 2.0f;
			if (value1 < 1.0f) return 0.5f * value1 * value1;
			value1 = 2.0f - value1;
			return 1.0f - 0.5f * value1 * value1;
		}
	}
}