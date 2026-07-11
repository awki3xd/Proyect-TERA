using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Estadísticas del personaje (armadura, curación, velocidad, etc.).")]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Inventario de armas y habilidades de Génesis.")]
    public DatosInventario datosInventario;

    [Header("Estadísticas en Tiempo de Juego")]
    public float vidaMaxima = 100f;
    public float vida;
    [Tooltip("Porcentaje de vida máxima regenerada por segundo.")]
    public float tasaRegeneracionBase = 2f;
    [Tooltip("Cantidad de vida que repara a los nodos por segundo.")]
    public float tasaReparacionBase = 10f;

    [Header("Estado de Interacción")]
    [Tooltip("Indica si el jugador está reparando algún nodo (desactiva ranuras de armas 3 y 4).")]
    public bool estaReparando = false;

    private Vector2 entradaMovimiento;
    private Rigidbody2D rb;
    private int nodosEnContacto = 0;
    private bool inicializado = false;

    // Estadísticas calculadas en el Start (permanecen fijas durante la partida del nivel)
    private float factorMitigacion;
    private float velocidadReal;
    private float tasaRegeneracionReal;
    private float tasaReparacionReal;
    private float dañoReal;
    private float rangoAtaqueReal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Garantizamos que las físicas de colisión 2D no roten a Génesis
        rb.freezeRotation = true;
    }

    private void Start()
    {
        if (!inicializado)
        {
            InicializarValores();
        }
    }

    /// <summary>
    /// Precalcula todos los modificadores de estadísticas en el Start de la escena,
    /// evitando cálculos en bucles Update, FixedUpdate o al recibir impactos.
    /// </summary>
    private void InicializarValores()
    {
        inicializado = true;
        vida = vidaMaxima;

        // 1. Mitigación de Daño (Armadura)
        float armaduraVal = datosPersonaje != null ? datosPersonaje.armadura : 100f;
        float armaduraMinima = Mathf.Max(1f, armaduraVal);
        factorMitigacion = armaduraMinima / 100f;

        // 2. Velocidad de Movimiento
        float multVelocidad = datosPersonaje != null ? datosPersonaje.velocidadMovimiento / 100f : 1f;
        velocidadReal = 5f * multVelocidad; // 5f es la velocidad física base

        // 3. Tasa de Regeneración y Reparación
        float factorCuracion = datosPersonaje != null ? datosPersonaje.curacion / 100f : 1f;
        tasaRegeneracionReal = tasaRegeneracionBase * factorCuracion;
        tasaReparacionReal = tasaReparacionBase * factorCuracion;

        // 4. Estadísticas de Combate Auxiliares (Daño y Rango)
        float multDaño = datosPersonaje != null ? datosPersonaje.daño / 100f : 1f;
        dañoReal = 10f * multDaño; // 10f daño base

        float multRango = datosPersonaje != null ? datosPersonaje.rangoAtaque / 100f : 1f;
        rangoAtaqueReal = 1.2f * multRango; // 1.2f rango base

        StartCoroutine(RegeneracionPasivaCo());
    }

    private void Update()
    {
        // 1. Capturar entradas de movimiento WASD o flechas
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        entradaMovimiento = new Vector2(inputX, inputY);

        // 2. Normalizar el vector de movimiento para evitar velocidad extra en diagonal
        if (entradaMovimiento.sqrMagnitude > 1f)
        {
            entradaMovimiento.Normalize();
        }
    }

    private void FixedUpdate()
    {
        // Mover físicamente utilizando la velocidad real precalculada
        rb.linearVelocity = entradaMovimiento * velocidadReal;
    }

    /// <summary>
    /// Corrutina de regeneración de vida pasiva utilizando la tasa calculada al inicio.
    /// </summary>
    private IEnumerator RegeneracionPasivaCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (vida < vidaMaxima)
            {
                vida = Mathf.Min(vidaMaxima, vida + tasaRegeneracionReal);
            }
        }
    }

    /// <summary>
    /// Recibe daño mitigado por el factor de mitigación precalculado en el Start.
    /// </summary>
    public void RecibirDaño(float cantidad)
    {
        // Mitigar el daño usando el factor ya calculado al inicio
        float dañoFinal = cantidad / factorMitigacion;

        vida = Mathf.Max(0f, vida - dañoFinal);
        
        if (vida <= 0f)
        {
            Morir();
        }
    }

    private void Morir()
    {
        Debug.Log("Génesis ha sido destruido. Partida Terminada.");
        // TODO: Invocar pantalla de Game Over o reiniciar escena
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Nodo"))
        {
            nodosEnContacto++;
            estaReparando = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Nodo"))
        {
            NodoEstandar nodo = other.GetComponent<NodoEstandar>();
            if (nodo != null && !nodo.EstaRoto())
            {
                // Cura el nodo según la tasa de reparación real precalculada en el Start
                nodo.Curar(tasaReparacionReal * Time.deltaTime);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Nodo"))
        {
            nodosEnContacto = Mathf.Max(0, nodosEnContacto - 1);
            if (nodosEnContacto == 0)
            {
                estaReparando = false;
            }
        }
    }
}
