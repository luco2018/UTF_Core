using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    public class EnumMaskOption : MonoBehaviour
    {

        public Text label;
        public Toggle toggle;
        public DropdownEnumMask enumController;

		public void Selected(bool state)
		{
			if(enumController.active)
            	enumController.SelectOption(transform.GetSiblingIndex(), this, state);
        }

    }
}
