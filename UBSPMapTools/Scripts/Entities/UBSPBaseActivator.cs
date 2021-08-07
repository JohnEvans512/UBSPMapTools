using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UBSPEntities
{
	public class UBSPBaseActivator : MonoBehaviour
	{
		[Tooltip("Can be set in BSP editor")] public UBSPBaseActivator target;
		public virtual void activate (Transform actor)
		{
			if (target != null) target.trigger();
		}
		
		public virtual void trigger ()
		{
			
		}
	}
}
