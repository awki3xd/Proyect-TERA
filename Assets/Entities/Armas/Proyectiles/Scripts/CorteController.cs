using UnityEngine;

[RequireComponent(typeof(EntidadDaño))]
public class CorteController : MonoBehaviour, IProyectil
{
    [Header("Configuración de Desplazamiento de Corte")]
    [Tooltip("Velocidad de avance rápido del barrido de la espada.")]
    public float velocidad = 2f;

    private Vector2 posicionInicial;
    private float rangoMaximo;
    private bool inicializado = false;

    /// <summary>
    /// Inicializa los parámetros de rango y de daño del corte de espada.
    /// Recibe la estructura de datos empaquetada desde el ArmaController.
    /// </summary>
    public void Inicializar(DatosProyectil datos)
    {
        rangoMaximo = datos.rango;
        posicionInicial = transform.position;
        inicializado = true;

        // Configurar el componente EntidadDaño adjunto en este prefab
        EntidadDaño dañoScript = GetComponent<EntidadDaño>();
        if (dañoScript != null)
        {
            // Inicializamos el daño y origen
            dañoScript.Inicializar(datos.daño, datos.origen, false);
            
            // Recordatorio de diseño: Para el prefab del sable/espada, la casilla 
            // "destruirAlImpactar" del script EntidadDaño debe estar en FALSE en el Inspector
            // para permitir el barrido de área (dañar a múltiples enemigos).
        }
    }

    private void Start()
    {
        // En caso de que no haya sido inicializado externamente (ej: testing en escena)
        if (!inicializado)
        {
            posicionInicial = transform.position;
            rangoMaximo = 1.8f;
        }
    }

    private void Update()
    {
        // El barrido se desplaza rápidamente hacia adelante en la dirección apuntada
        transform.Translate(Vector2.right * velocidad * Time.deltaTime, Space.Self);

        // Se destruye al alcanzar la distancia máxima de alcance de la espada (rango)
        if (Vector2.Distance(transform.position, posicionInicial) >= rangoMaximo)
        {
            Destroy(gameObject);
        }
    }
}
