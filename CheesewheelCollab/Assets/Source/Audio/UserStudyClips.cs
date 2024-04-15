using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Source.Audio
{
    public class UserStudyClips : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private GameObject audioSourcePrefab;
        [SerializeField] private AudioClip[] clips;
        public enum Clips { BUSINESS, MOMENT, DISCORD, BELL, GLOCK, KNOCKING, FOOTSTEPS };
        private float globalDelay = 0;

        public void Clip0() // no notif from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.MOMENT, 0, 0, 1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 0, -1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 1, 0));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, -1, 0));
        }

        public void Clip1() // DISCORD ping from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.DISCORD, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));

            StartCoroutine(CreateClip(Clips.DISCORD, 5, 0, -1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, -1));

            StartCoroutine(CreateClip(Clips.DISCORD, 5, 1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 1, 0));

            StartCoroutine(CreateClip(Clips.DISCORD, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip2() // BELL ping from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.BELL, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));

            StartCoroutine(CreateClip(Clips.BELL, 5, 0, -1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, -1));

            StartCoroutine(CreateClip(Clips.BELL, 5, 1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 1, 0));

            StartCoroutine(CreateClip(Clips.BELL, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip3() // GLOCK ping from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.GLOCK, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));

            StartCoroutine(CreateClip(Clips.GLOCK, 5, 0, -1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, -1));

            StartCoroutine(CreateClip(Clips.GLOCK, 5, 1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 1, 0));

            StartCoroutine(CreateClip(Clips.GLOCK, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip4() // KNOCKING ping from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.KNOCKING, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));

            StartCoroutine(CreateClip(Clips.KNOCKING, 5, 0, -1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, -1));

            StartCoroutine(CreateClip(Clips.KNOCKING, 5, 1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 1, 0));

            StartCoroutine(CreateClip(Clips.KNOCKING, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip5() // FOOTSTEPS ping from cardinal directions
        {
            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 5, 0, -1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, -1));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 5, 1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 1, 0));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip6() // SFX Comparison 1, wide angle
        {

            StartCoroutine(CreateClip(Clips.BUSINESS, 0, 1.5f, 2));
            StartCoroutine(NowPlaying());

            StartCoroutine(CreateClip(Clips.MOMENT, 5, -1, 0));

            StartCoroutine(CreateClip(Clips.DISCORD, 7, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.BELL, 7, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.3f, -1, 0));

            StartCoroutine(CreateClip(Clips.GLOCK, 7, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.KNOCKING, 7, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 7, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip7() // SFX Comparison 2, similar-angle test
        {
            StartCoroutine(CreateClip(Clips.BUSINESS, 0, 0, 2.5f));
            StartCoroutine(NowPlaying());

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 0.5f, 1));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 7, 0.5f, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0.5f, 1));

            StartCoroutine(CreateClip(Clips.BELL, 7, 0.5f, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.3f, 0.5f, 1));

            StartCoroutine(CreateClip(Clips.DISCORD, 7, 0.5f, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0.5f, 1));

            StartCoroutine(CreateClip(Clips.KNOCKING, 7, 0.5f, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0.5f, 1));

            StartCoroutine(CreateClip(Clips.GLOCK, 7, 0.5f, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0.5f, 1));
        }

        public IEnumerator CreateClip(Clips clip, float delay, float xPos, float yPos)
		{
            globalDelay += delay;
            yield return new WaitForSeconds(globalDelay);

            //play several sounds, at a delay
            GameObject instance = Instantiate(audioSourcePrefab, new Vector3(xPos, yPos, 0), Quaternion.identity);
            CustomAudioSource cas = instance.GetComponent<CustomAudioSource>();
            cas.Loop = false;
            cas.Clip = clips[(int)clip];
            if (clip == Clips.GLOCK || clip == Clips.BELL)
            {
                cas.Volume = 0.6f;
            }
        }

        public IEnumerator NowPlaying()
		{
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0);
            yield return new WaitForSeconds(46f);
            gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 1, 0);

        }
    }
}
