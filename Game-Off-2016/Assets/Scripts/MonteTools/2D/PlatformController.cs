using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using MonteTools;

namespace MonteTools.Platforming2D{
	/// <summary>
	/// Controller for moving platforms
	/// </summary>
	public class PlatformController : RayCastController {

	#region Public Variables

		/// <summary>
		/// The passenger collision mask.
		/// </summary>
		public LayerMask passengerMask;
		/// <summary>
		/// Waypoints for automated movement relative to the platform
		/// </summary>
		public Vector3[] localWaypoints;
		/// <summary>
		/// The platform speed.
		/// </summary>
		public float speed;
		/// <summary>
		/// Determines if movement is cyclic
		/// </summary>
		public bool cyclic;
		/// <summary>
		/// The wait time between waypoints.
		/// </summary>
		public float waitTime;
		/// <summary>
		/// The ease value.
		/// </summary>
		[Range(1,3)]
		public float easeValue;

	#endregion

	#region Private/Protected Variables

		/// <summary>
		/// The list of passenger data for the platform
		/// </summary>
		private List<PassengerMovement> passengerMovement;
		/// <summary>
		/// A dictionary to cache the passenger's Controller2D
		/// </summary>
		private Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();
		/// <summary>
		/// Waypoints for automated movement
		/// </summary>
		private Vector3[] globalWaypoints;
		/// <summary>
		/// The index of the from waypoint.
		/// </summary>
		private int fromWaypointIndex = 0;
		/// <summary>
		/// The percent between waypoints.
		/// </summary>
		private float percentBetweenWaypoints = 0.0f;
		/// <summary>
		/// The next move time.
		/// </summary>
		private float nextMoveTime;

	#endregion

	#region Structs

		/// <summary>
		/// Stores platform passenger information
		/// </summary>
		struct PassengerMovement{
			/// <summary>
			/// The transform.
			/// </summary>
			public Transform transform;
			/// <summary>
			/// The velocity.
			/// </summary>
			public Vector3 velocity;
			/// <summary>
			/// Whether the passenger is currently standing on the platform or not
			/// </summary>
			public bool standingOnPlatform;
			/// <summary>
			/// Whether the passenger should move before the platform or not
			/// </summary>
			public bool moveBeforePlatform;

			/// <summary>
			/// Initializes a new instance of the <see cref="PlatformController+PassengerMovement"/> struct.
			/// </summary>
			/// <param name="_transform">Transform.</param>
			/// <param name="_velocity">Velocity.</param>
			/// <param name="_standingOnPlatform">If set to <c>true</c> standing on platform.</param>
			/// <param name="_moveBeforePlatform">If set to <c>true</c> move before platform.</param>
			public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform){
				transform = _transform;
				velocity = _velocity;
				standingOnPlatform = _standingOnPlatform;
				moveBeforePlatform = _moveBeforePlatform;
			}
		}

	#endregion

	#region Unity Methods

		/// <summary>
		/// Awake this instance.
		/// </summary>
		public override void Awake() {
			base.Awake();

			globalWaypoints = new Vector3[localWaypoints.Length];
			for(int i=0; i < localWaypoints.Length;++i){
				globalWaypoints[i] = localWaypoints[i] + transform.position;
			}
		}
		
		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update () {
			UpdateRaycastOrigins();

			Vector3 velocity = CalculatePlatformMovement();

			CalculatePassengerMovement(velocity);

			MovePassenger(true);
			transform.Translate(velocity);
			MovePassenger(false);
		}

		/// <summary>
		/// Raises the draw gizmos event.
		/// </summary>
		void OnDrawGizmos(){
			if(localWaypoints != null && Selection.Contains(gameObject)){
				Gizmos.color = Color.red;
				float size = 0.3f;

				for(int i=0;i < localWaypoints.Length; ++i){
					Vector3 globalWaypointPos = (Application.isPlaying ? globalWaypoints[i] : localWaypoints[i] + transform.position);
					Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
					Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
				}
			}
		}

	#endregion

	#region Private/Protected Methods

