using System.Collections;
using UnityEngine;

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
    [Tooltip("Rango máximo del proyectil/disparo. Debe ser mayor a la distancia de detección (ej: 6) para evitar bucles infinitos de movimiento.")]
    public float rangoDisparo = 6f;
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
        float multRango = datosGlobales != null ? datosGlobales.rangoAtaque / 100f : 1f;

        // Sobrescribir variables de balance aplicando los modificadores
        vida = vida * multVida;
        daño = daño * multDaño;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;
        velocidadAtaque = velocidadAtaque * multVelocidadAtaque;
        distanciaDeteccion = distanciaDeteccion * multRango;
        rangoDisparo = rangoDisparo * multRango;

        cooldownDisparo = 0f;
        estadoActual = EstadoMosca.PatrullandoCentro;
    }

    private void Update()
    {
        if (estaMuerto) return;

        // Descontar el cooldown de disparo
        if (cooldownDisparo > 0f)
        {
            cooldownDisparo -= Time.deltaTime;
        }

        // Medir distancia al jugador
        float distAlPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;

        // Máquina de estados
        switch (estadoActual)
        {
            case EstadoMosca.PatrullandoCentro:
                // Si el jugador entra al doble de la distancia de parada (rango de persecución amplio), lo perseguimos
                if (distAlPlayer <= distanciaDeteccion * 2f)
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
                // Si el jugador se aleja demasiado (fuera del rango de persecución), volvemos a patrullar al centro
                else if (distAlPlayer > distanciaDeteccion * 2f)
                {
                    estadoActual = EstadoMosca.PatrullandoCentro;
                }
                break;

            case EstadoMosca.AtacandoPlayer:
                // Si el jugador se aleja más allá de nuestro rango máximo de disparo, volvemos a perseguirlo
                if (distAlPlayer > rangoDisparo)
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

        Vector2 posDisparo = (Vector2)transform.position + direccion * offsetDistanciaDisparo;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angulo);

        GameObject proyectil = Instantiate(prefabProyectil, posDisparo, rot);

        // Intentar inicializar el daño si el proyectil usa EntidadDaño directamente
        EntidadDaño dañoScript = proyectil.GetComponent<EntidadDaño>();
        if (dañoScript != null)
        {
            dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, true);
        }
        else
        {
            // O inicializar si es compatible con la interfaz IProyectil (como las balas de las armas)
            IProyectil proyectilScript = proyectil.GetComponent<IProyectil>();
            if (proyectilScript != null)
            {
                DatosProyectil datos = new DatosProyectil
                {
                    daño = daño,
                    rango = rangoDisparo,
                    origen = EntidadDaño.OrigenDaño.Enemigo,
                    arma = "enemigo"
                };
                proyectilScript.Inicializar(datos);
            }
        }
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
            Instantiate(prefabMaterial, transform.position, Quaternion.identity);
        }

        // 5. Destruir el objeto
        Destroy(gameObject);
    }
}
