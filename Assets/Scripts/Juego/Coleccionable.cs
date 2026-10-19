using UnityEngine;

// Objeto coleccionable: se recoge al tocarlo, desaparece y suma una sola vez al progreso.
public class Coleccionable : MonoBehaviour
{
    public string nombreItem = "Botiquín";
    public int valor = 1;

    bool recogido;
    Vector3 inicio;

    void Start()
    {
        inicio = transform.position;
    }

    void Update()
    {
        transform.position = inicio + Vector3.up * Mathf.Sin(Time.time * 3f + inicio.x) * 0.08f;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (recogido || !otro.CompareTag("Player")) return; // solo la jugadora y solo una vez
        recogido = true;
        Sonidos.Tocar("recoger");
        if (LevelManager.Instancia != null) LevelManager.Instancia.AgregarItem(nombreItem, valor);
        Destroy(gameObject);
    }
}
