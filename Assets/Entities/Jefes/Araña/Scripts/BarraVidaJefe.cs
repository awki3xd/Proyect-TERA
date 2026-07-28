using UnityEngine;

public class BarraVidaJefe : MonoBehaviour
{
    [Header("Referencias Visuales (Asignar en Inspector)")]
    [Tooltip("GameObject del fondo o marco de la barra de vida del jefe.")]
    public GameObject fondo;

    [Tooltip("Transform del relleno de la barra (con pivote a la izquierda para escalar en X).")]
    public Transform rellenoTransform;

    [Tooltip("SpriteRenderer del relleno para cambiar su apariencia visual según la fase.")]
    public SpriteRenderer rellenoSpriteRenderer;

    [Header("Sprites de Relleno según la Fase de Vida")]
    [Tooltip("Sprite del relleno cuando la vida está por encima del 50% (Fase 1/Normal).")]
    public Sprite spriteRellenoFase1;

    [Tooltip("Sprite del relleno cuando la vida baja del 50% (Fase 2/Furia).")]
    public Sprite spriteRellenoFase2;

    private System.Func<float> obtenerVidaActual;
    private System.Func<float> obtenerVidaMaxima;
    private float escalaXOriginal = 1f;
    private bool inicializado = false;

    /// <summary>
    /// Asocia los delegados para consultar la salud del jefe en tiempo real.
    /// </summary>
    public void Inicializar(System.Func<float> obtenerVidaActual, System.Func<float> obtenerVidaMaxima)
    {
        this.obtenerVidaActual = obtenerVidaActual;
        this.obtenerVidaMaxima = obtenerVidaMaxima;
        inicializado = true;

        if (rellenoTransform != null)
        {
            escalaXOriginal = rellenoTransform.localScale.x;
        }

        // Asegurar que la barra del jefe esté siempre visible desde el inicio
        EstablecerVisibilidad(true);
    }

    private void Start()
    {
        if (rellenoTransform != null && escalaXOriginal == 1f)
        {
            escalaXOriginal = rellenoTransform.localScale.x;
        }

        if (rellenoSpriteRenderer == null && rellenoTransform != null)
        {
            rellenoSpriteRenderer = rellenoTransform.GetComponent<SpriteRenderer>();
        }

        EstablecerVisibilidad(true);
    }

    private void Update()
    {
        if (!inicializado || obtenerVidaActual == null || obtenerVidaMaxima == null) return;

        float vidaActual = obtenerVidaActual();
        float vidaMaxima = obtenerVidaMaxima();

        if (vidaMaxima <= 0f) return;

        // Si la vida del jefe es 0 o menor (ha muerto), deshabilitar y ocultar la barra por completo
        if (vidaActual <= 0f)
        {
            EstablecerVisibilidad(false);
            return;
        }

        float porcentaje = Mathf.Clamp01(vidaActual / vidaMaxima);

        // Mientras el jefe continúe con vida, mantener la barra visible
        EstablecerVisibilidad(true);

        // Escalar el relleno proporcionalmente en el eje X
        if (rellenoTransform != null)
        {
            rellenoTransform.localScale = new Vector3(escalaXOriginal * porcentaje, rellenoTransform.localScale.y, rellenoTransform.localScale.z);
        }

        // Cambiar el sprite de relleno cuando la salud baje del 50%
        if (rellenoSpriteRenderer != null)
        {
            if (porcentaje <= 0.5f && spriteRellenoFase2 != null)
            {
                if (rellenoSpriteRenderer.sprite != spriteRellenoFase2)
                {
                    rellenoSpriteRenderer.sprite = spriteRellenoFase2;
                    Debug.Log("[BarraVidaJefe] Cambiado sprite de relleno a Fase 2 (Vida <= 50%).");
                }
            }
            else if (porcentaje > 0.5f && spriteRellenoFase1 != null)
            {
                if (rellenoSpriteRenderer.sprite != spriteRellenoFase1)
                {
                    rellenoSpriteRenderer.sprite = spriteRellenoFase1;
                }
            }
        }
    }

    private void EstablecerVisibilidad(bool visible)
    {
        if (fondo != null && fondo.activeSelf != visible)
        {
            fondo.SetActive(visible);
        }

        if (rellenoTransform != null && rellenoTransform.gameObject.activeSelf != visible)
        {
            rellenoTransform.gameObject.SetActive(visible);
        }
    }
}
