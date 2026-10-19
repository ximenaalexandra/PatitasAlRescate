using UnityEngine;
using UnityEngine.InputSystem;

// Mueve a la rescatista con A/D o flechas. Shift izquierdo = correr.
public class PlayerMovement : MonoBehaviour
{
    public float velocidad = 2.5f;
    public float multiplicadorCorrer = 2f;
    public float limiteX = 14.5f; // borde del fondo

    Animator anim;
    SpriteRenderer sr;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float x = 0f;
        bool correr = false;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1f;
            correr = k.leftShiftKey.isPressed && x != 0f;
        }

        float v = velocidad * (correr ? multiplicadorCorrer : 1f);
        transform.Translate(x * v * Time.deltaTime, 0f, 0f);
        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -limiteX, limiteX);
        transform.position = pos;

        anim.SetBool("IsWalking", x != 0f);
        anim.SetBool("IsRunning", correr);
        if (x != 0f) sr.flipX = x < 0f; // el sprite original mira a la derecha
    }
}
