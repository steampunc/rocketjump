using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnIfTouched : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Respawn")
        {
            Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            transform.position = GameObject.Find("Spawnpoint").transform.position;
        }
    }
}
