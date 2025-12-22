using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class BulletAudioTrigger : MonoBehaviour
{
    public float time = 3;
    public AudioClip onClip;

    void Start()
    {
        var audio = gameObject.AddComponent<AudioSource>();
        if (onClip != null)
        {
            audio.clip = onClip;
            audio.Play();
        }
        AutoDestroy(time);

    }
    void AutoDestroy(float time)
    {
        Invoke("Destroy", time);
    }
    public void Destroy()
    {
        Destroy(gameObject);
    }
}