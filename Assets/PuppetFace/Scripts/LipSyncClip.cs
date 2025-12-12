using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;


public class LipSyncClip : PlayableAsset
{
    public int LipSyncIndex;
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<LipSyncBehaviour>.Create(graph);
        LipSyncBehaviour lipSyncBehaviour = playable.GetBehaviour();
        lipSyncBehaviour.LipSyncIndex = LipSyncIndex;

        return playable;
    }
}
