using UnityEngine;
using UnityEngine.InputSystem;

// Salud y poder de la rescatista, reflejados en las barras del HUD.
// Teclas de prueba: H = recibir dano, J = curar, P = usar poder, O = recargar poder.
public class PlayerStats : MonoBehaviour
{
    [Range(0, 100)] public float salud = 100f;
    [Range(0, 100)] public float poder = 60f;
    public RectTransform barraSalud;
    public RectTransform barraPoder;

    void Update()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.hKey.wasPressedThisFrame) salud -= 10f;
            if (k.jKey.wasPressedThisFrame) salud += 10f;
            if (k.pKey.wasPressedThisFrame) poder -= 10f;
            if (k.oKey.wasPressedThisFrame) poder += 10f;
        }
        salud = Mathf.Clamp(salud, 0f, 100f);
        poder = Mathf.Clamp(poder, 0f, 100f);
        Refrescar();
    }

    void OnValidate() { Refrescar(); }

    void Refrescar()
    {
        if (barraSalud != null) barraSalud.anchorMax = new Vector2(salud / 100f, 1f);
        if (barraPoder != null) barraPoder.anchorMax = new Vector2(poder / 100f, 1f);
    }
}
