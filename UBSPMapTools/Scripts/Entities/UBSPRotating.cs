using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UBSPEntities
{
	public class UBSPRotating : MonoBehaviour
	{
		public enum Axis
		{
			X = 0,
			Y = 1,
			Z = 2
		}
		public float speed = 90.0f;
		public Axis axis = Axis.Y;
		public Space space = Space.Self;
		[Tooltip("Optional")]public AudioClip sound;
		[RangeAttribute(0.0f, 1.0f)]public float soundVolume = 0.8f;
		public float soundRange = 10.0f;
		[RangeAttribute(0.0f, 1.0f)]public float sound2DTo3D = 1.0f;
		private Vector3 rv;

		void Start () 
		{
			switch (axis)
			{
				case Axis.X:
				rv = new Vector3(speed, 0, 0);
				break;
				case Axis.Y:
				rv = new Vector3(0, speed, 0);
				break;
				case Axis.Z:
				rv = new Vector3(0, 0, speed);
				break;
			}
			if (sound != null)
			{
				AudioSource src = gameObject.AddComponent<AudioSource>();
				src.clip = sound;
				src.volume = soundVolume;
				src.maxDistance = soundRange;
				src.minDistance = soundRange * 0.1f;
				src.spatialBlend = sound2DTo3D;
				src.playOnAwake = true;
				src.loop = true;
				src.Play();
			}
		}

		void Update () 
		{
			transform.Rotate(rv * Time.deltaTime, space);
		}
	}
}