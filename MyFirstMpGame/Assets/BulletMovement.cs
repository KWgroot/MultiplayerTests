using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    private Rigidbody rigidbody;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    public bool player1 = true;
    public float bulletSpeed = 5f;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (player1)
            rigidbody.velocity = (Vector3.right + Vector3.up) * bulletSpeed;
        else
            rigidbody.velocity = (Vector3.left + Vector3.up) * bulletSpeed;

        Destroy(this.gameObject, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(rigidbody.velocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (this.gameObject.tag != collision.gameObject.tag)
            Destroy(this.gameObject);
    }
}
