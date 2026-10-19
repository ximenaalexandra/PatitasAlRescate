using UnityEngine;

// Obstáculo que le quita un corazón a la rescatista. Si tiene velocidad, rueda hacia la izquierda.
public class Obstaculo : MonoBehaviour
{
    public int danio = 1;
    public float velocidad = 0f;     // 0 = quieto
    public bool gira = false;        // la pelota gira al rodar
    public float limiteX = 15.5f;

    void Update()
    {
        if (velocidad <= 0f) return;
        var n = LevelManager.Instancia;
        if (n != null && (!n.Jugando || !n.MisionIniciada || UIJuego.PanelAbierto)) return; // ruedan cuando empieza la misión

        transform.position += Vector3.left * velocidad * Time.deltaTime;
        if (gira) transform.Rotate(0f, 0f, velocidad * 140f * Time.deltaTime);
        if (transform.position.x < -limiteX)
            transform.position = new Vector3(limiteX, transform.position.y, transform.position.z);
    }
}
