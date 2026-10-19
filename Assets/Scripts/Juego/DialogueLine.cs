using UnityEngine;

// Una línea de diálogo: quién habla, de qué color se ve su nombre y qué dice.
// En los textos, {jugadora} se reemplaza por el nombre de la rescatista elegida.
[System.Serializable]
public struct DialogueLine
{
    [Tooltip("Nombre de quien habla, por ejemplo 'Dra. Lucía' o '{jugadora} (tú)'")]
    public string speakerName;
    [Tooltip("Color del nombre (si es transparente se usa blanco)")]
    public Color nameColor;
    [TextArea(2, 5)]
    public string text;

    public DialogueLine(string speaker, Color color, string texto)
    {
        speakerName = speaker;
        nameColor = color;
        text = texto;
    }
}
