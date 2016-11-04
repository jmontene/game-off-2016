using UnityEngine;
using System.Collections;

namespace MonteTools.Platforming2D{
	/// <summary>
	/// Class that feeds input to the Player
	/// </summary>
	[RequireComponent (typeof (Player))]
	public class PlayerInput : MonoBehaviour {

	#region Public Variables

		public InputConfiguration config;

	#endregion

	#region Private/Protected Variables

		private Player player;

	#endregion

	#region Inner Classes

		[System.Serializable]
		public class InputConfiguration{
			public AxisRecord horizontalMovement;
			public AxisRecord verticalMovement;
			public ButtonRecord jump;
			public ButtonRecord dash;
		}

		[System.Serializable]
		public class AxisRecord{
			public KeyCode positiveKey;
			public KeyCode negativeKey;
			public bool enabled;

			public float getValue(){
				if(!enabled) return 0f;
				if(Input.GetKey(positiveKey)){
					return 1f;
				}else if(Input.GetKey(negativeKey)){
					return -1f;
				}else{
					return 0f;
				}
			}
		}

		[System.Serializable]
		public class ButtonRecord{
			public KeyCode keyCode;
			public bool enabled;

			public bool getButtonDown(){
				return enabled && Input.GetKeyDown(keyCode);
			}

			public bool getButtonUp(){
				return enabled && Input.GetKeyUp(keyCode);
			}

			public bool getButton(){
				return enabled && Input.GetKey(keyCode);
			}
		}

	#endregion

	#region Unity Methods

		/// <summary>
		/// Awake this instance.
		/// </summary>
		void Awake() {
			player = GetComponent<Player>();
		}
		
		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update () {
			Vector2 directionalInput = new Vector2(config.horizontalMovement.getValue(), config.verticalMovement.getValue());
			player.SetDirectionalInput(directionalInput);

			if(config.jump.getButtonDown()){
				player.OnJumpInputDown();
			}

			if(config.jump.getButtonUp()){
				player.OnJumpInputUp();
			}

			if(config.dash.getButtonDown()){
				player.OnDashInputDown();
			}
		}

	#endregion
	}
}
