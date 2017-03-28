using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StateAgent : MonoBehaviour {

	public enum TaskType
	{
		CPR = 0,
		PrecordialThump = 1,
		Pulse = 2,
		Start = 3,
		Success = 4,
		EndLose = 5,
		EndWin = 6,
		Invalid = 7,
	}

	public struct Task
	{
		public TaskType task { get; private set; }
		public KeyCode button { get; private set; }
		public AudioClip[] clips  { get; private set; }
		public Texture2D image { get; private set; }
		public float duration { get; private set; }

		public Task( TaskType newTask, KeyCode newButton, AudioClip[] newClips, Texture2D newImage, float newDuration )
		{
			task = newTask;
			button = newButton;
			clips = newClips;
			image = newImage;
			duration = newDuration;
		}

		public void Activate()
		{
			if( clips.Length > 0 )
				PlayClip( clips[ Random.Range( 0, clips.Length ) ] );

			SetImage( image );
		}
	}

	public bool playSounds = true;

	public GameObject imageQuadPrefab;

	public GameObject clockFacePrefab;
	public GameObject clockHandPrefab;

	public GUIStyle textStyle;
	
	public AudioClip[] cprClips;
	public AudioClip[] precordialThumpClips;
	public AudioClip[] pulseClips;
	public AudioClip[] startClips;
	public AudioClip[] successClips;
	public AudioClip[] endLoseClips;
	public AudioClip[] endWinClips;

	public Texture2D cprImage;
	public Texture2D precordialThumpImage;
	public Texture2D pulseImage;
	public Texture2D startImage;
	public Texture2D successImage;
	public Texture2D endLoseImage;
	public Texture2D endWinImage;

	private GameObject imageQuad;

	private GameObject clockFace;
	private GameObject clockHand;

	private Quaternion zeroRotation = Quaternion.AngleAxis( 90f, Vector3.forward );

	private Rect textRect;

	private Task startTask;
	private Task successTask;
	private Task endLoseTask;
	private Task endWinTask;
	private List<Task> possibleTasks;
	private Task currentTask;

	private float failTime = 6f;
	private float beginTime;

	private float currentFailTimeReduce;
	private float failTimeReduceRate = 0.25f;
	private float minimumFailTime = 1.5f;
	private float resetTime = 25f;

	private int score;
	public int winScore = 20;
	private int highScore;
	private string highScoreString = "HighScore";

    private string winScoreString = "";

	private TaskType randomTaskType = TaskType.Invalid;

	private static StateAgent mInstance = null;
	public static StateAgent instance
	{
		get
		{
			return mInstance;
		}
	}
	
	void Awake()
	{
		if( mInstance != null )
		{
			Debug.LogError( string.Format( "Only one instance of StateAgent allowed! Destroying:" + gameObject.name + ", Other:" + mInstance.gameObject.name ) );
			Destroy( gameObject );
			return;
		}
		
		mInstance = this;

		startTask = new Task( TaskType.Start, KeyCode.T, startClips, startImage, Mathf.Infinity );
		successTask = new Task( TaskType.Success, KeyCode.None, successClips, successImage, 1f );
		endLoseTask = new Task( TaskType.EndLose, KeyCode.T, endLoseClips, endLoseImage, Mathf.Infinity );
		endWinTask = new Task( TaskType.EndWin, KeyCode.T, endWinClips, endWinImage, Mathf.Infinity );

		possibleTasks = new List<Task>();

		possibleTasks.Add( new Task( TaskType.CPR, KeyCode.C, cprClips, cprImage, failTime ) );
		possibleTasks.Add( new Task( TaskType.PrecordialThump, KeyCode.T, precordialThumpClips, precordialThumpImage, failTime ) );
		possibleTasks.Add( new Task( TaskType.Pulse, KeyCode.P, pulseClips, pulseImage, failTime ) );
	}

	void Start()
	{
		if( imageQuadPrefab )
		{
			imageQuad = Instantiate( imageQuadPrefab ) as GameObject;
			imageQuad.transform.position = new Vector3( Screen.width * 0.5f, Screen.height * 0.5f, 0f );
			imageQuad.transform.localScale = new Vector3( Screen.width, Screen.height, 0f );
		}

		if( clockFacePrefab )
		{
			clockFace = Instantiate( clockFacePrefab ) as GameObject;
			clockFace.transform.position = new Vector3( Screen.width * 0.875f, Screen.height * 0.8f, 0f );
			clockFace.transform.localScale = new Vector3( Screen.height * 0.4f, Screen.height * 0.4f, 0f );
			clockFace.GetComponent<Renderer>().enabled = false;
		}

		if( clockHandPrefab ) 
		{
			clockHand = Instantiate( clockHandPrefab ) as GameObject;
			clockHand.transform.position = new Vector3( Screen.width * 0.875f, Screen.height * 0.8f, 0f );
			clockHand.transform.localScale = new Vector3( Screen.height * 0.1f, Screen.height * 0.1f, 0f );
			clockHand.GetComponent<Renderer>().enabled = false;
		}

		textRect = new Rect( 0f, 0f, Screen.width, Screen.height );

		if( PlayerPrefs.HasKey( highScoreString ) )
			highScore = PlayerPrefs.GetInt( highScoreString );
		else
			highScore = 0;

		SetNextTask( TaskType.Start );
	}

	void Update()
	{
		float duration = Mathf.Infinity;

		if( currentTask.duration != Mathf.Infinity )
			duration = currentTask.duration - currentFailTimeReduce;

		if( currentTask.task == TaskType.Success )
			duration = currentTask.duration;

		float currentTime = ( Time.time - beginTime );

		if( currentTime > duration ) 
		{
			if( currentTask.task == TaskType.Success )
				SetRandomTask();
			else
				SetNextTask( TaskType.EndLose );
		}

        if( Input.GetKeyDown( KeyCode.UpArrow ) )
        {
            winScore++;

            StopCoroutine( "DoDisplayWinScore" );
            StartCoroutine( "DoDisplayWinScore" );
        }

        if( Input.GetKeyDown( KeyCode.DownArrow ) )
        {
            winScore--;

            StopCoroutine( "DoDisplayWinScore" );
            StartCoroutine( "DoDisplayWinScore" );
        }

		if( currentTime > 0.3f && Input.GetKeyDown( currentTask.button ) && !Input.GetKey( KeyCode.Space ) )
		{
			if( currentTask.task == TaskType.Start || currentTask.task == TaskType.EndLose || currentTask.task == TaskType.EndWin )
			{
				currentFailTimeReduce = 0f;
				score = 0;
				SetRandomTask();
			}
			else
			{
				score++;

                if( score >= winScore )
                {
                    SetNextTask( TaskType.EndWin );
                }
                else
                {
                    SetNextTask( TaskType.Success );
                }
			}
		}

		if( clockHand && duration != Mathf.Infinity )
		{
			clockHand.transform.rotation *= Quaternion.AngleAxis( -360f / duration * Time.deltaTime, Vector3.forward );

			if( currentTime > duration * 0.5f && clockHand.transform.eulerAngles.z < 90f )
				clockHand.transform.rotation = zeroRotation;
		}

		if( Input.GetKeyDown( KeyCode.R ) || ( ( currentTask.task == TaskType.EndWin || currentTask.task == TaskType.EndLose ) && ( currentTime > resetTime ) ) )
		{
			SetNextTask( TaskType.Start );
		}

		if( Input.GetKeyDown( KeyCode.Backspace ) )
		{
			highScore = 0;
			PlayerPrefs.DeleteAll();
		}

		if( Input.GetKeyDown( KeyCode.Escape ) )
		{
			Application.Quit();
		}
	}

	void OnGUI()
	{
		string text = "" + currentTask.task;

		if( imageQuad != null && imageQuad.GetComponent<Renderer>().enabled )
			text = "";

		if( currentTask.task == TaskType.Start )
		{
			text = "Veterinarian's Hospital:\nRuff Day\n\n\n\n\nThump to play!";
			GUI.color = new Color( 0.25f, 0.25f, 0.25f, 1f );
		}

		if( currentTask.task == TaskType.EndLose )
		{
			text = "High Score: " + highScore + "\n\nScore: " + score + "\n\n\n\n\nThump to play!";
			GUI.color = Color.white;
		}

		if( currentTask.task == TaskType.EndWin )
		{
			text = "Best Veterinarian Ever\n\nYou Win!\n\n\n\n\nThump to play!";
			GUI.color = new Color( 0.25f, 0.25f, 0.25f, 1f );
		}

		GUI.Label( textRect, text, textStyle );

        if( !string.IsNullOrEmpty( winScoreString ) )
        {
            GUI.Label( new Rect( 5f, 5f, 50f, 50f ), winScoreString );
        }
	}

	private void SetNextTask( TaskType nextTaskType )
	{
		switch( nextTaskType )
		{
			case TaskType.CPR: case TaskType.PrecordialThump: case TaskType.Pulse:
			{
				currentTask = possibleTasks[ (int)nextTaskType ];
				
				if( clockFace )
					clockFace.GetComponent<Renderer>().enabled = true;

				if( clockHand )
				{
					clockHand.transform.rotation = zeroRotation;
					clockHand.GetComponent<Renderer>().enabled = true;
				}
			} break;

			case TaskType.Start:
			{	
				currentTask = startTask;
				
				if( clockFace )
					clockFace.GetComponent<Renderer>().enabled = false;

				if( clockHand )
					clockHand.GetComponent<Renderer>().enabled = false;
			} break;

			case TaskType.Success:
			{
				currentTask = successTask; 
				currentFailTimeReduce += failTimeReduceRate;

				if( failTime - currentFailTimeReduce < minimumFailTime )
					currentFailTimeReduce = failTime - minimumFailTime; 

				if( clockFace )
					clockFace.GetComponent<Renderer>().enabled = false;

				if( clockHand )
					clockHand.GetComponent<Renderer>().enabled = false;
			} break;

			case TaskType.EndLose:
			{
				currentTask = endLoseTask; 
				
				if( score > highScore )
				{
					highScore = score;
					PlayerPrefs.SetInt( highScoreString, highScore );
				}

				if( clockFace )
					clockFace.GetComponent<Renderer>().enabled = false;

				if( clockHand )
					clockHand.GetComponent<Renderer>().enabled = false;
			} break;

		case TaskType.EndWin:
		{
			currentTask = endWinTask; 
			
			if( score > highScore )
			{
				highScore = score;
				PlayerPrefs.SetInt( highScoreString, highScore );
			}
			
			if( clockFace )
				clockFace.GetComponent<Renderer>().enabled = false;
			
			if( clockHand )
				clockHand.GetComponent<Renderer>().enabled = false;
		} break;

			default: return;
		}
	
		currentTask.Activate();
		beginTime = Time.time;
	}

	private void SetRandomTask()
	{
		int randomIndex = Random.Range( 0, possibleTasks.Count );

		if( randomTaskType != TaskType.Invalid && randomIndex == (int)randomTaskType )
			randomIndex = ( randomIndex + 1 )%possibleTasks.Count;

		randomTaskType = possibleTasks[ randomIndex ].task;

		if( randomTaskType == TaskType.Pulse && Input.GetKey( KeyCode.P ) )
		{
			randomIndex = ( randomIndex + 1 )%possibleTasks.Count;
			randomTaskType = possibleTasks[ randomIndex ].task;
		}

		SetNextTask( randomTaskType );
	}

	public static void PlayClip( AudioClip clip )
	{
		if( instance )
			instance.internalPlayClip( clip );
	}

	private void internalPlayClip( AudioClip clip )
	{
		if( clip == null || !playSounds )
			return;

		AudioSource audioSource = CameraAgent.MainCameraObject.GetComponent<AudioSource>();

		if( audioSource == null )
			audioSource = CameraAgent.MainCameraObject.AddComponent<AudioSource>();
		else
			audioSource.Stop();

		audioSource.clip = clip;
		audioSource.loop = false;

		if( currentTask.task == TaskType.EndLose )
		{
			audioSource.volume = 0.5f;
		}
		else
		{
			audioSource.volume = 1f;
		}

		if( currentTask.task == TaskType.Start || currentTask.task == TaskType.Success || currentTask.task == TaskType.EndLose || currentTask.task == TaskType.EndWin )
		{
			audioSource.pitch = 1f;
		}
		else
		{
			if( currentFailTimeReduce > 1f )
				audioSource.pitch = Mathf.Lerp( 1f, 1.3f, ( currentFailTimeReduce - 1f ) / ( failTime - minimumFailTime - 1f ) );
		}

		audioSource.Play();		
	}

	public static void SetImage( Texture2D image )
	{
		if( instance )
			instance.internalSetImage( image );
	}

	private void internalSetImage( Texture2D image )
	{
		imageQuad.GetComponent<Renderer>().enabled = ( image != null );

		if( imageQuad )
			imageQuad.GetComponent<Renderer>().material.mainTexture = image;
	}

    private IEnumerator DoDisplayWinScore()
    {
        winScoreString = "" + winScore;

        yield return new WaitForSeconds( 1f );

        winScoreString = "";
    }
}
