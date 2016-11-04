using UnityEngine;
using System.Collections;

namespace MonteTools.Platforming2D{
	/// <summary>
	/// Controller for raycasting operations
	/// </summary>
	[RequireComponent (typeof (BoxCollider2D))]
	public class RayCastController : MonoBehaviour {

	#region Public Variables

		/// <summary>
		/// The horizontal ray count when raycasting for collisions.
		/// </summary>
		[HideInInspector]
		public int horizontalRayCount;
		/// <summary>
		/// The vertical ray count.
		/// </summary>
		[HideInInspector]
		public int verticalRayCount;
		/// <summary>
		/// The mask used to determined objects that can be collided with
		/// </summary>
		public LayerMask collisionMask;
		/// <summary>
		/// This object's collider
		/// </summary>
		new public BoxCollider2D collider;

	#endregion

	#region Private/Protected Variables

		/// <summary>
		/// Small offset for raycasts and collision detection so it does not happen in the borders of the collider
		/// </summary>
		protected const float skinWidth = 0.015f;
		/// <summary>
		/// The horizontal space between vertical rays.
		/// </summary>
		protected float horizontalRaySpacing;
		/// <summary>
		/// The vertical space between horizontal rays.
		/// </summary>
		protected float verticalRaySpacing;
		/// <summary>
		/// Custom struct that holds the 4 corners of the collider for raycasting
		/// </summary>
		protected RayCastOrigins raycastOrigins;
		/// <summary>
		/// The distance between rays.
		/// </summary>
		protected const float dstBetweenRays = 0.25f;

	#endregion

	#region Structs

		/// <summary>
		/// Struct to store raycast origins (usually the collider's corner points)
		/// </summary>
		protected struct RayCastOrigins {
			/// <summary>
			/// The top left point.
			/// </summary>
			public Vector2 topLeft;
			/// <summary>
			/// The top right point.
			/// </summary>
			public Vector2 topRight;
			/// <summary>
			/// The bottom left point.
			/// </summary>
			public Vector2 bottomLeft;
			/// <summary>
			/// The bottom right point.
			/// </summary>
			public Vector2 bottomRight;
		}
	#endregion

	#region Unity Methods

		/// <summary>
		/// Awake this instance.
		/// </summary>
		public virtual void Awake() {
			collider = GetComponent<BoxCollider2D>();
			CalculateRaySpacing();
		}

	#endregion

	#region Private/Protected Methods

		/// <summary>
		/// Updates the raycast origins.
		/// </summary>
		protected void UpdateRaycastOrigins(){
			//Get the collider bounds expanded negatively depending on skin width
			Bounds bounds = GetExpandedBounds();

			raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
			raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
			raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
			raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
		}

		/// <summary>
		/// Calculates the ray spacing and ray counts.
		/// </summary>
		protected void CalculateRaySpacing(){
			Bounds bounds = GetExpandedBounds();

			float boundsWidth = bounds.size.x;
			float boundsHeight = bounds.size.y;

			//There must be a minimum of 2 rays
			horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
			verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

			horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
			verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
		}

		/// <summary>
		/// Get the collider bounds expanded negatively depending on skinWidth
		/// </summary>
		/// <returns>The expanded bounds.</returns>
		protected Bounds GetExpandedBounds(){
			Bounds bounds = collider.bounds;
			bounds.Expand(skinWidth * -2);
			return bounds;
		}

	#endregion

	}
}