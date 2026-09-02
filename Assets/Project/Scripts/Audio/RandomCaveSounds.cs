using System.Collections;
using UnityEngine;

public class RandomCaveSounds : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private float minimumDelay = 8.0f;
    [SerializeField] private float maximumDelay = 24.0f;
    [SerializeField] private float minimumVolume = 0.25f;
    [SerializeField] private float maximumVolume = 0.6f;

    private IEnumerator Start()
    {
        while (true)
        {
            float delay = Random.Range(minimumDelay, maximumDelay);
            yield return new WaitForSeconds(delay);

            if (clips == null || clips.Length == 0 || audioSource == null)
                continue;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = Random.Range(0.92f, 1.05f);
            audioSource.PlayOneShot(
                clip,
                Random.Range(minimumVolume, maximumVolume)
            );
        }
    }
}
