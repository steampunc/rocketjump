using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidBomb : MonoBehaviour
{

    public float explosionStrength = 0.5f;
    public float explosionRadius = 10.0f;
    
    public float upStrength = 0.5f;
    // Start is called before the first frame update

    float fuse_time = 4.0f; // seconds
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (fuse_time > 0) {
            fuse_time -= Time.deltaTime;
            Debug.Log(fuse_time);
        } else {
            Explode();
        }
    }

    void Explode() {
        ParticleSystem exp = GetComponent<ParticleSystem>();
        exp.Play();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders) {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddForce(CalculateExplosion(transform.position, rb.position), ForceMode.Impulse);
                Debug.Log(CalculateExplosion(transform.position, rb.position));
            }
        }
        Destroy(gameObject, exp.main.duration);

    }

    void OnCollisionEnter(Collision coll) {
        Explode();
    }

    Vector3 CalculateExplosion(Vector3 origin, Vector3 target) {
        Vector3 displacement = (target - origin);
        Vector3 dir = (displacement).normalized;
        float strength = explosionStrength;// * Mathf.Cos(displacement.magnitude/explosionRadius * Mathf.PI / 2);
        Debug.Log(dir);
        return dir * strength + Vector3.up * upStrength;
    }
}
