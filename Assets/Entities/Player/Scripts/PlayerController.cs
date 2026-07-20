using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Referencias a Slots de Armas")]
    [Tooltip("Transforms de los puntos de anclaje de las armas en el cuerpo de Génesis (Slots 1 a 4).")]
    public Transform[] slotsArmas = new Transform[4];

    [Header("Configuración de Datos")]
    [Tooltip("Estadísticas del personaje (armadura, curación, velocidad, etc.).")]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Inventario de armas y habilidades de Génesis.")]
    public DatosInventario datosInventario;

    [Header("Configuración de Capas Visuales de Armas")]
    [Tooltip("Sorting Order para las armas situadas al frente (capa visible).")]
    public int capaFrente = 8;
    [Tooltip("Sorting Order para las armas situadas detrás (capa oculta por el cuerpo del robot).")]
    public int capaAtras = 5;

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

    public Animator Animaciones;

    private Vector2 entradaMovimiento;
    private Rigidbody2D rb;
    private int nodosEnContacto = 0;
    private bool inicializado = false;

    // Referencias a instancias y renderizado
    private GameObject[] armasInstanciadas = new GameObject[4];
    private bool mirandoDerecha = true;
    private SpriteRenderer spriteRenderer;

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
        
        // Obtener el SpriteRenderer principal de Génesis
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (!inicializado)
        {
            InicializarValores();
        }

        InstanciarArmasEquipadas();
    }

    /// <summary>
    /// Precalcula todos los modificadores de estadísticas en el Start de la escena,
    /// evitando cálculos en bucles Update, FixedUpdate o al recibir impactos.
    /// </summary>
    private void InicializarValores()
    {
        inicializado = true;
        vida = vidaMaxima;

        // Resetear el estado de reparación en el inventario
        if (datosInventario != null)
        {
            datosInventario.estaReparando = false;
        }

        // 1. Mitigación de Daño (Armadura)
        float armaduraVal = datosPersonaje != null ? datosPersonaje.armadura : 100f;
        float armaduraMinima = Mathf.Max(1f, armaduraVal);
        factorMitigacion = armaduraMinima / 100f;

        // 2. Velocidad de Movimiento
        float multVelocidad = datosPersonaje != null ? datosPersonaje.velocidadMovimiento / 100f : 1f;
        velocidadReal = 5f * multVelocidad; // 3f es la velocidad física base

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

    /// <summary>
    /// Instancia dinámicamente las armas del inventario en los puntos de anclaje (slotsArmas) del jugador.
    /// </summary>
    private void InstanciarArmasEquipadas()
    {
        if (datosInventario == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (i < slotsArmas.Length && slotsArmas[i] != null && i < datosInventario.armasEquipadas.Length)
            {
                GameObject weaponPrefab = datosInventario.armasEquipadas[i];
                if (weaponPrefab != null)
                {
                    // Instanciamos el arma como hijo del slot correspondiente
                    GameObject armaObj = Instantiate(weaponPrefab, slotsArmas[i].position, slotsArmas[i].rotation, slotsArmas[i]);
                    armasInstanciadas[i] = armaObj;

                    // Inicializar el controlador del arma directamente (a distancia, sable o motosierra)
                    ArmaController armaScript = armaObj.GetComponent<ArmaController>();
                    if (armaScript != null)
                    {
                        armaScript.datosPersonaje = datosPersonaje;
                    }
                    else
                    {
                        SableController sableScript = armaObj.GetComponent<SableController>();
                        if (sableScript != null)
                        {
                            sableScript.datosPersonaje = datosPersonaje;
                        }
                        else
                        {
                            MotosierraController motosierraScript = armaObj.GetComponent<MotosierraController>();
                            if (motosierraScript != null)
                            {
                                motosierraScript.datosPersonaje = datosPersonaje;
                            }
                        }
                    }
                }
            }
        }

        // Establecer el orden de renderizado inicial (derecha)
        ActualizarOrdenCapas(true);
    }

    /// <summary>
    /// Actualiza el Sorting Order de los SpriteRenderers de las armas en caliente según la dirección de vista del jugador.
    /// </summary>
    private void ActualizarOrdenCapas(bool derecha)
    {
        for (int i = 0; i < 4; i++)
        {
            if (armasInstanciadas[i] != null)
            {
                SpriteRenderer sr = armasInstanciadas[i].GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    // Si miramos a la derecha, slots 1 y 2 están al frente (capaFrente), slots 3 y 4 detrás (capaAtras)
                    // Si miramos a la izquierda, slots 1 y 2 están detrás (capaAtras), slots 3 y 4 al frente (capaFrente)
                    bool esFrente = (derecha && (i == 0 || i == 1)) || (!derecha && (i == 2 || i == 3));
                    sr.sortingOrder = esFrente ? capaFrente : capaAtras;
                }
            }
        }
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

        // 3. Control de volteo (Flip) visual y reordenamiento de capas según dirección del movimiento
        if (inputX > 0f && !mirandoDerecha)
        {
            mirandoDerecha = true;
            if (spriteRenderer != null) spriteRenderer.flipX = false;
            ActualizarOrdenCapas(true);
        }
        else if (inputX < 0f && mirandoDerecha)
        {
            mirandoDerecha = false;
            if (spriteRenderer != null) spriteRenderer.flipX = true;
            ActualizarOrdenCapas(false);
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
        Animaciones.SetTrigger("daño");
        // Mitigar el daño usando el factor ya calculado al inicio
        float dañoFinal = cantidad / factorMitigacion;

        vida = Mathf.Max(0f, vida - dañoFinal);

        // Crear número de daño flotante en color rojo
        TextoDañoFlotante.Crear(transform.position, dañoFinal, Color.red);

        // Reproducir sonido de daño al personaje
        if (SoundManager.Instance != null && vida > 0f)
        {
            SoundManager.Instance.PlaySFX(SoundID.DañoPersonaje);
        }
        
        if (vida <= 0f)
        {
            Morir();
        }
    }

    private void Morir()
    {
        Debug.Log("Génesis ha sido destruido. Partida Terminada.");
        // Cargar escena de Derrota (Build Index 2)
        SceneManager.LoadScene(2);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Nodo"))
        {
            nodosEnContacto++;
            estaReparando = true;
            if (datosInventario != null)
            {
                datosInventario.estaReparando = true;
            }

            // Desactivar armas secundarias (ranuras 3 y 4, índices 2 y 3)
            SetPuedeDispararArmasSecundarias(false);
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
                if (datosInventario != null)
                {
                    datosInventario.estaReparando = false;
                }

                // Reactivar armas secundarias (ranuras 3 y 4, índices 2 y 3)
                SetPuedeDispararArmasSecundarias(true);
            }
        }
    }

    /// <summary>
    /// Cambia el estado de disparo de las armas secundarias equipadas en las ranuras 3 y 4.
    /// </summary>
    private void SetPuedeDispararArmasSecundarias(bool estado)
    {
        // Las ranuras 3 y 4 corresponden a los índices 2 y 3 del array de armas
        for (int i = 2; i <= 3; i++)
        {
            if (i < armasInstanciadas.Length && armasInstanciadas[i] != null)
            {
                // Intentar desactivar arma a distancia
                ArmaController arma = armasInstanciadas[i].GetComponent<ArmaController>();
                if (arma != null)
                {
                    arma.puedeDisparar = estado;
                }
                else
                {
                    // Intentar desactivar el sable
                    SableController sable = armasInstanciadas[i].GetComponent<SableController>();
                    if (sable != null)
                    {
                        sable.puedeDisparar = estado;
                    }
                    else
                    {
                        // Intentar desactivar la motosierra
                        MotosierraController motosierra = armasInstanciadas[i].GetComponent<MotosierraController>();
                        if (motosierra != null)
                        {
                            motosierra.puedeDisparar = estado;
                        }
                    }
                }
            }
        }
    }
}
