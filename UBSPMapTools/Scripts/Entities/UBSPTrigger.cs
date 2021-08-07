using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UBSPEntities
{
	public class UBSPTrigger : UBSPBaseActivator
	{
		public UBSPBaseActivator exitTarget;
		public bool once = false;
		public bool partialMatch = false;
		public string restrictName;
		private bool restrict = false;
		
		void Start ()
		{
			restrict = (!string.IsNullOrEmpty(restrictName));
		}
		
		void OnTriggerEnter (Collider c1)
		{
			if (restrict)
			{
				if (partialMatch)
				{
					if (c1.gameObject.name.Contains(restrictName))
					{
						if (target != null) target.trigger();
					}
				}
				else
				{
					if (c1.gameObject.name == restrictName)
					{
						if (target != null) target.trigger();
					}
				}				
			}
			else
			{
				if (target != null) target.trigger();
			}
		}
		void OnTriggerExit(Collider c1)
		{
			if (restrict)
			{
				if (partialMatch)
				{
					if (c1.gameObject.name.Contains(restrictName))
					{
						if (exitTarget != null) exitTarget.trigger();
					}
				}
				else
				{
					if (c1.gameObject.name == restrictName)
					{
						if (exitTarget != null) exitTarget.trigger();
					}
				}				
			}
			else
			{
				if (exitTarget != null) exitTarget.trigger();
			}
		}
	}
}