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


		private void Start()
		{
            StartCoroutine(CreateClip(Clips.DISCORD, 0, 0, 1));
            StartCoroutine(CreateClip(Clips.BELL, 1, 1, 0));
		}

		public IEnumerator CreateClip(Clips clip, float delay, float xPos, float yPos)
		{
            yield return new WaitForSeconds(delay);

            //play several sounds, at a delay
            GameObject instance = Instantiate(audioSourcePrefab, new Vector3(xPos, yPos, 0), Quaternion.identity);
            CustomAudioSource cas = instance.GetComponent<CustomAudioSource>();
            cas.Loop = false;
            cas.Clip = clips[(int)clip];
		}
    }
}
