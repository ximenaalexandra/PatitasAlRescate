using UnityEngine;

// Animación sencilla por cuadros para los NPC; opcionalmente mira hacia la jugadora.
[RequireComponent(typeof(SpriteRenderer))]
public class AnimadorSprites : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 6f;
    public bool mirarAJugadora = true;

    SpriteRenderer sr;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        t = Random.value;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (frames != null && frames.Length > 0) sr.sprite = frames[(int)(t * fps) % frames.Length];
        var j = PlayerController.Instancia;
        if (mirarAJugadora && j != null) sr.flipX = j.transform.position.x < transform.position.x; // el sprite original mira a la derecha
    }
}
