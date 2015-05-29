using UnityEngine;
using System.Collections;

public class CameraAgent : MonoBehaviour {
	
	public static GameObject MainCameraObject
	{
		get
		{
			return Camera.main.gameObject;
		}
	}
	
	private static CameraAgent m_Instance;
	public static CameraAgent instance
	{
		get
		{
			return m_Instance;
		}
	}
	
	void Awake()
	{
		if( m_Instance != null )
		{
			Debug.LogError( "Only one instance of CameraAgent allowed. Destroying " + gameObject + " and leaving " + m_Instance.gameObject );
			Destroy( gameObject );
			return;
		}
		
		m_Instance = this;
		
		MainCameraObject.GetComponent<Camera>().orthographic = true;
		MainCameraObject.GetComponent<Camera>().orthographicSize = Screen.height * 0.5f;
		MainCameraObject.transform.position = new Vector3( Screen.width * 0.5f, Screen.height * 0.5f, -10f );
	}
}
