using UnityEngine;
using System.Collections;
using UnityEditor;

namespace MonteTools.Platforming2D.Editor{

[CustomEditor(typeof(Player))]
public class PlayerEditor : UnityEditor.Editor {

	//Movement
	SerializedProperty movementEnabled;
	SerializedProperty moveSpeed;
	SerializedProperty dashSpeed;

	//Gravity
	SerializedProperty gravityScale;

	//Jumping
	SerializedProperty jumpEnabled;
	SerializedProperty maxJumpHeight;
	SerializedProperty minJumpHeight;
	SerializedProperty timeToJumpApex;

	//Wall Jumping
	SerializedProperty wallJumpEnabled;
	SerializedProperty wallSlideSpeedMax;
	SerializedProperty wallJumpClimb;
	SerializedProperty wallJumpOff;
	SerializedProperty wallLeap;
	SerializedProperty wallStickTime;


	void OnEnable(){
		movementEnabled = serializedObject.FindProperty("movementEnabled");
		moveSpeed = serializedObject.FindProperty("moveSpeed");
		dashSpeed = serializedObject.FindProperty("dashSpeed");

		gravityScale = serializedObject.FindProperty("gravityScale");

		jumpEnabled = serializedObject.FindProperty("jumpEnabled");
		maxJumpHeight = serializedObject.FindProperty("maxJumpHeight");
		minJumpHeight = serializedObject.FindProperty("minJumpHeight");
		timeToJumpApex = serializedObject.FindProperty("timeToJumpApex");
	}

	override public void OnInspectorGUI(){
		serializedObject.Update();


		EditorGUILayout.PropertyField(movementEnabled);
		if(movementEnabled.boolValue){
			EditorGUILayout.PropertyField(moveSpeed);
			EditorGUILayout.PropertyField(dashSpeed);
		}

		EditorGUILayout.PropertyField(gravityScale);

		EditorGUILayout.PropertyField(jumpEnabled);
		if(jumpEnabled.boolValue){
			EditorGUILayout.PropertyField(maxJumpHeight);
			EditorGUILayout.PropertyField(minJumpHeight);
			EditorGUILayout.PropertyField(timeToJumpApex);
		}

		serializedObject.ApplyModifiedProperties();
	}
}

}