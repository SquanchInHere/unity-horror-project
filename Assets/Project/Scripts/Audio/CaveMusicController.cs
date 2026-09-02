using System.Collections;
using UnityEngine;

public class CaveMusicController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] tracks;

    [SerializeField] private float targetVolume = 0.12f;
    [SerializeField] private float fadeDuration = 4.0f;
    [SerializeField] private float minimumSilence = 30.0f;
    [SerializeField] private float maximumSilence = 70.0f;

    private int previousTrack = -1;

    private IEnumerator Start()
    {
        if (musicSource == null)
            yield break;

        musicSource.loop = false;
        musicSource.volume = 0.0f;

        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minimumSilence, maximumSilence)
            );

            if (tracks == null || tracks.Length == 0)
                continue;

            int index = Random.Range(0, tracks.Length);

            if (tracks.Length > 1 && index == previousTrack)
                index = (index + 1) % tracks.Length;

            previousTrack = index;

            AudioClip track = tracks[index];
            musicSource.clip = track;
            musicSource.Play();

            yield return FadeTo(targetVolume);

            float playingTime = Mathf.Max(
                0.0f,
                track.length - fadeDuration * 2.0f
            );

            yield return new WaitForSeconds(playingTime);
            yield return FadeTo(0.0f);

            musicSource.Stop();
        }
    }

    private IEnumerator FadeTo(float target)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0.0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            musicSource.volume = Mathf.Lerp(
                startVolume,
                target,
                elapsed / fadeDuration
            );

            yield return null;
        }

        musicSource.volume = target;
    }
}
