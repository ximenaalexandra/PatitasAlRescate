using UnityEngine;

// Clase base de todo lo que se activa con la tecla E (NPC, carteles, mascotas, estaciones, familias).
// Necesita un Collider2D con Is Trigger activado que marca el área de interacción.
public abstract class Interactuable : MonoBehaviour
{
    [Tooltip("Indicador flotante con la tecla E que aparece al acercarse")]
    public GameObject indicador;

    // Texto que se muestra en el HUD: "Presiona E para ..."
    public abstract string Accion { get; }

    public virtual bool PuedeInteractuar { get { return true; } }

    public abstract void Interactuar(PlayerController jugadora);

    protected virtual void LateUpdate()
    {
        if (indicador == null) return;
        var j = PlayerController.Instancia;
        bool mostrar = j != null && j.Objetivo == this && !UIJuego.PanelAbierto;
        if (indicador.activeSelf != mostrar) indicador.SetActive(mostrar);
    }
}
