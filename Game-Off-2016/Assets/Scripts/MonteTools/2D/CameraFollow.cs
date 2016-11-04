using UnityEngine;
using System.Collections;

namespace MonteTools.Platforming2D{
	public class CameraFollow : MonoBehaviour {

	#region Public Variables
		/// <summary>
		/// The target to follow.
		/// </summary>
		public Controller2D target;
		/// <summary>
		/// The size of the focus area.
		/// </summary>
		public Vector2 focusAreaSize;
		/// <summary>
		/// The vertical offset.
		/// </summary>
		public float verticalOffset;
		/// <summary>
		/// The vertical smooth time.
		/// </summary>
		public float verticalSmoothTime;
		/// <summary>
		/// The look ahead distance x.
		/// </summary>
		public float lookAheadDistanceX;
		/// <summary>
		/// The look smooth time x.
		/// </summary>
		public float lookSmoothTimeX;
	#endregion

	#region Private/Protected Variables
		/// <summary>
		/// The focus area.
		/// </summary>
		private FocusArea focusArea;
		/// <summary>
		/// The current look ahead x.
		/// </summary>
		private float currentLookAheadX;
		/// <summary>
		/// The target look ahead x.
		/// </summary>
		private float targetLookAheadX;
		/// <summary>
		/// The look ahead dir x.
		/// </summary>
		private float lookAheadDirX;
		/// <summary>
		/// The smooth look velocity x.
		/// </summary>
		private float smoothLookVelocityX;
		/// <summary>
		/// The smooth velocity x.
		/// </summary>
		private float smoothVelocityX;
		/// <summary>
		/// The smooth velocity y.
		/// </summary>
		private float smoothVelocityY;
		/// <summary>
		/// The look ahead stopped.
		/// </summary>
		private bool lookAheadStopped;
	#endregion

	#region Unity Methods

		/// <summary>
		/// Start this instance.
		/// </summary>
		void Start(){
			focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
		}

		/// <summary>
		/// Late Update.
		/// </summary>
		void LateUpdate(){
			focusArea.Update(target.collider.bounds);

			Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;


			if(focusArea.velocity.x != 0){
				lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
				if(Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0){
					lookAheadStopped = false;
					targetLookAheadX = lookAheadDirX * lookAheadDistanceX;
				}else{
					if(!lookAheadStopped){
						lookAheadStopped = true;
						targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDistanceX - currentLookAheadX)/4f;
					}
				}
			}

			currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX,targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

			focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
			focusPosition += Vector2.right * currentLookAheadX;
			transform.position = (Vector3) focusPosition + Vector3.forward * -10;
		}

		/// <summary>
		/// Raises the draw gizmos event.
		/// </summary>
		void OnDrawGizmos(){
			Gizmos.color = new Color(1,0,0,0.5f);
			Gizmos.DrawCube(focusArea.center, focusAreaSize);
		}

	#endregion

	#region Structs
		/// <summary>
		/// Struct for focus area data
		/// </summary>
		struct FocusArea{
			/// <summary>
			/// The center.
			/// </summary>
			public Vector2 center;
			/// <summary>
			/// The left coordinate.
			/// </summary>
			public float left;
			/// <summary>
			/// The right coordinate.
			/// </summary>
			public float right;
			/// <summary>
			/// The top coordinate.
			/// </summary>
			public float top;
			/// <summary>
			/// The bottom coordinate.
			/// </summary>
			public float bottom;
			/// <summary>
			/// The velocity of the focus area.
			/// </summary>
			public Vector2 velocity;

			/// <summary>
			/// Initializes a new instance of the <see cref="CameraFollow+FocusArea"/> struct.
			/// </summary>
			/// <param name="targetBounds">Target bounds.</param>
			/// <param name="size">Size.</param>
			public FocusArea(Bounds targetBounds, Vector2 size){
				left = targetBounds.center.x - size.x/2;
				right = targetBounds.center.x + size.x/2;
				bottom = targetBounds.min.y;
				top = targetBounds.min.y + size.y;

				velocity = Vector2.zero;

				center = new Vector2((left+right)/2,(top+bottom)/2);
			}

			/// <summary>
			/// Update the focus area using specified targetBounds.
			/// </summary>
			/// <param name="targetBounds">Target bounds.</param>
			public void Update(Bounds targetBounds){
				float shiftX = 0f;
				if(targetBounds.min.x < left){
					shiftX = targetBounds.min.x - left;
				}else if(targetBounds.max.x > right){
					shiftX = targetBounds.max.x - right;
				}
				left += shiftX;
				right += shiftX;

				float shiftY = 0f;
				if(targetBounds.min.y < bottom){
					shiftY = targetBounds.min.y - bottom;
				}else if(targetBounds.max.y > top){
					shiftY = targetBounds.max.y - top;
				}
				bottom += shiftY;
				top += shiftY;

				velocity = new Vector2(shiftX, shiftY);
				center = new Vector2((left+right)/2,(top+bottom)/2);
			}
		}
	#endregion

	}
}
