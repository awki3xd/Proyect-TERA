using UnityEngine;
using Unity.Netcode;

public class ValaMoscaController : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [Tooltip("Velocidad de movimiento del proyectil. Las balas enemigas deben ser más lentas para que el jugador pueda esquivarlas (ej: 2 o 3).")]
    public float velocidad = 2.5f;

    private Vector2 posicionInicial;
    private float rangoMaximo = 8f;
    private bool inicializado = false;

    /// <summary>
    /// Inicializa la bala de la mosca con el daño y el rango correspondientes.
    /// También inicializa el componente EntidadDaño para aplicar daño al jugador/nodos.
    /// </summary>
    public void Inicializar(float daño, float rango, bool destruir = true)
    {
        rangoMaximo = rango;
        posicionInicial = transform.position;
        inicializado = true;

        // Configurar el daño y origen de colisión
        EntidadDaño dañoScript = GetComponent<EntidadDaño>();
        if (dañoScript == null)
        {
            dañoScript = GetComponentInChildren<EntidadDaño>();
        }

        if (dañoScript != null)
        {
            dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, destruir);
        }
    }

    private void Start()
    {
        if (!inicializado)
        {
            posicionInicial = transform.position;
        }
    }

    private void Update()
    {
        // Mueve la bala hacia adelante en su eje local derecho
        transform.Translate(Vector2.right * velocidad * Time.deltaTime, Space.Self);

        // Control de autodestrucción si supera el rango de alcance máximo
        if (Vector2.Distance(transform.position, posicionInicial) >= rangoMaximo)
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    netObj.Despawn(true);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
