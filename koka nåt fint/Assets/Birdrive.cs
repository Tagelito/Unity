using UnityEngine;
using UnityEngine.SceneManagement;

public class Birdrive : MonoBehaviour
{
    public Rigidbody2D rigidBody;
    public float jumpStrength;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rigidBody.linearVelocityY = jumpStrength;
        }

        if (transform.position.y <= -23.41)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
