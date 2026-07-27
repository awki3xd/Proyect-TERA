using System.Collections;
using UnityEngine;

public class TextoDañoFlotante : MonoBehaviour
{
    private TextMesh mainTextMesh;
    private TextMesh[] outlineTextMeshes;
    private float duracion = 1f;
    private float velocidadFlotar = 1.2f;

    /// <summary>
    /// Configura el texto, color, estilo de negrita y contorno negro para el texto flotante.
    /// </summary>
    public void Inicializar(string texto, Color color, float escala = 0.05f)
    {
        // 1. TextMesh principal
        mainTextMesh = GetComponent<TextMesh>();
        if (mainTextMesh == null)
        {
            mainTextMesh = gameObject.AddComponent<TextMesh>();
        }

        ConfigurarTextMesh(mainTextMesh, texto, color, FontStyle.Bold, 80, 20);
        transform.localScale = Vector3.one * escala;

        // 2. Crear contorno negro de 4 direccionales para maxima legibilidad
        CrearContornoNegro(texto, escala);

        StartCoroutine(AnimarTextoCo());
    }

    private void ConfigurarTextMesh(TextMesh tm, string texto, Color color, FontStyle estilo, int fontSize, int sortingOrder)
    {
        tm.text = texto;
        tm.color = color;
        tm.fontStyle = estilo;
        tm.fontSize = fontSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        MeshRenderer mr = tm.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = sortingOrder;
        }
    }

    private void CrearContornoNegro(string texto, float escala)
    {
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-0.025f, 0f, 0.01f),
            new Vector3(0.025f, 0f, 0.01f),
            new Vector3(0f, -0.025f, 0.01f),
            new Vector3(0f, 0.025f, 0.01f)
        };

        outlineTextMeshes = new TextMesh[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject outlineGo = new GameObject("Outline_" + i);
            outlineGo.transform.SetParent(transform, false);
            outlineGo.transform.localPosition = offsets[i];
            outlineGo.transform.localScale = Vector3.one;

            TextMesh tm = outlineGo.AddComponent<TextMesh>();
            ConfigurarTextMesh(tm, texto, Color.black, FontStyle.Bold, 80, 19);
            outlineTextMeshes[i] = tm;
        }
    }

    private IEnumerator AnimarTextoCo()
    {
        float elapsed = 0f;
        Color colorOriginal = mainTextMesh.color;

        // Añadir una pequeña dispersión horizontal inicial
        Vector3 direccionDesplazamiento = new Vector3(Random.Range(-0.4f, 0.4f), velocidadFlotar, 0f);

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;

            // Flotar hacia arriba
            transform.Translate(direccionDesplazamiento * Time.deltaTime, Space.World);

            // Desvanecer opacidad del texto principal y contornos
            float alpha = Mathf.Lerp(1f, 0f, t);
            mainTextMesh.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, alpha);

            if (outlineTextMeshes != null)
            {
                foreach (var tm in outlineTextMeshes)
                {
                    if (tm != null)
                    {
                        tm.color = new Color(0f, 0f, 0f, alpha);
                    }
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // --- MÉTODOS ESTÁTICOS CENTRALIZADOS ---

    /// <summary>
    /// Crea un texto flotante de daño (Rojo para jugador/nodos, Blanco para enemigos).
    /// </summary>
    public static void Crear(Vector2 posicion, float daño, Color color)
    {
        int cantidadInt = Mathf.RoundToInt(daño);
        if (cantidadInt <= 0) return;

        CrearTextoGeneral(posicion, cantidadInt.ToString(), color);
    }

    /// <summary>
    /// Crea un texto flotante de curación en verde brillante con prefijo "+".
    /// </summary>
    public static void CrearCuracion(Vector2 posicion, float curacion)
    {
        int cantidadInt = Mathf.RoundToInt(curacion);
        if (cantidadInt <= 0) return;

        Color colorVerde = new Color(0.2f, 1.0f, 0.3f);
        CrearTextoGeneral(posicion, "+" + cantidadInt, colorVerde);
    }

    /// <summary>
    /// Crea un texto flotante de recolección de materiales en amarillo-naranjoso cálido con prefijo "+".
    /// </summary>
    public static void CrearMaterial(Vector2 posicion, int cantidad)
    {
        if (cantidad <= 0) return;

        Color colorAmarilloNaranja = new Color(1.0f, 0.7f, 0.0f);
        CrearTextoGeneral(posicion, "+" + cantidad, colorAmarilloNaranja);
    }

    private static void CrearTextoGeneral(Vector2 posicion, string texto, Color color)
    {
        GameObject go = new GameObject("FloatingText_" + texto);
        go.transform.position = posicion + new Vector2(Random.Range(-0.2f, 0.2f), 0.5f);

        TextoDañoFlotante script = go.AddComponent<TextoDañoFlotante>();
        script.Inicializar(texto, color);
    }
}
