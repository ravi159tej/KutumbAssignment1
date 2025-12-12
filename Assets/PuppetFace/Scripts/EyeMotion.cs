using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PuppetFace
{
	[AddComponentMenu("Puppet Face/EyeMotion")]
	public class EyeMotion : MonoBehaviour
	{
		public Transform EyeL;
		public Transform EyeR;

		public Quaternion EyeLDefault = Quaternion.identity;
		public Quaternion EyeRDefault = Quaternion.identity;

		public Vector3 LookDirection= Vector3.up;

		public Vector3 UpDirection = Vector3.up;

		public Transform Head;
		public SkinnedMeshRenderer FaceSkin;
		public bool EyeLidBones = false;
		public bool EyeLidBonesUpDown = false;
		public Transform EyeLidTopL;
		public Transform EyeLidTopR;
		public Transform EyeLidBottomL;
		public Transform EyeLidBottomR;

		public Quaternion EyeLidTopLDefault = Quaternion.identity;
		public Quaternion EyeLidTopLClosed = Quaternion.identity;

		public Quaternion EyeLidTopRDefault = Quaternion.identity;
		public Quaternion EyeLidTopRClosed = Quaternion.identity;

		public Quaternion EyeLidBottomLDefault = Quaternion.identity;
		public Quaternion EyeLidBottomLClosed = Quaternion.identity;

		public Quaternion EyeLidBottomRDefault = Quaternion.identity;
		public Quaternion EyeLidBottomRClosed = Quaternion.identity;

		public Quaternion EyeLidTopLUp = Quaternion.identity;
		public Quaternion EyeLidTopLDown = Quaternion.identity;

		public Quaternion EyeLidTopRUp = Quaternion.identity;
		public Quaternion EyeLidTopRDown = Quaternion.identity;

		public Quaternion EyeLidBottomLUp = Quaternion.identity;
		public Quaternion EyeLidBottomLDown = Quaternion.identity;

		public Quaternion EyeLidBottomRUp = Quaternion.identity;
		public Quaternion EyeLidBottomRDown = Quaternion.identity;

		private Quaternion _EyeLidTopL;
		private Quaternion _EyeLidTopR;
		private Quaternion _EyeLidBottomL;
		private Quaternion _EyeLidBottomR;

		//public Transform EyeLidTopR;
		//public Transform EyeLidBottomR;

		public Transform LookAtTarget = null;
		private Transform _hiddenTarget;
		private Vector3 _hiddenTargetVel;

		private Quaternion EyeLOffset;
		[Header("Look Around")]
		public Vector2 LookAroundScale = new Vector2(10f, 5f);

		public float LookUpDownAmount = 1f;
		public float LookUpDownOffset = -.5f;

		private Vector3 _newPos = Vector3.zero;
		[Range(0f, 1f)]
		public float LookRandomAmount = 0.1f;
		public float LookAroundSpeed = 100f;
		public int EyesUpIndex = 0;
		public int EyesDownIndex = 0;

		[Header("Blink")]
		public float BlinkRandomAmount = 0.2f;
		public float BlinkSpeed = 30f;

		public int BlinkIndex = 0;
		private float _currentBlink = 0f;
		private float _targetBlink = 0f;

		public bool LookAround = true;
		public bool Blink = true;
		public bool UpDown = false;

		private Vector3 defaultEyeForward;
		
		// Use this for initialization
		void Start()
		{
			_hiddenTarget = new GameObject().transform;
			_hiddenTarget.parent = LookAtTarget;
			_hiddenTarget.localPosition = Vector3.zero;
			
		}

		// Update is called once per frame
		void LateUpdate()
		{
			if (EyeL != null && EyeR != null && _hiddenTarget != null)
			{
				_EyeLidTopL = EyeLidTopLDefault;
				_EyeLidTopR = EyeLidTopRDefault;
				_EyeLidBottomL = EyeLidBottomLDefault;
				_EyeLidBottomR = EyeLidBottomRDefault;

				
				if (LookAround)
				{
					if (Random.value > 1f - (LookRandomAmount / 10f))
					{
						float distScaler = Vector3.Distance(EyeL.position, LookAtTarget.position) / 100f;
						_newPos = new Vector3((LookAroundScale.x * distScaler) * Random.Range(-1f, 1f), (LookAroundScale.y * distScaler) * Random.Range(-1f, 1f), 0f);

					}

					_hiddenTarget.localPosition = Vector3.SmoothDamp(_hiddenTarget.localPosition, _newPos, ref _hiddenTargetVel, 1f / LookAroundSpeed);
					
					Quaternion lookDirL = Quaternion.LookRotation(_hiddenTarget.position - EyeL.position, Vector3.up);
					EyeL.rotation = lookDirL * EyeLDefault;
					Quaternion lookDirR = Quaternion.LookRotation(_hiddenTarget.position - EyeR.position, Vector3.up);
					EyeR.rotation = lookDirR * EyeRDefault;
					

				}
				if (FaceSkin != null)
				{
					if (UpDown)
					{
						float upAmount = LookUpDownAmount*(Vector3.Dot((_hiddenTarget.position - EyeL.position).normalized,( Head.forward*LookDirection.z + Head.up * LookDirection.y + Head.right * LookDirection.x).normalized)) ;
						upAmount += LookUpDownOffset;
						if (EyesUpIndex < FaceSkin.sharedMesh.blendShapeCount)
							FaceSkin.SetBlendShapeWeight(EyesUpIndex, 100f * Mathf.Clamp01(upAmount));
						if (EyesDownIndex < FaceSkin.sharedMesh.blendShapeCount)
							FaceSkin.SetBlendShapeWeight(EyesDownIndex, 100f * Mathf.Clamp01(-upAmount));
						if (EyeLidTopL != null)
						{
							if(upAmount>0)
								_EyeLidTopL = (Quaternion.Lerp(_EyeLidTopL, EyeLidTopLUp, Mathf.Clamp01(upAmount*2f)));
							else
								_EyeLidTopL = (Quaternion.Lerp(_EyeLidTopL, EyeLidTopLDown, Mathf.Clamp01(-upAmount * 2f)));

						}
						if (EyeLidTopR != null)
						{
							if (upAmount > 0)
								_EyeLidTopR =(Quaternion.Lerp(_EyeLidTopR, EyeLidTopRUp, Mathf.Clamp01(upAmount * 2f)));
							else
								_EyeLidTopR =(Quaternion.Lerp(_EyeLidTopR, EyeLidTopRDown, Mathf.Clamp01(-upAmount * 2f)));

						}
						if (EyeLidBottomL != null)
						{
							if (upAmount > 0)
								_EyeLidBottomL =(Quaternion.Lerp(_EyeLidBottomL, EyeLidBottomLUp, Mathf.Clamp01(upAmount * 2f)));
							else
								_EyeLidBottomL = (Quaternion.Lerp(_EyeLidBottomL, EyeLidBottomLDown, Mathf.Clamp01(-upAmount * 2f)));

						}
						if (EyeLidBottomR != null)
						{
							if (upAmount > 0)
								_EyeLidBottomR = (Quaternion.Lerp(_EyeLidBottomR, EyeLidBottomRUp, Mathf.Clamp01(upAmount * 2f)));
							else
								_EyeLidBottomR = (Quaternion.Lerp(_EyeLidBottomR, EyeLidBottomRDown, Mathf.Clamp01(-upAmount * 2f)));

						}
					}
				}
			}
			if (FaceSkin != null)
			{
				if (Blink)
				{
					if (Random.value > 1f - (BlinkRandomAmount / 10f))
					{
						_targetBlink = 1;

					}

					_currentBlink = Mathf.Lerp(_currentBlink, _targetBlink, Time.deltaTime * BlinkSpeed);
					if (_targetBlink - _currentBlink < 0.01f)
						_targetBlink = 0f;
					if(BlinkIndex< FaceSkin.sharedMesh.blendShapeCount)
						FaceSkin.SetBlendShapeWeight(BlinkIndex, _currentBlink*100f);

					if (EyeLidTopL != null)					
						_EyeLidTopL = (Quaternion.Lerp(_EyeLidTopL, EyeLidTopLClosed,_currentBlink));
					if (EyeLidTopR != null)
						_EyeLidTopR = (Quaternion.Lerp(_EyeLidTopR, EyeLidTopRClosed, _currentBlink));
					if (EyeLidBottomL != null)
						_EyeLidBottomL = (Quaternion.Lerp(_EyeLidBottomL, EyeLidBottomLClosed, _currentBlink));
					if (EyeLidBottomR != null)
						_EyeLidBottomR = (Quaternion.Lerp(_EyeLidBottomR, EyeLidBottomRClosed, _currentBlink));


				}
				if (EyeLidTopL != null)
					EyeLidTopL.localRotation = (_EyeLidTopL);
				if (EyeLidTopR != null)
					EyeLidTopR.localRotation = (_EyeLidTopR);
				if (EyeLidBottomL != null)
					EyeLidBottomL.localRotation = (_EyeLidBottomL);
				if (EyeLidBottomR != null)
					EyeLidBottomR.localRotation = (_EyeLidBottomR);


			}


		}
	}
}
