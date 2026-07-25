using UnityEngine;

public class BarraVida : MonoBehaviour
{
    [Header("Referencias Visuales (Asignar en Inspector)")]
    [Tooltip("El GameObject del fondo negro de la barra de vida.")]
    public GameObject fondo;
    [Tooltip("El Transform del relleno rojo de la barra (debe tener su pivote a la izquierda).")]
    public Transform relleno;

    private System.Func<float> obtenerVidaActual;
    private System.Func<float> obtenerVidaMaxima;
    private float escalaXOriginal = 1f;
    private bool inicializado = false;

    /// <summary>
    /// Asocia los delegados para consultar la vida actual y máxima desde el personaje/nodo.
    /// </summary>
    public void Inicializar(System.Func<float> obtenerVidaActual, System.Func<float> obtenerVidaMaxima)
    {
        this.obtenerVidaActual = obtenerVidaActual;
        this.obtenerVidaMaxima = obtenerVidaMaxima;
        inicializado = true;

        if (relleno != null)
        {
            escalaXOriginal = relleno.localScale.x;
        }
    }

    private void Start()
    {
        // Guardar la escala X original definida en el editor para redimensionar proporcionalmente
        if (relleno != null && escalaXOriginal == 1f)
        {
            escalaXOriginal = relleno.localScale.x;
        }
    }

    private void Update()
    {
        if (!inicializado || obtenerVidaActual == null || obtenerVidaMaxima == null) return;

        float vidaActual = obtenerVidaActual();
        float vidaMaxima = obtenerVidaMaxima();

        if (vidaMaxima <= 0f) return;

        float porcentaje = Mathf.Clamp01(vidaActual / vidaMaxima);

        // Si la vida está al 100%, ocultar la barra por completo
        if (porcentaje >= 0.999f)
        {
            if (fondo != null && fondo.activeSelf) fondo.SetActive(false);
            if (relleno != null && relleno.gameObject.activeSelf) relleno.gameObject.SetActive(false);
        }
        else
        {
            if (fondo != null && !fondo.activeSelf) fondo.SetActive(true);
            if (relleno != null && !relleno.gameObject.activeSelf) relleno.gameObject.SetActive(true);

            // Escalar el relleno proporcionalmente
            if (relleno != null)
            {
                relleno.localScale = new Vector3(escalaXOriginal * porcentaje, relleno.localScale.y, relleno.localScale.z);
            }
        }
    }
}
