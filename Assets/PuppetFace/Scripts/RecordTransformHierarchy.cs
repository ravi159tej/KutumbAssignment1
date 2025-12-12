#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using UnityEditor;
public class RecordTransformHierarchy : MonoBehaviour
{
	public AnimationClip clip;

	private GameObjectRecorder m_Recorder;
	bool isRecording = false;
	private AudioSource audioSource;

	//temporary audio vector we write to every second while recording is enabled..
	List<float> tempRecording = new List<float>();

	//list of recorded clips...
	List<float[]> recordedClips = new List<float[]>();


	void Start()
	{
		audioSource = GetComponent<AudioSource>();
		//set up recording to last a max of 1 seconds and loop over and over
		//audioSource.clip = Microphone.Start(null, true, 1, 44100);
		//audioSource.Play();
		//resize our temporary vector every second
		//Invoke("ResizeRecording", 1);
		
		// Create recorder and record the script GameObject.
		m_Recorder = new GameObjectRecorder(gameObject);

		// Bind all the Transforms on the GameObject and all its children.
		m_Recorder.BindComponentsOfType<Transform>(gameObject, true);
		m_Recorder.BindComponentsOfType<SkinnedMeshRenderer>(gameObject, true);
	}

	void LateUpdate()
	{
		
	}
	/*
	void OnDisable()
	{
		if (clip == null)
			return;

		if (m_Recorder.isRecording)
		{
			// Save the recorded session to the clip.
			m_Recorder.SaveToClip(clip);
			SavWav.Save("registerAudio", audioSource.clip);

		}
	}
	*/


	

	void ResizeRecording()
	{
		if (isRecording)
		{
			//add the next second of recorded audio to temp vector
			int length = 441000;
			float[] clipData = new float[length];
			audioSource.clip.GetData(clipData, 0);
			tempRecording.AddRange(clipData);
			Invoke("ResizeRecording", 10);
		}
	}

	void Update()
	{
		//space key triggers recording to start...
		if (Input.GetKeyDown("space"))
		{
			isRecording = !isRecording;
			Debug.Log(isRecording == true ? "Is Recording" : "Off");

			if (isRecording == false)
			{
				//stop recording, get length, create a new array of samples
				int length = Microphone.GetPosition(null);

				Microphone.End(null);
				float[] clipData = new float[length];
				audioSource.clip.GetData(clipData, 0);

				//create a larger vector that will have enough space to hold our temporary
				//recording, and the last section of the current recording
				float[] fullClip = new float[clipData.Length + tempRecording.Count];
				for (int i = 0; i < fullClip.Length; i++)
				{
					//write data all recorded data to fullCLip vector
					if (i < tempRecording.Count)
						fullClip[i] = tempRecording[i];
					else
						fullClip[i] = clipData[i - tempRecording.Count];
				}

				recordedClips.Add(fullClip);
				audioSource.clip = AudioClip.Create("recorded samples", fullClip.Length, 1, 44100, false);
				audioSource.clip.SetData(fullClip, 0);
				audioSource.loop = true;
				if (m_Recorder.isRecording)
				{
					// Save the recorded session to the clip.
					m_Recorder.SaveToClip(clip);
					SavWav.Save("registerAudio", audioSource.clip);

				}

			}
			else
			{
				if (clip == null)
					return;

				// Take a snapshot and record all the bindings values for this frame.

				//stop audio playback and start new recording...
				//audioSource.Stop();
				//tempRecording.Clear();
				//Microphone.End(null);
				audioSource.clip = Microphone.Start(null, true, 10, 44100);
				Invoke("ResizeRecording", 10);
			}




		}
		if (isRecording)
		{
			m_Recorder.TakeSnapshot(Time.deltaTime);


		}
	}

	//chooose which clip to play based on number key..
	void SwitchClips(int index)
	{
		if (index < recordedClips.Count)
		{
			audioSource.Stop();
			int length = recordedClips[index].Length;
			audioSource.clip = AudioClip.Create("recorded samples", length, 1, 44100, false);
			audioSource.clip.SetData(recordedClips[index], 0);
			audioSource.loop = true;
			audioSource.Play();
		}
	}



}
#endif