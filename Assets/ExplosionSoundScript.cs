using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionSoundScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AudioSource boomSound = GetComponent<AudioSource>();
        boomSound.Play();
        Destroy(gameObject, boomSound.clip.length);
    }
}
