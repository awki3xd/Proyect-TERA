using UnityEngine;

public class Ola : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bala"))
        {
            Destroy(other.gameObject);
        }
    }

}
