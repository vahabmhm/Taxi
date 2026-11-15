using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The taxi sounds: clips from Resources/TaxiAudio ( from the chase and the crowd games ) and a car horn made in
    /// code. Follows the game's sound setting ( "SoundVolume" in PlayerPrefs, see ECCAudioToggle )
    /// </summary>
    public static class TaxiAudio
    {
        public const string Pickup = "Pickup";
        public const string Cash = "Cash";
        public const string Siren = "Siren";
        public const string Fanfare = "fanfare";
        public const string Bad = "gate_bad";
        public const string Pop = "pop";
        public const string Clink = "clink";
        public const string Combo = "combo";

        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioClip horn;
        static AudioSource source;

        static float Volume
        {
            get { return PlayerPrefs.GetFloat("SoundVolume", 1); }
        }

        static AudioSource Source
        {
            get
            {
                if (source == null)
                {
                    GameObject holder = new GameObject("TaxiAudio");
                    Object.DontDestroyOnLoad(holder);
                    source = holder.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0;
                }

                return source;
            }
        }

        public static void Play(string clipName, float volume = 1, float pitch = 1)
        {
            if (Volume <= 0) return;

            AudioClip clip;

            if (clips.TryGetValue(clipName, out clip) == false)
            {
                clip = Resources.Load<AudioClip>("TaxiAudio/" + clipName);
                clips[clipName] = clip;
            }

            if (clip == null) return;

            Source.pitch = pitch;
            Source.PlayOneShot(clip, volume * Volume);
        }

        /// <summary>
        /// A car horn. Quieter when far from the camera
        /// </summary>
        public static void Honk(Vector3 position, float volume = 1)
        {
            if (Volume <= 0) return;

            Camera view = Camera.main;

            if (view)
            {
                float distance = ECCRuntimeVisuals.FlatDistance(view.transform.position, position);

                volume *= Mathf.Clamp01(1.3f - distance / 90);
            }

            if (volume <= 0.02f) return;

            Source.pitch = Random.Range(0.92f, 1.08f);
            Source.PlayOneShot(Horn, volume * Volume);
        }

        /// <summary>
        /// Two detuned tones, a little buzzy, with a soft start and end
        /// </summary>
        static AudioClip Horn
        {
            get
            {
                if (horn) return horn;

                const int rate = 22050;
                const float length = 0.45f;

                int samples = (int)(rate * length);
                float[] data = new float[samples];

                for (int index = 0; index < samples; index++)
                {
                    float time = index / (float)rate;
                    float envelope = Mathf.Clamp01(time / 0.02f) * Mathf.Clamp01((length - time) / 0.06f);

                    float tone = Mathf.Sin(2 * Mathf.PI * 392 * time) + Mathf.Sin(2 * Mathf.PI * 494 * time) * 0.8f;
                    tone += Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 196 * time)) * 0.25f;

                    data[index] = Mathf.Clamp(tone * 0.3f, -1, 1) * envelope;
                }

                horn = AudioClip.Create("TaxiHorn", samples, 1, rate, false);
                horn.SetData(data, 0);

                return horn;
            }
        }
    }
}
