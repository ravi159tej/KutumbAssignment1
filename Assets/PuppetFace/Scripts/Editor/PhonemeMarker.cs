using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace PuppetFace
{
	public class PhonemeMarker
	{
		public Rect rect;
		public string title;
		public bool isDragged;

		public GUIStyle style;
		public string Label;
		public float Start;
		public float End;
		public float Strength;
        public float Smoothing;

        public int BlendShapeID;

		float RangeVal = 0;
		public int Index;
		Rect StartRect;

		public PhonemeMarker(Vector2 position, float width, float height, GUIStyle nodeStyle, string label, float start, float end, float strength, int index, int blendShapeID = -1)
		{
			rect = new Rect(position.x, position.y, width, height);
			style = nodeStyle;
			Label = label;
			Start = start;
			End = end;
			RangeVal = End - Start;
			Index = index;
			Strength = strength;
			BlendShapeID = blendShapeID;
		}

		public void Drag(Vector2 delta)
		{
			rect.position += new Vector2(delta.x, 0);
			float offset = (delta.x / (float)PuppetFaceEditor.WaveFormWidth) * PuppetFaceEditor.AudioClipLoaded.length;
            Start += offset;
			End += offset;
		}

		public void Draw()
		{

			if (PuppetFaceEditor.CurrentSelectedPhonemeMarker == Index)
			{
				float shapeDuration = (End - Start + Smoothing);
				float end = (End - Start) * (1.35f - 0.35f * Mathf.Clamp01(shapeDuration)) + Start + Smoothing/2;
				float start = (Start - End) * (1.35f - 0.35f * Mathf.Clamp01(shapeDuration)) + End - Smoothing / 2;

				float range = ((end - start) / PuppetFaceEditor.AudioClipLoaded.length) * (float)PuppetFaceEditor.WaveFormWidth;
				Rect newRect2 = new Rect(rect.center + new Vector2(-range / 2f, -115), new Vector2(range, 100f));
				EditorGUI.DrawRect(newRect2, new Color(238f / 255f, 45f / 255f, 67f / 255f, 0.25f));

				GUIStyle textFieldStyle = new GUIStyle();
				textFieldStyle.normal.textColor = Color.white;
				
				Rect newRect3 = new Rect(rect.center - new Vector2(-10, -30), new Vector2(30f, 15f));
				EditorGUI.BeginChangeCheck();
				RangeVal = EditorGUI.FloatField(newRect3, RangeVal, textFieldStyle);
				if (EditorGUI.EndChangeCheck())
				{
					float rangeBefore = (End - Start);
					Start -= (0.5f * (RangeVal - rangeBefore));
					End += (0.5f * (RangeVal - rangeBefore));
				}
				newRect3.position = new Vector2(newRect3.position.x, newRect3.position.y + 15);

				EditorGUI.BeginChangeCheck();
				Strength = EditorGUI.FloatField(newRect3, Strength, textFieldStyle);

				Rect newRect4 = new Rect(rect.center - new Vector2(50, -30), new Vector2(5f, 15f));
				GUI.Label(newRect4, "Range:", textFieldStyle);
				newRect4.position = new Vector2(newRect4.position.x, newRect4.position.y + 15);
				GUI.Label(newRect4, "Strength:", textFieldStyle);

				newRect4.width = 130;
				newRect4.position = new Vector2(newRect4.position.x, newRect4.position.y - 15);
				EditorGUI.DrawRect(newRect4, new Color(1, 1f, 1f, .25f));
				newRect4.position = new Vector2(newRect4.position.x, newRect4.position.y + 15);
				EditorGUI.DrawRect(newRect4, new Color(1, 1f, 1f, .1f));
				GUI.color = new Color(2, .5f, .5f,1);

			}
			else
				GUI.color = Color.white;
			GUI.Box(rect, title, style);
			GUIStyle newStyle = new GUIStyle();
			newStyle.normal.textColor = Color.white;
			Rect newRect = rect;
			newRect.center += new Vector2(0, 7);
			newStyle.alignment = TextAnchor.MiddleCenter;
			if(BlendShapeID==-1)
				GUI.Label(newRect, PuppetFaceEditor.GetPrestonBlairNamesFromLetter(Label), newStyle);
			else
				GUI.Label(newRect, BlendShapeID.ToString(), newStyle);

			GUI.color = Color.white;
		}
        public bool IsMarkerUnderMouse(Vector2 mousePos)
        {
            Rect reducedRect = rect;
            reducedRect.width *= .5f;
            reducedRect.position = new Vector2(reducedRect.position.x + (reducedRect.width * .5f), reducedRect.position.y);
            if (reducedRect.Contains(mousePos))
            {                
                return true;
            }
            else
                return false;
        }
		public bool ProcessEvents(Event e)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0)
					{
                        if(IsMarkerUnderMouse(e.mousePosition))
						{
							isDragged = true;
							GUI.changed = true;
							PuppetFaceEditor.CurrentSelectedPhonemeMarker = Index;
							return true;
						}
					}

					break;

				case EventType.MouseUp:
					isDragged = false;
					break;

				case EventType.MouseDrag:
					if (e.button == 0 && isDragged)
					{
						Drag(e.delta);
						e.Use();
						return true;
					}
					break;
			}

			return false;
		}
	}
}