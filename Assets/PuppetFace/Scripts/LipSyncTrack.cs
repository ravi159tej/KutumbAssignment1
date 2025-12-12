using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
[TrackBindingType(typeof(PuppetFace.LipSync))]
[TrackClipType(typeof(LipSyncClip))]

public class LipSyncTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<LipSyncBehaviour>.Create(graph, inputCount);
    }

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        #if UNITY_EDITOR
        PuppetFace.LipSync trackBinding = director.GetGenericBinding(this) as PuppetFace.LipSync;
        if (trackBinding == null )
            return;
        if (trackBinding.Skin == null)
            return;

        var serializedObject = new UnityEditor.SerializedObject(trackBinding.Skin);
        var iterator = serializedObject.GetIterator();
        while (iterator.NextVisible(true))
        {
            if (iterator.hasVisibleChildren)
                continue;

            driver.AddFromName<SkinnedMeshRenderer>(trackBinding.Skin.gameObject, iterator.propertyPath);
        }
        foreach(Transform bone in trackBinding.FaceBones)
        {
            serializedObject = new UnityEditor.SerializedObject(bone);
            iterator = serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.hasVisibleChildren)
                    continue;

                driver.AddFromName<Transform>(bone.gameObject, iterator.propertyPath);
            }
        }

        #endif
        base.GatherProperties(director, driver);
    }
}
