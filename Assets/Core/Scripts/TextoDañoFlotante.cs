using System.Collections;
using UnityEngine;

public class TextoDañoFlotante : MonoBehaviour
{
    private TextMesh textMesh;
    private float duracion = 1f;
    private float velocidadFlotar = 1f;

    /// <summary>
    /// Configura el texto, color y escala inicial del número de daño flotante.
    /// </summary>
    public void Inicializar(string texto, Color color, float escala = 0.05f)
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        // Configurar alineación y tamaño de fuente
        textMesh.text = texto;
        textMesh.color = color;
        textMesh.fontSize = 80; // Alta resolución para que se vea nítido
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Reducir escala para contrarrestar el gran tamaño de fuente y que no se pixelee
        transform.localScale = Vector3.one * escala;

        // Asegurarse de que esté por encima de todos los sprites 2D en el rendering
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 20; 
        }

        StartCoroutine(AnimarTextoCo());
    }

    private IEnumerator AnimarTextoCo()
    {
        float elapsed = 0f;
        Color colorOriginal = textMesh.color;

        // Añadir una pequeña dispersión horizontal inicial para que múltiples números no se encimen
        Vector3 direccionDesplazamiento = new Vector3(Random.Range(-0.5f, 0.5f), velocidadFlotar, 0f);

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;

            // Flotar hacia arriba y ligeramente hacia los lados
            transform.Translate(direccionDesplazamiento * Time.deltaTime, Space.World);

            // Desvanecer opacidad
            textMesh.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Método estático utilitario para crear un número flotante en cualquier coordenada del mundo.
    /// </summary>
    public static void Crear(Vector2 posicion, float daño, Color color)
    {
        GameObject go = new GameObject("DamageNumber");
        // Desplazar un poco hacia arriba para que no nazca exactamente sobre el pivote de los pies del enemigo
        go.transform.position = posicion + new Vector2(0f, 0.5f);

        TextoDañoFlotante textoScript = go.AddComponent<TextoDañoFlotante>();
        // Redondear a entero para una estética más limpia
        string textoDaño = Mathf.RoundToInt(daño).ToString();
        textoScript.Inicializar(textoDaño, color);
    }
}
