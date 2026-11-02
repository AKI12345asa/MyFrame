using MyFrame;
using UnityEngine;

public class AudioEventProcessor:DetectionEventProcessor<PlayAudioEvent>
{
    public AudioEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void ProcessEvent(PlayAudioEvent ev,Vector3 hitPoint, IHitTarget target,Skill_Player skillPlayer)
    {
        if (ev.HitAudioClip != null)
        {
            //音效播放
            AudioSystem.PlayOneShot(ev.HitAudioClip, hitPoint);
        }
    }
}
