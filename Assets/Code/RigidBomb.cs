using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidBomb : MonoBehaviour
{
    public float explosionStrength = 16f;
    public float explosionRadius = 4f;
    public float upStrength = 4f;
    public float fuse_time = 4.0f; // seconds
    public float rocketSpeed = 21f;

    public GameObject explosionParticle;
    public GameObject explosionSound;

    private Rigidbody rb;
    private AudioSource audioSource;

    private bool exploding;

    //private bool willExplode = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.AddRelativeForce(Vector3.forward * rocketSpeed, ForceMode.VelocityChange);

        exploding = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    //put bomb stuff in here so it's not linked to framerate
    private void FixedUpdate()
    {
        if (fuse_time > 0)
        {
            fuse_time -= Time.fixedDeltaTime;
            //Debug.Log(fuse_time);
        }

        if (fuse_time <= 0)
        {
            Explode();
        }

        //if moving too slow, speeds up to minSpeed
        if (rb.velocity.magnitude != rocketSpeed)
        {
            rb.velocity = rb.velocity.normalized * rocketSpeed;
        }

        //always point in direction of movement
        Vector3 v_direction = rb.velocity.normalized;
        if (rb.velocity.magnitude != 0 && transform.forward != v_direction)
        {
            transform.forward = v_direction;
        }
    }

    void Explode() {
        //made explosion particles separate from bomb object
        Instantiate(explosionParticle, transform.position, transform.rotation);
        Instantiate(explosionSound, transform.position, transform.rotation);
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders) {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && rb.gameObject != gameObject) {
                Vector3 explosionForce = CalculateExplosion(transform.position, rb.position);
                rb.AddForce(explosionForce, ForceMode.VelocityChange);
                Debug.DrawRay(rb.position, (explosionForce + rb.velocity) * 0.5f, Color.white, 1f);
                //Debug.Log(explosionForce);
            }
        }
        //destroys instantly, rather than waiting for particles to finish playing
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision coll) {
        if (coll.gameObject.tag != "Player") {
            if (exploding == false)
            {
                exploding = true;
                Explode();
            }
        }
    }

    Vector3 CalculateExplosion(Vector3 origin, Vector3 target) {
        Vector3 displacement = (target - origin);
        Vector3 dir = (displacement).normalized;
        float strength = Mathf.Abs(explosionStrength * Mathf.Cos(displacement.magnitude/explosionRadius * Mathf.PI / 2));
        //float strength = explosionStrength;

        //return dir * strength + Vector3.up * upStrength;
        return strength * (dir + Vector3.up * upStrength);
    }
}
