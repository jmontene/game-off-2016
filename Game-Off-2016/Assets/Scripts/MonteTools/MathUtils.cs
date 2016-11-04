using UnityEngine;
using System.Collections;

namespace MonteTools{
	public class MathUtils{

	#region Public Static Methods

		/// <summary>
		/// Returns a float that represents the easing y value using the standard ease formula
		/// </summary>
		public static float Ease(float x, float a){
			return Mathf.Pow(x,a) / (Mathf.Pow(x,a) + Mathf.Pow(1-x,a));
		}

	#endregion

	}
}
