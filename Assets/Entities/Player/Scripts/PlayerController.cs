using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Multijugador")]
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("Jugador", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private TextMeshPro textoNombre;

    [Header("Referencias a Slots de Armas")]
    [Tooltip("Transforms de los puntos de anclaje de las armas en el cuerpo de Génesis (Slots 1 a 4).")]
    public Transform[] slotsArmas = new Transform[4];

    [Header("Configuración de Datos")]
    [Tooltip("Estadísticas del personaje (armadura, curación, velocidad, etc.).")]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Inventario de armas y habilidades de Génesis.")]
    public DatosInventario datosInventario;
    [Tooltip("Referencia a la barra de vida del jugador (asignada en inspector).")]
    public BarraVida barraVida;

    [Header("Configuración de Capas Visuales de Armas")]
    [Tooltip("Sorting Order para las armas situadas al frente (capa visible).")]
    public int capaFrente = 8;
    [Tooltip("Sorting Order para las armas situadas detrás (capa oculta por el cuerpo del robot).")]
    public int capaAtras = 5;

    [Header("Estadísticas en Tiempo de Juego")]
    public float vidaMaxima = 100f;
    public float vida;
    [Tooltip("Porcentaje de vida máxima regenerada por segundo.")]
    public float tasaRegeneracionBase = 5f;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        textoNombre = GetComponentInChildren<TextMeshPro>();

        if (IsOwner)
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Jugador");
            ActualizarTextoNombre(myName); // Actualizar localmente de inmediato
            SetPlayerNameServerRpc(myName); // Enviar al servidor para que los demás lo vean
        }

        playerName.OnValueChanged += (oldValue, newValue) => 
        {
            ActualizarTextoNombre(newValue.ToString());
        };
        
        ActualizarTextoNombre(playerName.Value.ToString());
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string newName)
    {
        playerName.Value = newName;
    }

    [ServerRpc]
    public void SetReadyStatusServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

    private void ActualizarTextoNombre(string nombre)
    {
        if (textoNombre != null)
        {
            textoNombre.text = nombre;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActualizarVisibilidadSegunEscena();
    }

    private void ActualizarVisibilidadSegunEscena()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool esEscenaJuego = (sceneName != "Menu" && sceneName != "Lobby");

        // Activar/desactivar renderers
        if (spriteRenderer != null) spriteRenderer.enabled = esEscenaJuego;
        
        // Ocultar armas instanciadas
        for (int i = 0; i < 4; i++)
        {
            if (armasInstanciadas[i] != null)
            {
                armasInstanciadas[i].SetActive(esEscenaJuego);
            }
        }

        // Ocultar texto de nombre
        if (textoNombre != null)
        {
            textoNombre.enabled = esEscenaJuego;
        }

        // Desactivar colisiones y físicas
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = esEscenaJuego;

        if (rb != null)
        {
            rb.simulated = esEscenaJuego;
            if (!esEscenaJuego)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void Start()
    {
        if (!inicializado)
        {
            InicializarValores();
        }

        InstanciarArmasEquipadas();
        ActualizarVisibilidadSegunEscena();

        // Inicializar la barra de vida del jugador si está asignada en el inspector
        if (barraVida != null)
        {
            barraVida.Inicializar(() => vida, () => vidaMaxima);
        
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
    public void RecargarArmasEquipadas()
    {
        // 1. Destruir cualquier arma previamente instanciada
        for (int i = 0; i < armasInstanciadas.Length; i++)
        {
            if (armasInstanciadas[i] != null)
            {
                Destroy(armasInstanciadas[i]);
                armasInstanciadas[i] = null;
            }
        }

        if (datosInventario == null) return;

        // 2. Instanciar según el ScriptableObject datosInventario actualizado
        for (int i = 0; i < 4; i++)
        {
            if (i < slotsArmas.Length && slotsArmas[i] != null && i < datosInventario.armasEquipadas.Length)
            {
                GameObject weaponPrefab = datosInventario.armasEquipadas[i];
                if (weaponPrefab != null)
                {
                    GameObject armaObj = Instantiate(weaponPrefab, slotsArmas[i].position, slotsArmas[i].rotation, slotsArmas[i]);
                    armasInstanciadas[i] = armaObj;

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

        ActualizarOrdenCapas(true);
    }

    private void InstanciarArmasEquipadas()
    {
        RecargarArmasEquipadas();
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
        if (!IsOwner) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Menu" || sceneName == "Lobby")
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

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
        if (!IsOwner) return;

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
        
        if (IsServer)
        {
            CargarDerrota();
        }
        else
        {
            NotificarMuerteServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotificarMuerteServerRpc()
    {
        CargarDerrota();
    }

    private void CargarDerrota()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.NotificarMuerteJugador();
        }
        else
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Derrota", LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene("Derrota");
            }
        }
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
