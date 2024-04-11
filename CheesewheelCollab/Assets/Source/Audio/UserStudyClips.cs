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


		private void Start()
		{
            Clip7();
		}

        public void Clip0() // no notif from all directions
        {
            StartCoroutine(CreateClip(Clips.MOMENT, 0, 0, 1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 0, -1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 1, 0));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, -1, 0));
        }

        public void Clip1() // discord ping from all directions
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

        public void Clip2() // bell ping from all directions
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

        public void Clip3() // glock ping from all directions
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

        public void Clip4() // KNOCKING ping from all directions
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

        public void Clip5() // FOOTSTEPS ping from all directions
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

        public void Clip6() // interruption test
        {
            StartCoroutine(CreateClip(Clips.BUSINESS, 0, 1, 2));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, -1, 0));

            StartCoroutine(CreateClip(Clips.DISCORD, 6, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.BELL, 6, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.GLOCK, 6, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.KNOCKING, 6, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));

            StartCoroutine(CreateClip(Clips.FOOTSTEPS, 6, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
        }

        public void Clip7() // similar-angle test
        {
            StartCoroutine(CreateClip(Clips.BUSINESS, 0, 0, 2));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 0, 1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 1, 0));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, 0, -1));

            StartCoroutine(CreateClip(Clips.MOMENT, 5, -1, 0));
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
		}
    }
}
