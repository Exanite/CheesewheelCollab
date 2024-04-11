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
            Clip1();
		}

        public void Clip1()
		{
            StartCoroutine(CreateClip(Clips.DISCORD, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, 0, 1));
            StartCoroutine(CreateClip(Clips.DISCORD, 5, -1, 0));
            StartCoroutine(CreateClip(Clips.MOMENT, 0.5f, -1, 0));
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
