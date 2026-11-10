using System.Collections.Generic;
using UnityEngine;

namespace Cluck
{
    internal sealed class GameAudio
    {
        const string MusicKey = "Cluck.MusicVolume";
        const string EffectsKey = "Cluck.EffectsVolume";
        readonly AudioSource music;
        readonly AudioSource effects;
        readonly Dictionary<AudioClip, float> lastPlayed = new();

        internal float MusicVolume => music.volume;
        internal float EffectsVolume => effects.volume;

        internal GameAudio(Transform parent, AudioClip soundtrack)
        {
            var obj = new GameObject("Audio");
            obj.transform.SetParent(parent, false);
            music = obj.AddComponent<AudioSource>();
            effects = obj.AddComponent<AudioSource>();
            music.playOnAwake = effects.playOnAwake = false;
            music.spatialBlend = effects.spatialBlend = 0;
            music.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, .48f));
            effects.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, .67f));
            music.clip = soundtrack;
            music.loop = true;
            if (music.clip != null) music.Play();
        }

        internal void Play(AudioClip clip, float gain = 1, float gap = 0)
        {
            if (clip == null) return;
            float now = Time.unscaledTime;
            if (gap > 0 && lastPlayed.TryGetValue(clip, out float previous) && now - previous < gap) return;
            lastPlayed[clip] = now;
            effects.PlayOneShot(clip, gain);
        }

        internal void SetMusic(float volume)
        {
            music.volume = volume;
            PlayerPrefs.SetFloat(MusicKey, volume);
        }

        internal void SetEffects(float volume)
        {
            effects.volume = volume;
            PlayerPrefs.SetFloat(EffectsKey, volume);
        }

        internal void SetPaused(bool paused)
        {
            if (paused)
            {
                music.Pause();
                effects.Stop();
            }
            else music.UnPause();
        }
    }
}
