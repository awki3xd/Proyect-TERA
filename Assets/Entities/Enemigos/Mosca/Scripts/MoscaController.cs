using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class MoscaController : MonoBehaviour
{
    public enum EstadoMosca
    {
        PatrullandoCentro,
        PersiguiendoPlayer,
        AtacandoPlayer
    }

    [Header("Estadísticas de la Mosca")]
    [Tooltip("Vida inicial de la mosca.")]
    public float vida = 30f;
    [Tooltip("Daño por cada disparo de proyectil.")]
    public float daño = 8f;
    [Tooltip("Velocidad de vuelo en unidades/segundo.")]
    public float velocidadMovimiento = 3f;
    [Tooltip("Cadencia de disparo en segundos (tiempo de cooldown).")]
    public float velocidadAtaque = 1.5f;

    [Header("Referencias de Prefabs")]
    [Tooltip("Prefab del proyectil que disparará la mosca.")]
    public GameObject prefabProyectil;
    [Tooltip("Prefab del material o recurso que soltará al morir.")]
    public GameObject prefabMaterial;

    [Header("Configuración de Rangos de IA")]
    [Tooltip("Distancia a la que la mosca se detendrá y cambiará a modo disparo.")]
    public float distanciaDeteccion = 4f;
    [Tooltip("Rango máximo de disparo/desenganche. Si el jugador se aleja más allá de esta distancia, la mosca vuelve a perseguirlo.")]
    public float rangoDisparo = 5.5f;
    [Tooltip("Distancia hacia adelante desde el centro de la mosca donde se instanciará el disparo.")]
    public float offsetDistanciaDisparo = 0.5f;

    [Header("Estado Actual de IA")]
    public EstadoMosca estadoActual = EstadoMosca.PatrullandoCentro;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private Vector2 direccionPatrulla;
    private float cooldownDisparo;
    private bool inicializado = false;
    private bool estaMuerto = false;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        // Si no ha sido inicializada por el spawner, nos auto-inicializamos con multiplicadores por defecto
        if (!inicializado)
        {
            Inicializar(null, transform.position);
        }
    }

    /// <summary>
    /// Método de inicialización llamado por el spawner global al spawnear al enemigo.
    /// Aplica los multiplicadores porcentuales de DatosGlobalesEnemigos de forma dinámica.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales, Vector2 posicionInicial)
    {
        if (inicializado) return;
        inicializado = true;

        transform.position = posicionInicial;

        // Buscar al jugador dinámicamente en la escena
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Definir dirección de patrulla hacia el centro con una desviación aleatoria (igual que el escorpión)
        Vector2 dirHaciaCentro = ((Vector2)Vector2.zero - (Vector2)transform.position).normalized;
        float desviacion = Random.Range(-15f, 15f);
        direccionPatrulla = Quaternion.Euler(0f, 0f, desviacion) * dirHaciaCentro;

        // Calcular modificadores dinámicos del ScriptableObject de balance global
        float multVida = datosGlobales != null ? datosGlobales.vida / 100f : 1f;
        float multDaño = datosGlobales != null ? datosGlobales.daño / 100f : 1f;
        float multVelocidad = datosGlobales != null ? datosGlobales.velocidadMovimiento / 100f : 1f;
        float multVelocidadAtaque = datosGlobales != null ? datosGlobales.velocidadAtaque / 100f : 1f;

        // Sobrescribir variables de balance aplicando los modificadores
        vida = vida * multVida;
        daño = daño * multDaño;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;
        velocidadAtaque = velocidadAtaque * multVelocidadAtaque;

        cooldownDisparo = 0f;
        estadoActual = EstadoMosca.PatrullandoCentro;
    }

    private void Update()
    {
        if (estaMuerto) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        BuscarJugadorMasCercano();

        // Descontar el cooldown de disparo
        if (cooldownDisparo > 0f)
        {
            cooldownDisparo -= Time.deltaTime;
        }

        // Medir distancia al jugador activo más cercano
        float distAlPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;

        // Umbral estricto de desenganche de disparo (distancia de detención + 1.2 unidades)
        float limiteDesenganche = distanciaDeteccion + 1.2f;

        // Máquina de estados
        switch (estadoActual)
        {
            case EstadoMosca.PatrullandoCentro:
                // Si el jugador entra al rango de persecución amplio, lo perseguimos
                if (distAlPlayer <= distanciaDeteccion * 2.5f)
                {
                    estadoActual = EstadoMosca.PersiguiendoPlayer;
                }
                break;

            case EstadoMosca.PersiguiendoPlayer:
                // Si llegamos a la distancia de detención/parada, empezamos a disparar
                if (distAlPlayer <= distanciaDeteccion)
                {
                    estadoActual = EstadoMosca.AtacandoPlayer;
                }
                // Si el jugador se aleja demasiado, volvemos a patrullar al centro
                else if (distAlPlayer > distanciaDeteccion * 3f)
                {
                    estadoActual = EstadoMosca.PatrullandoCentro;
                }
                break;

            case EstadoMosca.AtacandoPlayer:
                // Si el jugador se aleja más allá del límite estricto de desenganche, VOLVER A PERSEGUIRLO inmediatamente
                if (distAlPlayer > limiteDesenganche)
                {
                    estadoActual = EstadoMosca.PersiguiendoPlayer;
                }
                break;
        }

        // Lógica de apuntar y disparar en estado de ataque
        if (estadoActual == EstadoMosca.AtacandoPlayer && playerTransform != null)
        {
            Vector2 dirHaciaPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            
            // Apuntar rotando el transform hacia el jugador
            float angulo = Mathf.Atan2(dirHaciaPlayer.y, dirHaciaPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);

            // Control de disparo
            if (cooldownDisparo <= 0f)
            {
                Disparar(dirHaciaPlayer);
                cooldownDisparo = velocidadAtaque;

                // Disparar animación de disparo
                if (animator != null)
                {
                    animator.SetTrigger("dispara");
                }
            }
        }

        // Actualizar parámetros visuales del Animator y el SpriteRenderer
        ActualizarVisuales();
    }

    private void FixedUpdate()
    {
        if (estaMuerto)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Lógica de movimiento por físicas
        switch (estadoActual)
        {
            case EstadoMosca.PatrullandoCentro:
                rb.linearVelocity = direccionPatrulla * velocidadMovimiento;
                break;

            case EstadoMosca.PersiguiendoPlayer:
                if (playerTransform != null)
                {
                    Vector2 dirHaciaPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dirHaciaPlayer * velocidadMovimiento;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
                break;

            case EstadoMosca.AtacandoPlayer:
                // Se detiene para disparar de forma estable
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void Disparar(Vector2 direccion)
    {
        if (prefabProyectil == null) return;

        // Calcular desviación aleatoria entre -20 y +20 grados
        float desviacion = Random.Range(-20f, 20f);
        Vector2 direccionDesviada = Quaternion.Euler(0f, 0f, desviacion) * direccion;

        Vector2 posDisparo = (Vector2)transform.position + direccionDesviada * offsetDistanciaDisparo;
        float angulo = Mathf.Atan2(direccionDesviada.y, direccionDesviada.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angulo);

        GameObject proyectil = Instantiate(prefabProyectil, posDisparo, rot);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            var netObj = proyectil.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }

        // Reproducir sonido de disparo enemigo
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.DisparoEnemigo);
        }

        // Inicializar la bala de la mosca usando su controlador independiente
        ValaMoscaController valaScript = proyectil.GetComponent<ValaMoscaController>();
        if (valaScript != null)
        {
            valaScript.Inicializar(daño, rangoDisparo);
        }
        else
        {
            // Fallback por compatibilidad
            EntidadDaño dañoScript = proyectil.GetComponent<EntidadDaño>();
            if (dañoScript != null)
            {
                dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, true);
            }
        }
    }

    private void BuscarJugadorMasCercano()
    {
        PlayerController[] jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Transform masCercano = null;
        float minDist = float.MaxValue;

        foreach (var p in jugadores)
        {
            if (p != null && p.gameObject.activeSelf)
            {
                float dist = Vector2.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    masCercano = p.transform;
                }
            }
        }
        playerTransform = masCercano;
    }

    /// <summary>
    /// Actualiza en cada frame las variables del Animator y maneja el flipX del sprite de forma inteligente.
    /// </summary>
    private void ActualizarVisuales()
    {
        if (animator == null || spriteRenderer == null) return;

        // Determinar dirección de referencia (movimiento o apuntado)
        Vector2 dirReferencia = Vector2.right;
        bool isMoving = false;

        if (estadoActual == EstadoMosca.AtacandoPlayer && playerTransform != null)
        {
            dirReferencia = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            isMoving = false;
        }
        else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            dirReferencia = rb.linearVelocity.normalized;
            isMoving = true;
        }

        // Clasificar dirección
        float verticalVal = 0f;
        if (Mathf.Abs(dirReferencia.y) > Mathf.Abs(dirReferencia.x))
        {
            // Dirección predominantemente vertical (1 para arriba, -1 para abajo)
            verticalVal = dirReferencia.y > 0f ? 1f : -1f;
        }
        else
        {
            // Dirección predominantemente horizontal (0 en vertical)
            verticalVal = 0f;
            // Espejar horizontalmente si apunta a la izquierda
            spriteRenderer.flipX = dirReferencia.x < 0f;
        }

        // Enviar parámetros al Animator
        animator.SetBool("caminando", isMoving);
        animator.SetFloat("vertical", verticalVal);
    }

    /// <summary>
    /// Recibe daño del jugador y gestiona la muerte secuencial.
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
        // 1. Desactivar colisiones físicas de inmediato
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Reproducir sonido de muerte de enemigo
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.MuerteEnemigo);
        }

        // Quitar tag para que el jugador deje de apuntarle
        gameObject.tag = "Untagged";
        velocidadMovimiento = 0f;

        // 2. Lanzar animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("muere");
        }

        // 3. Esperar 1 segundo a que se complete la disolución/efecto
        yield return new WaitForSeconds(1f);

        // 4. Instanciar Bridgmanita / Recurso
        if (prefabMaterial != null)
        {
            GameObject matObj = Instantiate(prefabMaterial, transform.position, Quaternion.identity);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                var netObjMat = matObj.GetComponent<NetworkObject>();
                if (netObjMat != null)
                {
                    netObjMat.Spawn();
                }
            }
        }

        // 5. Destruir el objeto
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
}
