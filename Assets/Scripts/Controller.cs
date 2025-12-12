using System.Collections;
using System.Collections.Generic;
using PuppetFace;
using UnityEngine;

public class Controller : MonoBehaviour
{
    [SerializeField]private Animator animator;
    [SerializeField]private SkinnedMeshRenderer renderer;
    [SerializeField]private LipSync lipSync;

    int smileindx=0;
    int sadinx=0;

    void Start()
    {
         smileindx = renderer.sharedMesh.GetBlendShapeIndex("Smile");
         sadinx = renderer.sharedMesh.GetBlendShapeIndex("Sad");
        Debug.Log(renderer.sharedMesh.blendShapeCount);
    }

    public void PlayDialogue()
    {
        animator.SetTrigger("play");
        animator.SetInteger("action",3);
        lipSync.Play(0);
        StartCoroutine(IdleState());
    }

    public void Smile()
    {
        lipSync.Stop();
        animator.SetTrigger("play");
        animator.SetInteger("action",1);
        for(int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
        {
            renderer.SetBlendShapeWeight(i,0);
        }
        renderer.SetBlendShapeWeight(smileindx,100);

    }
    public void Sad()
    {
        lipSync.Stop();
        animator.SetTrigger("play");
        animator.SetInteger("action",4);
        for(int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
        {
            renderer.SetBlendShapeWeight(i,0);
        }
        renderer.SetBlendShapeWeight(sadinx,100);
    }

    IEnumerator IdleState()
    {
        float duration = lipSync.AudioClips[0].length;
        yield return new WaitForSeconds(duration+0.5f);
        Debug.LogError("du "+duration);
         animator.SetTrigger("play");
        animator.SetInteger("action",2);
    }
}
