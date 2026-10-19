using UnityEngine;

// Objeto interactivo educativo: un cartel con consejos para cuidar mascotas.
public class CartelInfo : Interactuable
{
    public string titulo = "Consejo";
    [TextArea(3, 8)]
    public string contenido;

    public override string Accion { get { return "leer el cartel"; } }

    public override void Interactuar(PlayerController jugadora)
    {
        if (UIJuego.Instancia != null) UIJuego.Instancia.MostrarInfo(titulo, contenido);
    }
}
