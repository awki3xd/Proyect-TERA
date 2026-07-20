using UnityEngine;

/// <summary>
/// Estructura de datos empaquetada para inicializar el proyectil.
/// </summary>
public struct DatosProyectil
{
    public float daño;
    public float rango;
    public EntidadDaño.OrigenDaño origen;
    [Tooltip("Identificador del arma o emisor del disparo para seleccionar efectos visuales.")]
    public string arma;
}

[RequireComponent(typeof(EntidadDaño))]
public class ProyectilController : MonoBehaviour, IProyectil
{
    [Header("Configuración de Vuelo")]
    [Tooltip("Velocidad de desplazamiento del proyectil.")]
    public float velocidad = 4f;

    [Header("Personalización Visual")]
    [Tooltip("Referencia al SpriteRenderer del proyectil para cambiar su diseño visual.")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Arreglo de sprites disponibles para las balas. Índice 0: Pistola, 1: Rifle, 2: Enemigo, 3: Metralleta.")]
    public Sprite[] spritesBalas;

    private Vector2 posicionInicial;
    private float rangoMaximo;
    private bool inicializado = false;

    /// <summary>
    /// Inicializa los parámetros de vuelo, daño y aspecto visual del proyectil.
    /// Recibe la estructura de datos empaquetada desde el arma o emisor.
    /// </summary>
    public void Inicializar(DatosProyectil datos)
    {
        rangoMaximo = datos.rango;
        posicionInicial = transform.position;
        inicializado = true;

        // 1. Configurar el sprite dinámicamente según el arma/origen
        ConfigurarAspectoVisual(datos.arma);

        // 2. Configurar el componente EntidadDaño adjunto en este prefab
        EntidadDaño dañoScript = GetComponent<EntidadDaño>();
        if (dañoScript != null)
        {
            dañoScript.Inicializar(datos.daño, datos.origen, true);
        }
    }

    /// <summary>
    /// Selecciona el sprite adecuado basado en el nombre del arma emisora.
    /// </summary>
    private void ConfigurarAspectoVisual(string nombreArma)
    {
        if (spriteRenderer == null || spritesBalas == null || spritesBalas.Length == 0)
        {
            return;
        }

        string armaClave = string.IsNullOrEmpty(nombreArma) ? "enemigo" : nombreArma.ToLower();

        switch (armaClave)
        {
            case "pistola":
                if (spritesBalas.Length > 0 && spritesBalas[0] != null)
                {
                    spriteRenderer.sprite = spritesBalas[0];
                }
                break;
            case "rifle":
                if (spritesBalas.Length > 1 && spritesBalas[1] != null)
                {
                    spriteRenderer.sprite = spritesBalas[1];
                }
                break;
            case "enemigo":
                // Sprite por defecto de enemigo
                if (spritesBalas.Length > 2 && spritesBalas[2] != null)
                {
                    spriteRenderer.sprite = spritesBalas[2];
                    velocidad = 2f;
                }
                break;
            case "metralleta":
                if (spritesBalas.Length > 3 && spritesBalas[3] != null)
                {
                    spriteRenderer.sprite = spritesBalas[3];
                }
                break;
            default:
                // Comportamiento genérico: si se pasa un tipo de enemigo específico (ej: "escorpion"),
                // usamos el sprite por defecto de enemigo del índice 2 si existe.
                if (spritesBalas.Length > 2 && spritesBalas[2] != null)
                {
                    spriteRenderer.sprite = spritesBalas[2];
                }
                break;
        }
    }

    private void Start()
    {
        // En caso de que no haya sido inicializado externamente (ej: colocación directa en escena para testing)
        if (!inicializado)
        {
            posicionInicial = transform.position;
            rangoMaximo = 10f;
        }
    }

    private void Update()
    {
        // Movimiento rectilíneo hacia adelante en base a su dirección local de apuntado
        transform.Translate(Vector2.right * velocidad * Time.deltaTime, Space.Self);

        // Control de autodestrucción si supera el rango de alcance
        if (Vector2.Distance(transform.position, posicionInicial) >= rangoMaximo)
        {
            Destroy(gameObject);
        }
    }
}
