using UnityEngine;
using System.Collections;

namespace MonteTools.Platforming2D{
	/// <summary>
	/// Controller for 2D movement and collision detection
	/// </summary>
	public class Controller2D : RayCastController {

	#region Public Variables

		/// <summary>
		/// Information about current collisions. This is a custom struct
		/// </summary>
		[HideInInspector]
		public CollisionInfo collisions;
		/// <summary>
		/// The player input.
		/// </summary>
		[HideInInspector]
		public Vector2 playerInput;
		/// <summary>
		/// The maximum angle of climbable slopes.
		/// </summary>
		public float maxClimbAngle = 80f;
		/// <summary>
		/// The max descend angle.
		/// </summary>
		public float maxDescendAngle = 75f;

	#endregion

	#region Private/Protected Variables



	#endregion

	#region Structs

		/// <summary>
		/// Struct that stores collision info.
		/// </summary>
		public struct CollisionInfo{
			/// <summary>
			/// Boolean that determines if there is currently a collision above this object
			/// </summary>
			public bool above;
			/// <summary>
			/// Boolean that determines if there is currently a collision below this object
			/// </summary>
			public bool below;
			/// <summary>
			/// Boolean that determines if there is currently a collision to the left of this object
			/// </summary>
			public bool left;
			///<summary>
			/// Boolean that determines if there is currently a collision to the right of this object
			///</summary> 
			public bool right;
			/// <summary>
			/// Boolean that determines the object is currently climbing a slope
			/// </summary>
			public bool climbingSlope;
			/// <summary>
			/// Boolean that determines the object is currently descending a slope
			/// </summary>
			public bool descendingSlope;
			/// <summary>
			/// The current slope's angle
			/// </summary>
			public float slopeAngle;
			/// <summary>
			/// Last frame's slope's angle
			/// </summary>
			public float slopeAngleOld;
			/// <summary>
			/// Last frame's moveAmount
			/// </summary>
			public Vector2 moveAmountOld;
			/// <summary>
			/// The direction the object is facing towards. 1 is right, -1 is left
			/// </summary>
			public int faceDirection;
			/// <summary>
			/// Are we falling through a platform?
			/// </summary>
			public bool fallingThroughPlatform;

			/// <summary>
			/// Reset this instance.
			/// </summary>
			public void Reset(){
				above = below = false;
				left = right = false;
				climbingSlope = false;
				descendingSlope = false;
				slopeAngleOld = slopeAngle;
				slopeAngle = 0;
			}
		}

	#endregion

	#region Unity Methods

		public override void Awake(){
			base.Awake();
			collisions.faceDirection = 1;
		}

	#endregion

	#region Public Methods

		/// <summary>
		/// Override to call Move without a need for the input
		/// </summary>
		/// <param name="moveAmount">moveAmount.</param>
		/// <param name="standingOnPlatform">If set to <c>true</c> standing on platform.</param>
		public void Move(Vector2 moveAmount, bool standingOnPlatform){
			Move(moveAmount, Vector2.zero, standingOnPlatform);
		}

		/// <summary>
		/// Take the given moveAmount vector, modify it based on collisions and then translate the object
		/// </summary>
		/// <param name="moveAmount">The given moveAmount</param>
		/// <param name="input"> Calling object's input </param>
		/// <param name="standingOnPlatform"> Determines if we are standing on a moving platform at this moment </param>  
		public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false){
			//Place the raycast origins at the right place
			UpdateRaycastOrigins();
			//Reset collision information
			collisions.Reset();
			collisions.moveAmountOld = moveAmount;

			playerInput = input;

			if(moveAmount.x != 0){
				collisions.faceDirection = (int) Mathf.Sign(moveAmount.x);
			}

			if(moveAmount.y < 0){
				DescendSlope(ref moveAmount);
			}

			//Only process vertical collisions if the respective axis on moveAmount is different
			//from zero
			HorizontalCollisions(ref moveAmount);

			if(moveAmount.y != 0){
				VerticalCollisions(ref moveAmount);
			}

			//Translate after processing collisions
			transform.Translate(moveAmount);

			if(standingOnPlatform){
				collisions.below = true;
			}
		}

	#endregion

	#region Private/Protected Methods

