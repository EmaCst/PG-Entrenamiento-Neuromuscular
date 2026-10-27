using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// One guide per Pose scene; announcements share the voice channel, never queue.
public sealed class NatacionVoiceGuide : MonoBehaviour
{
    private static bool setupSpoken;
    private INatacionExercise exercise;
    private bool prepared, preparing, lossAnnounced;
    private float lostFor, recoveredFor, nextLossNotice;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => setupSpoken=false;

    public static NatacionVoiceGuide For(MonoBehaviour controller)
    {
        var guide=controller.GetComponent<NatacionVoiceGuide>();
        if(guide==null) guide=controller.gameObject.AddComponent<NatacionVoiceGuide>();
        guide.exercise=controller as INatacionExercise;
        return guide;
    }
    public static void BeginTraining() => setupSpoken=false;
    public IEnumerator Prepare(MonoBehaviour owner, bool introduce = true)
    {
        if(prepared || preparing || exercise==null) yield break;
        preparing=true;
        try
        {
            var voice=PraxenVoice.Instance;
            voice.WarmUp("turn_left");voice.WarmUp("turn_right");voice.WarmUp("return_front");
            voice.WarmUp("tracking_lost");voice.WarmUp("tracking_ready");
            if(!setupSpoken)
            {
                yield return voice.Say(owner,"selfie_setup");
                setupSpoken=true;
            }
            string scene=SceneManager.GetActiveScene().name;
            if(introduce)
                yield return voice.Say(owner,scene=="TelefonoManos"?"pose_hands":scene=="TelefonoPies"?"pose_feet":"pose_turns");
            if(!exercise.IsReady) yield return voice.Say(owner,"pose_calibration");
            prepared=true;
        }
        finally { preparing=false; }
    }
    private void Update()
    {
        bool playing=ExerciseSetRunner.ActiveRunner!=null?ExerciseSetRunner.ActiveRunner.IsExercising:
            CombinedExerciseSession.Instance!=null && CombinedExerciseSession.Instance.IsExercising;
        if(!prepared || exercise==null || !playing || TrainingTransitionScreen.IsVisible)
        { lostFor=recoveredFor=0;lossAnnounced=false;return; }
        var voice=PraxenVoice.Instance;
        if(!exercise.TrackingReady)
        {
            lostFor+=Time.unscaledDeltaTime;recoveredFor=0;
            if(!lossAnnounced && lostFor>=.6f && Time.unscaledTime>=nextLossNotice && voice.TryPlay("tracking_lost"))
            {lossAnnounced=true;nextLossNotice=Time.unscaledTime+10;}
        }
        else
        {
            lostFor=0;
            if(!lossAnnounced) return;
            recoveredFor+=Time.unscaledDeltaTime;
            if(recoveredFor>=.5f && voice.TryPlay("tracking_ready"))
            {lossAnnounced=false;recoveredFor=0;}
        }
    }
    private void OnApplicationPause(bool paused)
    {
        if(paused) {lostFor=recoveredFor=0;lossAnnounced=false;PraxenVoice.Instance.Stop();}
    }
}
