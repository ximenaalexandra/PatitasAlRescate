using UnityEngine;

// La camara sigue al jugador en horizontal sin salirse del fondo.
public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public float limiteX = 15f; // mitad del ancho del fondo, en unidades

    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (objetivo == null) return;
        float mitad = cam.orthographicSize * cam.aspect;
        float max = Mathf.Max(0f, limiteX - mitad);
        var p = transform.position;
        p.x = Mathf.Clamp(objetivo.position.x, -max, max);
        transform.position = p;
    }
}
