using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UBSPEntities
{	
	public class UBSPTriggerParent : UBSPBaseActivator
	{
		void OnTriggerEnter (Collider c1)
		{
			if (c1.transform.parent == null)
			{
				c1.transform.parent = transform.parent;
			}
		}
		void OnTriggerExit(Collider c1)
		{
			if (c1.transform.parent == transform.parent)
			{
				c1.transform.parent = null;
			}
		}
		
		public override void activate (Transform actor)
		{
			if (transform.parent != null)
			{
				UBSPBaseActivator a1 = transform.parent.GetComponent<UBSPBaseActivator>();
				if (a1 != null) a1.activate(actor);
			}
		}
	}
}