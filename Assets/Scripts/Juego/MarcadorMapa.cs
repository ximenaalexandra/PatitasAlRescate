using UnityEngine;

// Marca un objeto para que aparezca en el minimapa.
public class MarcadorMapa : MonoBehaviour
{
    public Sprite icono;
    public Color color = Color.white;
    public Vector2 tamano = new Vector2(14, 14);
    public float alturaEnMapa = -16f;
}