		/// <summary>
		/// Process horizontal collisions
		/// </summary>
		/// <param name="moveAmount">The moveAmount vector to modify based on collisions</param>
		private void HorizontalCollisions(ref Vector2 moveAmount){
			float directionX = collisions.faceDirection;
			float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

			if(Mathf.Abs(moveAmount.x) < skinWidth){
				rayLength = 2*skinWidth;
			}

			for(int i=0; i < horizontalRayCount;++i){
				Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
				//Move the origin up depending on which ray are we processing
				rayOrigin += Vector2.up * (horizontalRaySpacing * i);
				RaycastHit2D hit =  Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

				#if DEBUG
				Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);
				#endif

				if(hit){
					if(hit.distance == 0) continue;
					
					float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
					//Check if the slope is climbable. Note that this is only checked on the bottom ray to avoid innacuracies
					if(i == 0 && slopeAngle <= maxClimbAngle){
						float distanceToSlopeStart = 0;
						//There is only distance to the slope if the angle is actually different from that of the last slope
						//In this case, moveAmount x will be nullified to get to the start of the slope
						if(slopeAngle != collisions.slopeAngleOld){
							if(collisions.descendingSlope){
								collisions.descendingSlope = false;
								moveAmount = collisions.moveAmountOld;
							}
							//Remember to account for the skin width or the object will look a little distanced from the slope
							distanceToSlopeStart = hit.distance - skinWidth;
							//Substract from the x moveAmount to reset to the slope's starting place
							moveAmount.x -= distanceToSlopeStart * directionX;
						}
						ClimbSlope(ref moveAmount, slopeAngle);
						//Now that the slope calculations have been done, we can add again the distance to the slope
						//This will of course only happen if the moveAmount x was nullified first
						moveAmount.x += distanceToSlopeStart * directionX;
					}

					//If we are not climbing a slope or we are but we approach a steep slope / wall
					if(!collisions.climbingSlope || slopeAngle > maxClimbAngle){
						//Put ourselves right at the end of the ray (start the wall)
						moveAmount.x = (hit.distance - skinWidth) * directionX;
						rayLength = hit.distance;

						//If we were climbing a slope, we need to change the moveAmount y to match the angle of the 
						//now modified moveAmount x
						if(collisions.climbingSlope){
							moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
						}

						//Update the left and right collisions since we have encountered a change in elevation
						collisions.left = directionX == -1;
						collisions.right = directionX == 1;
					}
				}
			}
		}

		/// <summary>
		/// Process vertical collisions
		/// </summary>
		/// <param name="moveAmount">The moveAmount vector to modify based on collisions</param>
		private void VerticalCollisions(ref Vector2 moveAmount){
			float directionY = Mathf.Sign(moveAmount.y);
			float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;
			for(int i=0; i < verticalRayCount;++i){
				Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
				//Move the origin right depending on which ray we are processing
				rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
				RaycastHit2D hit =  Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

				#if DEBUG
				Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);
				#endif

				if(hit){
					if(hit.collider.tag == "MoveThrough"){
						if(directionY == 1 || hit.distance == 0){
							continue;
						}
						if(collisions.fallingThroughPlatform){
							continue;
						}
						if(playerInput.y == -1){
							collisions.fallingThroughPlatform = true;
							Invoke("ResetFallingThroughPlatform",0.5f);
							continue;
						}
					}
					moveAmount.y = (hit.distance - skinWidth) * directionY;
					rayLength = hit.distance;

					//If there was a collision, the new moveAmount x must match the moveAmount y
					if(collisions.climbingSlope){
						moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
					}

					//Update collision status from above and below based on the direction of the collision
					collisions.below = directionY == -1;
					collisions.above = directionY == 1;
				}
			}

			//We need to account for the special case where we find another slope after already being in one
			if(collisions.climbingSlope){
				float directionX = Mathf.Sign(moveAmount.x);
				//Make a raycast horizontally to determine the next slope's angle. The origin of this ray has to take into account the y moveAmount
				//to get the appropiate height
				rayLength = Mathf.Abs(moveAmount.x + skinWidth);
				Vector2 rayOrigin = (directionX == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

				if(hit){
					float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
					//if we do have a change of slope, change the current slope angle and move accordingly in the horizontal axis
					if(slopeAngle != collisions.slopeAngle){
						moveAmount.x = (hit.distance - skinWidth) * directionX;
						collisions.slopeAngle = slopeAngle;
					}
				}
			}
		}

		/// <summary>
		/// Process slope movement
		/// </summary>
		/// <param name="moveAmount">moveAmount.</param>
		/// <param name="slopeAngle">Slope angle.</param>
		private void ClimbSlope(ref Vector2 moveAmount, float slopeAngle){
			//The slope does not slow us down at the moment
			float moveDistance = Mathf.Abs(moveAmount.x);
			float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
			if(moveAmount.y <= climbmoveAmountY){
				//Set x and y to the two shorter sides of the triangle the slope is forming relative to the object
				moveAmount.y = climbmoveAmountY;
				moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
				collisions.below = true;
				collisions.climbingSlope = true;
				collisions.slopeAngle = slopeAngle;
			}
		}

		/// <summary>
		/// Descends the slope.
		/// </summary>
		/// <param name="moveAmount">moveAmount.</param>
		private void DescendSlope(ref Vector2 moveAmount){
			float directionX = Mathf.Sign(moveAmount.x);
			Vector2 rayOrigin = (directionX == -1 ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);

			if(hit){
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if(slopeAngle != 0 && slopeAngle <= maxDescendAngle){
					if(Mathf.Sign(hit.normal.x) == directionX){
						if(hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)){
							float moveDistance = Mathf.Abs(moveAmount.x);
							float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
							moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
							moveAmount.y -= descendmoveAmountY;

							collisions.slopeAngle = slopeAngle;
							collisions.descendingSlope = true;
							collisions.below = true;
						}
					}
				}
			}
		}

		private void ResetFallingThroughPlatform(){
			collisions.fallingThroughPlatform = false;
		}

	#endregion

	}
}