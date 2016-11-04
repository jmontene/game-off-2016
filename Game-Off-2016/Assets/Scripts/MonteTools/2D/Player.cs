using UnityEngine;
using System.Collections;

namespace MonteTools.Platforming2D{
	/// <summary>
	/// Player script that takes input and feeds it to the appropiate controllers
	/// </summary>
	[RequireComponent (typeof (Controller2D))]
	public class Player : MonoBehaviour {

	#region Public Variables
		[Header("Movement")]
		/// <summary>
		/// Toggle for movement
		/// </summary>
		public bool movementEnabled = true;
		/// <summary>
		/// The move speed.
		/// </summary>
		public Vector2 moveSpeed = new Vector2(6f,0f);
		/// <summary>
		/// The dash speed.
		/// </summary>
		public float dashSpeed = 10f;

		[Header("Gravity")]
		/// <summary>
		/// The gravity scale.
		/// </summary>
		public float gravityScale = 1f;

		[Header("Jumping")]
		/// <summary>
		/// Toggle for jumps
		/// </summary>
		public bool jumpEnabled = true;
		/// <summary>
		/// The height of the jump.
		/// </summary>
		public float maxJumpHeight = 4f;
		/// <summary>
		/// The minimum height of the jump.
		/// </summary>
		public float minJumpHeight = 1f;
		/// <summary>
		/// The time to jump apex.
		/// </summary>
		public float timeToJumpApex = 0.4f;

		[Header("Wall Jump")]
		/// <summary>
		/// Determines if the wall jump action should be enabled
		/// </summary>
		public bool wallJumpEnabled = true;
		/// <summary>
		/// The max wall slide speed.
		/// </summary>
		public float wallSlideSpeedMax = 3f;
		/// <summary>
		/// The wall jump climb velocity
		/// </summary>
		public Vector2 wallJumpClimb;
		/// <summary>
		/// The wall jump off velocity.
		/// </summary>
		public Vector2 wallJumpOff;
		/// <summary>
		/// The wall leap velocity.
		/// </summary>
		public Vector2 wallLeap;
		/// <summary>
		/// The wall stick time.
		/// </summary>
		public float wallStickTime = 0.25f;
	#endregion

	#region Private Variables
		/// <summary>
		/// The controller for 2D movement.
		/// </summary>
		private Controller2D controller;
		/// <summary>
		/// The directional input given by the player.
		/// </summary>
		private Vector2 directionalInput;
		/// <summary>
		/// The acceleration time when airborne.
		/// </summary>
		float accelerationTimeAirborne = 0.2f;
		/// <summary>
		/// The acceleration time when grounded.
		/// </summary>
		float accelerationTimeGrounded = 0.2f;
		/// <summary>
		/// The gravity unique to this player.
		/// </summary>
		private float gravity;
		/// <summary>
		/// The maximum jump velocity.
		/// </summary>
		private float maxJumpVelocity;
		/// <summary>
		/// The minimum jump velocity.
		/// </summary>
		private float minJumpVelocity;
		/// <summary>
		/// The move velocity.
		/// </summary>
		private Vector3 velocity;
		/// <summary>
		/// The velocity X smoothing.
		/// </summary>
		private float velocityXSmoothing;
		/// <summary>
		/// The velocity Y smoothing.
		/// </summary>
		private float velocityYSmoothing;
		/// <summary>
		/// Determines if we are sliding on a wall
		/// </summary>
		private bool wallSliding;
		/// <summary>
		/// The direction the player is going towards when wall sliding
		/// </summary>
		private int wallDirX;
		/// <summary>
		/// The time to wall unstick.
		/// </summary>
		private float timeToWallUnstick;
	#endregion

	#region Unity Methods

		/// <summary>
		/// Awake this instance.
		/// </summary>
		void Awake(){
			controller = GetComponent<Controller2D>();
			gravity = -(2 * maxJumpHeight)/Mathf.Pow(timeToJumpApex,2) * gravityScale;
			maxJumpVelocity = Mathf.Abs(gravity * timeToJumpApex);
			minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

			#if DEBUG
			print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
			#endif
		}

		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update(){
			CalculateVelocity();
			if(wallJumpEnabled) HandleWallSliding();
			if(movementEnabled) controller.Move(velocity * Time.deltaTime, directionalInput);

			//Reset the velocity y to 0 if there are collisions above or below
			if(controller.collisions.above || controller.collisions.below){
				velocity.y = 0;
			}
		}
	#endregion

	#region Public Methods

	public void SetDirectionalInput(Vector2 input){
		directionalInput = input;
	}

	public void OnJumpInputDown(){
		if(!jumpEnabled) return;
		if(wallSliding){
			if(wallDirX == directionalInput.x){
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}else if(directionalInput.x == 0){
				velocity.x = -wallDirX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}else{
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if(controller.collisions.below){
			velocity.y = maxJumpVelocity;
		}
	}

	public void OnJumpInputUp(){
		if(!jumpEnabled) return;
		if(velocity.y > minJumpVelocity){
			velocity.y = minJumpVelocity;
		}
	}

	public void OnDashInputDown(){
		if(!movementEnabled) return;
		velocity.x = directionalInput.x * dashSpeed;
		velocity.y = directionalInput.y * dashSpeed;
	}

	#endregion

	#region Private Methods

	private void CalculateVelocity(){
		float targetVelocityX = directionalInput.x * moveSpeed.x;
		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
		if(moveSpeed.y != 0){
			float targetVelocityY = directionalInput.y * moveSpeed.y;
			velocity.y = Mathf.SmoothDamp(velocity.y, targetVelocityY, ref velocityYSmoothing, (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
		}else{
			velocity.y += gravity * Time.deltaTime;
		}
	}

	private void HandleWallSliding(){
		wallDirX = (controller.collisions.left) ? -1 : 1;
		wallSliding = ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0);
		if(wallSliding){
			if(velocity.y < -wallSlideSpeedMax){
				velocity.y = -wallSlideSpeedMax;
			}
			if(timeToWallUnstick > 0){
				velocityXSmoothing = 0;
				velocity.x = 0;

				if(directionalInput.x != wallDirX && directionalInput.x != 0){
					timeToWallUnstick -= Time.deltaTime;
				}else{
					timeToWallUnstick = wallStickTime;
				}
			}else{
				timeToWallUnstick = wallStickTime;
			}
		}
	}

	#endregion

	}
}
