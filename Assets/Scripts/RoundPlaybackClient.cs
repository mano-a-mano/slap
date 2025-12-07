using System.Collections;
using UnityEngine;

// Dependencies via channels:
// - RoundSummaryChannel: receives RoundEvent[] after Resolve begins.
// - PhaseChangedChannel: optional, to hide/show UI around phases.
// Uses your existing SoundManager (if present) and CameraShake (if present).
public class RoundPlaybackClient : MonoBehaviour
{
    [Header("Channels (assign)")]
    public RoundSummaryChannel roundSummaryChannel;
    public PhaseChangedChannel phaseChangedChannel;

    //[Header("Optional presentation refs (assign if you have them)")]
    //public CameraShake cameraShake;     // simple camera shake component
    //public float shakeAmount = 0.2f;
    //public float shakeDuration = 0.08f;

    [Header("Timings")]
    public float perEventDelay = 0.35f; // delay between events within a Resolve phase

    // Simple public events (UnityEvent) you can hook Animator triggers to in the Inspector
    public UnityEngine.Events.UnityEvent OnAnyHitStart;
    public UnityEngine.Events.UnityEvent OnAnyHitBlocked;

    private Coroutine playbackCo;

    private void OnEnable()
    {
        if (roundSummaryChannel != null)
            roundSummaryChannel.OnRoundResolved += HandleRoundResolved;

        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (roundSummaryChannel != null)
            roundSummaryChannel.OnRoundResolved -= HandleRoundResolved;

        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        // If we leave Resolve early (timer/transition), stop any running playback
        if (curr != MatchPhase.Resolve && playbackCo != null)
        {
            StopCoroutine(playbackCo);
            playbackCo = null;
        }
    }

    private void HandleRoundResolved(RoundEvent[] eventsArray)
    {
        // Start a fresh sequential playback
        if (playbackCo != null) StopCoroutine(playbackCo);
        playbackCo = StartCoroutine(Co_Play(eventsArray));
    }

    private IEnumerator Co_Play(RoundEvent[] eventsArray)
    {
        if (eventsArray == null || eventsArray.Length == 0)
            yield break;

        foreach (var e in eventsArray)
        {
            // Trigger presentation per event
            if (e.Outcome == SlapOutcome.Hit)
            {
                // Camera shake (optional)
                //if (cameraShake != null)
                //    cameraShake.PlayOneShot(shakeAmount, shakeDuration);

                // Placeholder SFX: route via your SoundManager if present
                // SoundManager.Instance.PlaySlapHit(e.Attacker /*or position*/);

                OnAnyHitStart?.Invoke();
            }
            else
            {
                // Block feedback (lighter shake or just SFX)
                // SoundManager.Instance.PlaySlapBlock(e.Defender);

                OnAnyHitBlocked?.Invoke();
            }

            // Optional: small hitstop-like pause could go here
            yield return new WaitForSeconds(perEventDelay);
        }

        playbackCo = null;
    }
}
