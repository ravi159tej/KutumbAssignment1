using System.Collections.Generic;
using System.Xml.Serialization;

public class mouthCue
{
	[XmlAttribute("start")]
	public float start;
	[XmlAttribute("end")]
	public float end;
	[XmlAttribute("strength")]
	public float strength;
	[XmlText]
	public string shapeName = "";
	[XmlAttribute("blendShapeID")]
	public int blendShapeID = -1;
}

[XmlRoot("rhubarbResult")]
public class LipSyncType
{
	[XmlArrayItem("mouthCue")]
	public List<mouthCue> mouthCues = new List<mouthCue>();
}