		/// <summary>
		/// Calculates the platform movement between the waypoints
		/// </summary>
		/// <returns>The platform movement speed vector.</returns>
		private Vector3 CalculatePlatformMovement(){
			if(Time.time < nextMoveTime){
				return Vector3.zero;
			}
			fromWaypointIndex %= globalWaypoints.Length;
			int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
			float distanceBetweenWayPoints = Vector3.Distance(globalWaypoints[fromWaypointIndex],globalWaypoints[toWaypointIndex]);
			//Divide between distance to get an accurate percentage. Otherwise we always sum the same percentage no matter what distance there is
			//Between the two waypoints
			percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWayPoints;
			percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
			float easedPercentBetweenWaypoints = MathUtils.Ease(percentBetweenWaypoints, easeValue);

			Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

			if(percentBetweenWaypoints >= 1.0f){
				percentBetweenWaypoints = 0.0f;
				fromWaypointIndex++;
				if(!cyclic){
					if(fromWaypointIndex >= globalWaypoints.Length-1){
						fromWaypointIndex = 0;
						//Reverse the array so the platform goes back and forth through the path made by the waypoints
						System.Array.Reverse(globalWaypoints);
					}
				}
				nextMoveTime = Time.time + waitTime;
			}

			return newPos - transform.position;
		}

		/// <summary>
		/// Moves the passenger.
		/// </summary>
		/// <param name="beforeMovePlatform">If set to <c>true</c> move before platform.</param>
		private void MovePassenger(bool beforeMovePlatform){
			foreach (PassengerMovement passenger in passengerMovement){
				if(!passengerDictionary.ContainsKey(passenger.transform)){
					passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
				}
				if(passenger.moveBeforePlatform == beforeMovePlatform){
					passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
				}
			}
		}

		/// <summary>
		/// Calculates the platform's passenger's movements
		/// </summary>
		/// <param name="velocity">Platform's velocity</param>
		private void CalculatePassengerMovement(Vector3 velocity){
			//Hash set to only apply movement for the first ray that hits a given passenger
			HashSet<Transform> movedPassengers = new HashSet<Transform>();
			passengerMovement = new List<PassengerMovement>();
			float directionX = Mathf.Sign(velocity.x);
			float directionY = Mathf.Sign(velocity.y);
			float rayLength = 0.0f;
			RaycastHit2D hit;
			Vector2 rayOrigin;

			//Vertical moving platform
			if(velocity.y != 0){
				rayLength = Mathf.Abs(velocity.y) + skinWidth;

				for(int i=0; i < verticalRayCount; ++i){
					rayOrigin = (directionY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft);
					rayOrigin += Vector2.right * (verticalRaySpacing * i);
					hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

					if(hit && hit.distance != 0) {

						if(!movedPassengers.Contains(hit.transform)){
							movedPassengers.Add(hit.transform);
							float pushY = velocity.y - (hit.distance - skinWidth) * directionY;
							float pushX = (directionY == 1) ? velocity.x : 0.0f;

							passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), directionY == 1, true));
						}
					}
				}
			}

			//Horizontal moving platform
			if(velocity.x != 0){
				rayLength = Mathf.Abs(velocity.x) + skinWidth;

				for(int i=0; i< horizontalRayCount; ++i){
					rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
					rayOrigin += Vector2.up * (horizontalRaySpacing * i);
					hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

					if(hit && hit.distance != 0){
						if(!movedPassengers.Contains(hit.transform)){
							movedPassengers.Add(hit.transform);
							float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
							//Small force downwards is applied in order to be able to jump when faced against a platform
							float pushY = -skinWidth;

							passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), false, true));
						}
					}
				}
			}

			//Case where passenger is on top of horizontally moving or downward platform
			if(directionY == -1 || velocity.y == 0 && velocity.x != 0){
				rayLength = skinWidth * 2f;

				for(int i=0; i < verticalRayCount; ++i){
					rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
					rayOrigin += Vector2.right * (verticalRaySpacing * i);
					hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

					if(hit && hit.distance != 0) {
						if(!movedPassengers.Contains(hit.transform)){
							movedPassengers.Add(hit.transform);
							float pushY = velocity.y;
							float pushX = velocity.x;

							passengerMovement.Add(new PassengerMovement(hit.transform,new Vector3(pushX,pushY), true, false));
						}
					}
				}
			}
		}

	#endregion
	}
}
