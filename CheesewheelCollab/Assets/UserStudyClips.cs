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

		private void Start()
		{
            ClipTest();
            Invoke("ClipTest", 3);
		}

		public void ClipTest()
		{
            //play several sounds, at a delay
            GameObject instance = Instantiate(audioSourcePrefab);
            CustomAudioSource cas = instance.GetComponent<CustomAudioSource>();
            cas.Loop = false;
            cas.Clip = clips[0];
		}
    }
}
