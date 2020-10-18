using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidBomb : MonoBehaviour
{
    public float explosionStrength = 1f;
    public float explosionRadius = 1f;
    public float upStrength = 1f;
    public float fuse_time = 4.0f; // seconds

    private float minSpeed = 10f;

    public GameObject explosionParticle;

    private Rigidbody rb;

    private bool willExplode = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // Update is called once per frame
    void Update()
    {
        if (fuse_time > 0) {
            fuse_time -= Time.deltaTime;
            //Debug.Log(fuse_time);
        } else {
            willExplode = true;
        }

        //if moving too slow, speeds up to minSpeed
        if (rb.velocity.magnitude < minSpeed)
        {
            rb.velocity = rb.velocity.normalized * minSpeed;
        }

        if (willExplode) {
            Explode();
        }
    }

    void Explode() {
        //made explosion particles separate from bomb object
        Instantiate(explosionParticle, transform.position, transform.rotation);
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders) {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && rb.gameObject != gameObject) {
                rb.AddForce(CalculateExplosion(transform.position, rb.position), ForceMode.VelocityChange);
            }
        }
        //destroys instantly, rather than waiting for particles to finish playing
        //Debug.Log("destroying");
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision coll) {
        if (coll.gameObject.tag != "Player") {
            willExplode = true;
        }
    }

    Vector3 CalculateExplosion(Vector3 origin, Vector3 target) {
        Vector3 displacement = (target - origin);
        Vector3 dir = (displacement).normalized;
        //float strength = explosionStrength * Mathf.Cos(displacement.magnitude/explosionRadius * Mathf.PI / 2);
        
        Debug.DrawRay(target, dir * explosionStrength + Vector3.up * upStrength, Color.white, 1f);
        return dir * explosionStrength + Vector3.up * upStrength;
    }
}
