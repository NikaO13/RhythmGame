using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SelectMusic : MonoBehaviour
{
    [Header("Звук")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float fadeDuration = 0.5f;

    public async void LoadScene(int index)
    {
        await FadeOutAudioAsync();
        SceneManager.LoadScene(index);
    }

    private async Task FadeOutAudioAsync()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            var startVolume = bgmSource.volume;
            var currentTime = 0f;

            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, currentTime / fadeDuration);
                await Task.Yield();
            }

            bgmSource.Stop();
        }
    }
}