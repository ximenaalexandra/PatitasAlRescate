using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Minimapa del barrio: muestra mascotas que necesitan ayuda, coleccionables, NPC, estación y familias.
// Se amplía en el centro de la pantalla cuando la rescatista mira el celular (tecla M).
public class MapaUI : MonoBehaviour
{
    public RectTransform area;               // franja con el dibujo del barrio
    public RectTransform marcadorJugadora;
    public RectTransform plantillaMarcador;  // imagen que se copia por cada marcador
    public GameObject leyenda;
    public float mundoMinX = -15f;
    public float mundoMaxX = 15f;
    public float escalaGrande = 2.4f;

    PlayerController jugadora;
    readonly List<MarcadorMapa> marcas = new List<MarcadorMapa>();
    readonly List<Image> imagenes = new List<Image>();
    RectTransform rt;
    Vector2 posNormal;

    void Awake()
    {
        rt = (RectTransform)transform;
        posNormal = rt.anchoredPosition;
        if (plantillaMarcador != null) plantillaMarcador.gameObject.SetActive(false);
        if (leyenda != null) leyenda.SetActive(false);
    }

    public void Configurar(PlayerController j)
    {
        jugadora = j;
        foreach (var m in FindObjectsByType<MarcadorMapa>(FindObjectsSortMode.InstanceID))
        {
            var r = Instantiate(plantillaMarcador, area);
            r.gameObject.SetActive(true);
            r.name = "Marcador_" + m.name;
            r.sizeDelta = m.tamano;
            var img = r.GetComponent<Image>();
            img.sprite = m.icono;
            marcas.Add(m);
            imagenes.Add(img);
        }
        marcadorJugadora.SetAsLastSibling();
    }

    float AX(float xMundo)
    {
        return Mathf.InverseLerp(mundoMinX, mundoMaxX, xMundo) * area.rect.width;
    }

    void Update()
    {
        if (jugadora == null) return;

        marcadorJugadora.anchoredPosition = new Vector2(AX(jugadora.transform.position.x), 2f);
        for (int i = 0; i < marcas.Count; i++)
        {
            var m = marcas[i];
            if (m == null) { imagenes[i].gameObject.SetActive(false); continue; } // coleccionable ya recogido
            var r = (RectTransform)imagenes[i].transform;
            r.anchoredPosition = new Vector2(AX(m.transform.position.x), m.alturaEnMapa);
            imagenes[i].color = m.color;
        }

        bool grande = jugadora.UsandoCelular;
        var padre = (RectTransform)rt.parent;
        Vector2 tam = rt.rect.size * escalaGrande;
        Vector2 posGrande = new Vector2(-padre.rect.width / 2f + tam.x / 2f, -padre.rect.height / 2f + tam.y / 2f);
        float s = Mathf.Lerp(rt.localScale.x, grande ? escalaGrande : 1f, 12f * Time.deltaTime);
        rt.localScale = new Vector3(s, s, 1f);
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, grande ? posGrande : posNormal, 12f * Time.deltaTime);
        if (leyenda != null) leyenda.SetActive(grande);
    }
}
