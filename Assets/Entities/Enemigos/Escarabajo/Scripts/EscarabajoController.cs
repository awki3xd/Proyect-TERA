using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class EscarabajoController : MonoBehaviour
{
    public enum EstadoEscarabajo
    {
        PatrullandoCentro,
        YendoAlNodo,
        EscudoActivo
    }

    [Header("Estadísticas del Escarabajo")]
    [Tooltip("Vida del escarabajo (muy resistente, funciona como un escudo).")]
    public float vida = 150f;
    [Tooltip("Velocidad de movimiento del escarabajo (un poco más rápido que el escorpión).")]
    public float velocidadMovimiento = 2f;

    [Header("Configuración del Escudo")]
    [Tooltip("Distancia desde el nodo en la que se interpondrá hacia el jugador.")]
    public float distanciaDelNodo = 1.3f;

    [Header("Referencias de Prefabs y Recompensas")]
    [Tooltip("Prefab del recurso que soltará al morir.")]
    public GameObject prefabMaterial;
    [Tooltip("Prefab del cofre de armas élite.")]
    public GameObject prefabCofre;
    [Tooltip("Referencia al ScriptableObject DatosNivel para calcular probabilidad de drop.")]
    public DatosNivel datosNivel;

    [Header("Estado de IA")]
    public EstadoEscarabajo estadoActual = EstadoEscarabajo.PatrullandoCentro;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector3 escalaOriginal;

    private NodoEstandar nodoObjetivo;
    private PlayerController jugadorObjetivo;

    private bool inicializado = false;
    private bool estaMuerto = false;
    private bool enModoEscudo = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (transform.localScale != Vector3.zero)
        {
            escalaOriginal = transform.localScale;
        }
        else
        {
            escalaOriginal = Vector3.one;
        }
    }

    private void Start()
    {
        if (datosNivel == null)
        {
            datosNivel = Resources.Load<DatosNivel>("DatosNivel");
        }

        if (!inicializado)
        {
            Inicializar(null, transform.position);
        }
    }

    /// <summary>
    /// Inicializa y escala las estadísticas del escarabajo en base al nivel.
    /// Al no atacar, no escala daño ni velocidad de ataque.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales, Vector2 posicionInicial)
    {
        if (inicializado) return;
        inicializado = true;

        transform.position = posicionInicial;

        float multVida = datosGlobales != null ? datosGlobales.vida / 100f : 1f;
        float multVelocidad = datosGlobales != null ? datosGlobales.velocidadMovimiento / 100f : 1f;

        vida = vida * multVida;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;

        estadoActual = EstadoEscarabajo.PatrullandoCentro;
    }

    private void Update()
    {
        if (estaMuerto) return;

        // 1. Monitoreo constante del nodo activo más cercano
        ActualizarNodoObjetivo();

        // 2. Control de estado
        if (nodoObjetivo == null)
        {
            // Si no hay nodos activos, patrullar hacia el centro
            estadoActual = EstadoEscarabajo.PatrullandoCentro;
            DesactivarModoEscudo();
        }
        else
        {
            float distAlNodo = Vector2.Distance(transform.position, nodoObjetivo.transform.position);

            if (distAlNodo > distanciaDelNodo + 1.5f)
            {
                // Si está lejos, viajar hacia el nodo
                estadoActual = EstadoEscarabajo.YendoAlNodo;
                DesactivarModoEscudo();
            }
            else
            {
                // Si está cerca del nodo, entrar en modo Escudo Interpuesto
                estadoActual = EstadoEscarabajo.EscudoActivo;
                ActivarModoEscudo();
            }
        }

        // 3. Orientar visualmente el sprite del escarabajo (flipX)
        ActualizarOrientacionVisual();
    }

    private void LateUpdate()
    {
        if (estaMuerto) return;

        // Asegurar que la escala 2x se mantenga activa en LateUpdate para evitar que el Animator o clips de animación la sobrescriban
        if (enModoEscudo)
        {
            Vector3 targetScale = (escalaOriginal != Vector3.zero ? escalaOriginal : Vector3.one) * 1.5f;
            transform.localScale = targetScale;

            if (spriteRenderer != null && spriteRenderer.gameObject != this.gameObject)
            {
                spriteRenderer.transform.localScale = Vector3.one * 1.5f;
            }
        }
    }

    private void FixedUpdate()
    {
        if (estaMuerto)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        switch (estadoActual)
        {
            case EstadoEscarabajo.PatrullandoCentro:
                Vector2 dirCentro = ((Vector2)Vector2.zero - (Vector2)transform.position).normalized;
                rb.linearVelocity = dirCentro * velocidadMovimiento;
                break;

            case EstadoEscarabajo.YendoAlNodo:
                if (nodoObjetivo != null)
                {
                    Vector2 dirNodo = ((Vector2)nodoObjetivo.transform.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dirNodo * velocidadMovimiento;
                }
                break;

            case EstadoEscarabajo.EscudoActivo:
                if (nodoObjetivo != null)
                {
                    BuscarJugadorMasCercano();

                    if (jugadorObjetivo != null)
                    {
                        // Calcular la posición exacta de interposición (entre el Nodo y el Jugador)
                        Vector2 posNodo = nodoObjetivo.transform.position;
                        Vector2 posPlayer = jugadorObjetivo.transform.position;
                        Vector2 dirAlPlayer = (posPlayer - posNodo).normalized;
                        Vector2 posicionDestinoEscudo = posNodo + dirAlPlayer * distanciaDelNodo;

                        // Desplazarse hacia esa posición de escudo
                        float distADestino = Vector2.Distance(transform.position, posicionDestinoEscudo);
                        if (distADestino > 0.1f)
                        {
                            Vector2 dirEscudo = (posicionDestinoEscudo - (Vector2)transform.position).normalized;
                            rb.linearVelocity = dirEscudo * velocidadMovimiento;
                        }
                        else
                        {
                            rb.linearVelocity = Vector2.zero;
                        }
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
                break;
        }
    }

    private void ActualizarNodoObjetivo()
    {
        NodoEstandar[] nodos = FindObjectsByType<NodoEstandar>(FindObjectsSortMode.None);
        NodoEstandar masCercano = null;
        float minDist = float.MaxValue;

        foreach (var n in nodos)
        {
            if (n != null && !n.EstaRoto())
            {
                float dist = Vector2.Distance(transform.position, n.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    masCercano = n;
                }
            }
        }
        nodoObjetivo = masCercano;
    }

    private void BuscarJugadorMasCercano()
    {
        PlayerController[] jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController masCercano = null;
        float minDist = float.MaxValue;

        foreach (var p in jugadores)
        {
            if (p != null)
            {
                float dist = Vector2.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    masCercano = p;
                }
            }
        }
        jugadorObjetivo = masCercano;
    }

    private void ActivarModoEscudo()
    {
        if (enModoEscudo) return;
        enModoEscudo = true;

        // Agrandar escala a 2x en todo el bicho para tapar más balas
        Vector3 targetScale = (escalaOriginal != Vector3.zero ? escalaOriginal : Vector3.one) * 2f;
        transform.localScale = targetScale;

        if (spriteRenderer != null && spriteRenderer.gameObject != this.gameObject)
        {
            spriteRenderer.transform.localScale = Vector3.one * 2f;
        }

        // Activar animación de escudo
        if (animator != null)
        {
            animator.SetBool("escudo", true);
        }
    }

    private void DesactivarModoEscudo()
    {
        if (!enModoEscudo) return;
        enModoEscudo = false;

        // Restaurar escala a 1x
        Vector3 targetScale = (escalaOriginal != Vector3.zero ? escalaOriginal : Vector3.one);
        transform.localScale = targetScale;

        if (spriteRenderer != null && spriteRenderer.gameObject != this.gameObject)
        {
            spriteRenderer.transform.localScale = Vector3.one;
        }

        // Desactivar animación de escudo (vuelve a caminar normal)
        if (animator != null)
        {
            animator.SetBool("escudo", false);
        }
    }

    private void ActualizarOrientacionVisual()
    {
        if (spriteRenderer == null) return;

        Vector2 velocidadActual = rb.linearVelocity;

        // Si está en escudo, orientar la vista según la posición del jugador objetivo
        if (estadoActual == EstadoEscarabajo.EscudoActivo && jugadorObjetivo != null)
        {
            Vector2 dirAlPlayer = (jugadorObjetivo.transform.position - transform.position).normalized;
            spriteRenderer.flipX = dirAlPlayer.x < 0f;
        }
        // Si se está moviendo, orientar la vista según el sentido físico de desplazamiento
        else if (velocidadActual.sqrMagnitude > 0.05f)
        {
            spriteRenderer.flipX = velocidadActual.x < 0f;
        }
    }

    /// <summary>
    /// Recibe daño del jugador y gestiona la muerte.
    /// </summary>
    public void RecibirDaño(float cantidad)
    {
        if (estaMuerto) return;

        vida = Mathf.Max(0f, vida - cantidad);

        // Crear número de daño flotante en color blanco
        TextoDañoFlotante.Crear(transform.position, cantidad, Color.white);

        if (vida <= 0f)
        {
            estaMuerto = true;
            StartCoroutine(MuerteCo());
        }
    }

    private IEnumerator MuerteCo()
    {
        // Desactivar colisiones y modo escudo
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        gameObject.tag = "Untagged";
        DesactivarModoEscudo();

        // Sonido de muerte
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.MuerteEnemigo);
        }

        // Animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("muere");
        }

        yield return new WaitForSeconds(1f);

        // Spawnear 3 materiales dispersos (+/- 0.5f unidades)
        if (prefabMaterial != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
                GameObject matObj = Instantiate(prefabMaterial, (Vector2)transform.position + offset, Quaternion.identity);
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    var netObjMat = matObj.GetComponent<NetworkObject>();
                    if (netObjMat != null) netObjMat.Spawn();
                }
            }
        }

        // Evaluar probabilidad de drop de cofre de armas según el nivel
        int nivelActual = datosNivel != null ? datosNivel.numeroNivel : 1;
        float probCofre = CalcularProbabilidadCofre(nivelActual);

        if (prefabCofre != null && Random.value <= probCofre)
        {
            GameObject cofreObj = Instantiate(prefabCofre, transform.position, Quaternion.identity);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                var netObjCofre = cofreObj.GetComponent<NetworkObject>();
                if (netObjCofre != null) netObjCofre.Spawn();
            }
            Debug.Log($"[Escarabajo Muerte] ¡Cofre generado en Nivel {nivelActual}! (Probabilidad: {probCofre * 100:F0}%).");
        }

        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Despawn(true);
            }
            yield break;
        }

        Destroy(gameObject);
    }

    private float CalcularProbabilidadCofre(int nivel)
    {
        if (nivel <= 2) return 1.0f;       // 100% en Niveles 1 y 2
        if (nivel <= 5) return 0.75f;      // 75% en Niveles 3 a 5
        if (nivel <= 10) return 0.65f;     // 65% en Niveles 6 a 10
        if (nivel <= 15) return 0.50f;     // 50% en Niveles 11 a 15
        if (nivel <= 20) return 0.20f;     // 20% en Niveles 16 a 20
        return 0.10f;                      // 10% en Nivel > 20
    }
}
