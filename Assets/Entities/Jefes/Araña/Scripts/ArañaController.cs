using System.Collections;
using UnityEngine;

public class ArañaController : MonoBehaviour
{
    public enum EstadoAraña
    {
        SpawneoEntrada,
        Orbitando,
        PersecucionYDisparo
    }

    [Header("Estadísticas de la Araña (Jefe)")]
    [Tooltip("Vida máxima del jefe araña.")]
    public float vidaMaxima = 500f;
    [Tooltip("Vida actual del jefe araña.")]
    public float vida = 500f;
    [Tooltip("Daño infligido por los proyectiles de la ráfaga.")]
    public float daño = 20f;
    [Tooltip("Velocidad de desplazamiento de la araña.")]
    public float velocidadMovimiento = 2.5f;
    [Tooltip("Intervalo base en segundos entre ráfagas de disparo en la Fase 3.")]
    public float velocidadAtaque = 4.0f;

    [Header("Referencias de Prefabs y Recompensas")]
    [Tooltip("Prefab del proyectil que dispara en ráfaga radial en estrella (360°).")]
    public GameObject prefabProyectil;

    [Tooltip("Prefab del charco de ácido que suelta periódicamente.")]
    public GameObject prefabCharcoAcido;

    [Tooltip("Punto de anclaje/salida donde se instanciará el charco de ácido (si es nulo se usará transform.position).")]
    public Transform puntoCharcoAcido;

    [Tooltip("Punto de anclaje/salida del ácido para cuando la araña mira a la DERECHA.")]
    public Transform puntoCharcoAcidoDerecha;

    [Tooltip("Punto de anclaje/salida del ácido para cuando la araña mira a la IZQUIERDA.")]
    public Transform puntoCharcoAcidoIzquierda;

    [Tooltip("Prefab de material o recurso que suelta al morir.")]
    public GameObject prefabMaterial;

    [Tooltip("Prefab del cofre de recompensa de armas del jefe.")]
    public GameObject prefabCofre;

    [Tooltip("Referencia al ScriptableObject de datos del nivel para la probabilidad de cofre.")]
    public DatosNivel datosNivel;

    [Tooltip("Referencia a la barra de vida del jefe araña (asignada en inspector).")]
    public BarraVidaJefe barraVidaJefe;

    [Header("Configuración de Fases y Tiempos")]
    [Tooltip("Cantidad de proyectiles generados en el patrón radial en estrella de 360°.")]
    public int cantidadProyectilesEstrella = 24;
    [Tooltip("Intervalo en segundos para soltar charcos de ácido durante la Fase de Órbita.")]
    public float cooldownCharcoAcido = 5f;

    [Header("Estado Actual de IA")]
    public EstadoAraña estadoActual = EstadoAraña.SpawneoEntrada;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Transform playerObjetivo;
    private DatosGlobalesEnemigos datosGlobalesLocales;
    private bool inicializado = false;
    private bool estaMuerta = false;
    private bool estaPausadoPorAtaque = false;

    private float timerFase = 0f;
    private float timerCharco = 0f;
    private float timerDisparo = 0f;
    private float anguloOrbita = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.freezeRotation = true;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (datosNivel == null)
        {
            datosNivel = Resources.Load<DatosNivel>("DatosNivel");
        }

        if (barraVidaJefe == null)
        {
            barraVidaJefe = GetComponentInChildren<BarraVidaJefe>();
        }

        if (barraVidaJefe != null)
        {
            barraVidaJefe.Inicializar(() => vida, () => vidaMaxima);
        }

        if (!inicializado)
        {
            Inicializar(null, transform.position);
        }
    }

    /// <summary>
    /// Inicializa las estadísticas del jefe Araña aplicando los modificadores del nivel.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales, Vector2 posicionInicial)
    {
        if (inicializado) return;
        inicializado = true;
        datosGlobalesLocales = datosGlobales;

        transform.position = posicionInicial;

        float multVida = datosGlobales != null ? datosGlobales.vida / 100f : 1f;
        float multDaño = datosGlobales != null ? datosGlobales.daño / 100f : 1f;
        float multVelocidad = datosGlobales != null ? datosGlobales.velocidadMovimiento / 100f : 1f;

        vidaMaxima = vidaMaxima * multVida;
        vida = vidaMaxima;
        daño = daño * multDaño;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;

        if (barraVidaJefe != null)
        {
            barraVidaJefe.Inicializar(() => vida, () => vidaMaxima);
        }

        estadoActual = EstadoAraña.SpawneoEntrada;
        timerFase = 0f;
        timerCharco = 0f;
        timerDisparo = 0f;
        anguloOrbita = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    }

    private void Update()
    {
        if (estaMuerta) return;

        BuscarJugadorMasCercano();

        // Pausar la acumulación de temporizadores y movimiento si está ejecutando la animación del ataque de ácido (5s)
        if (estaPausadoPorAtaque)
        {
            return;
        }

        switch (estadoActual)
        {
            case EstadoAraña.SpawneoEntrada:
                // Caminar desde el exterior hacia el centro (0,0)
                float distAlCentro = Vector2.Distance(transform.position, Vector2.zero);
                if (distAlCentro <= 8f)
                {
                    estadoActual = EstadoAraña.Orbitando;
                    timerFase = 0f;
                    timerCharco = 0f;
                    Debug.Log("[Araña Jefe] Entrada completada. Cambiando a Fase: Orbitando.");
                }
                break;

            case EstadoAraña.Orbitando:
                timerCharco += Time.deltaTime;

                // Soltar charco de ácido periódicamente (cada 5s por defecto)
                if (timerCharco >= cooldownCharcoAcido)
                {
                    timerCharco = 0f;
                    SoltarCharcoAcido();
                }

                // Transición a Fase 3 cuando la vida baje del 50%
                if (vida <= vidaMaxima * 0.5f)
                {
                    estadoActual = EstadoAraña.PersecucionYDisparo;
                    timerDisparo = 0f;
                    Debug.Log($"[Araña Jefe] Vida bajó del 50% ({vida}/{vidaMaxima}). ¡Transición a Fase 3: Persecución y Disparo!");
                }
                break;

            case EstadoAraña.PersecucionYDisparo:
                timerDisparo += Time.deltaTime;

                // Disparar ráfaga radial en estrella en base a la velocidad de ataque (base 4.0s)
                float cooldownCalculado = velocidadAtaque / (datosGlobalesLocales != null ? Mathf.Max(0.1f, datosGlobalesLocales.velocidadAtaque / 100f) : 1f);
                if (timerDisparo >= cooldownCalculado)
                {
                    timerDisparo = 0f;
                    StartCoroutine(RutinaDisparoEstrellaCo());
                }
                break;
        }

        ActualizarOrientacionVisual();
    }

    private void FixedUpdate()
    {
        if (estaMuerta || rb == null) return;

        // Pausar movimiento 100% si está ejecutando la animación del ataque de ácido o disparo estrella
        if (estaPausadoPorAtaque)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        switch (estadoActual)
        {
            case EstadoAraña.SpawneoEntrada:
                Vector2 dirCentro = ((Vector2)Vector2.zero - (Vector2)transform.position).normalized;
                rb.linearVelocity = dirCentro * velocidadMovimiento;
                break;

            case EstadoAraña.Orbitando:
                // Movimiento de órbita elíptica variada alrededor de (0,0)
                anguloOrbita += (velocidadMovimiento / 6f) * Time.fixedDeltaTime;
                
                // Variación ondulante en el radio para que no sea un círculo perfecto
                float radioDinamico = 8f + Mathf.Sin(Time.time * 2f) * 1.5f;
                Vector2 objetivoOrbita = new Vector2(
                    Mathf.Cos(anguloOrbita) * radioDinamico,
                    Mathf.Sin(anguloOrbita) * (radioDinamico * 0.75f)
                );

                Vector2 dirOrbita = (objetivoOrbita - (Vector2)transform.position).normalized;
                rb.linearVelocity = dirOrbita * velocidadMovimiento;
                break;

            case EstadoAraña.PersecucionYDisparo:
                if (playerObjetivo != null)
                {
                    float distAlJugador = Vector2.Distance(transform.position, playerObjetivo.position);
                    Vector2 dirJugador = ((Vector2)playerObjetivo.position - (Vector2)transform.position).normalized;

                    // Mantener un margen prudencial de ~5 unidades respecto al jugador
                    if (distAlJugador > 4f)
                    {
                        rb.linearVelocity = dirJugador * velocidadMovimiento;
                    }
                    else if (distAlJugador < 2f)
                    {
                        // Retroceder ligeramente si el jugador se acerca demasiado
                        rb.linearVelocity = -dirJugador * (velocidadMovimiento * 0.8f);
                    }
                    else
                    {
                        // Orbitar lateralmente alrededor del jugador
                        Vector2 dirPerpendicular = new Vector2(-dirJugador.y, dirJugador.x);
                        rb.linearVelocity = dirPerpendicular * velocidadMovimiento;
                    }
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
                break;
        }
    }

    private IEnumerator RutinaPausaAcidoCo()
    {
        estaPausadoPorAtaque = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        yield return new WaitForSeconds(5.5f);

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        estaPausadoPorAtaque = false;
        Debug.Log("[Araña Jefe] Fin de animación de ácido. Reanudando movimiento.");
    }

    private void SoltarCharcoAcido()
    {
        if (prefabCharcoAcido == null) return;

        // 1. Pausar movimiento 100% quieto durante 5.5 segundos exactos
        StartCoroutine(RutinaPausaAcidoCo());

        // 2. Disparar el trigger de animación 'acido' en el mismo fotograma
        if (animator != null)
        {
            animator.SetTrigger("acido");
        }

        // 3. Determinar el punto de spawneo exacto según la orientación del sprite (flipX)
        bool mirandoIzquierda = spriteRenderer != null && spriteRenderer.flipX;
        Vector3 posCharco = transform.position;

        if (mirandoIzquierda && puntoCharcoAcidoIzquierda != null)
        {
            posCharco = puntoCharcoAcidoIzquierda.position;
        }
        else if (!mirandoIzquierda && puntoCharcoAcidoDerecha != null)
        {
            posCharco = puntoCharcoAcidoDerecha.position;
        }
        else if (puntoCharcoAcido != null)
        {
            posCharco = puntoCharcoAcido.position;
        }

        // 4. Instanciar e inicializar el charco de ácido con rotación Y=180° si la araña mira a la izquierda
        Quaternion rotacionCharco = mirandoIzquierda ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        GameObject acidoObj = Instantiate(prefabCharcoAcido, posCharco, rotacionCharco);

        AcidoController acidoScript = acidoObj.GetComponent<AcidoController>();
        if (acidoScript == null) acidoScript = acidoObj.GetComponentInChildren<AcidoController>();
        
        if (acidoScript != null)
        {
            acidoScript.Inicializar(datosGlobalesLocales);
        }

        Debug.Log($"[Araña Jefe] Ataque de ácido ejecutado (Rotación Y: {(mirandoIzquierda ? 180 : 0)}°). Posición: {posCharco}. Congelada 5.5s.");
    }

    private IEnumerator RutinaDisparoEstrellaCo()
    {
        // 1. Congelar movimiento inmediatamente
        estaPausadoPorAtaque = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // 2. Disparar el trigger de animación 'dispara'
        if (animator != null)
        {
            animator.SetTrigger("dispara");
        }

        // 3. Esperar 1.7 segundos de anticipación/carga de la animación
        yield return new WaitForSeconds(1.7f);

        // 4. Ejecutar el disparo radial en estrella (360°)
        EjecutarDisparoRadial();

        // 5. Esperar 0.5 segundos de recuperación post-disparo (2.2s total congelado)
        yield return new WaitForSeconds(0.5f);

        // 6. Restablecer físicas y reanudar movimiento
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        estaPausadoPorAtaque = false;
        Debug.Log("[Araña Jefe] Fin de ráfaga estrella. Movimiento reanudado.");
    }

    private void EjecutarDisparoRadial()
    {
        if (prefabProyectil == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.DisparoEnemigo);
        }

        float pasoAngulo = 360f / Mathf.Max(1, cantidadProyectilesEstrella);
        float offsetAngulo = Random.Range(0f, 360f); // Leve variación angular aleatoria por ráfaga

        for (int i = 0; i < cantidadProyectilesEstrella; i++)
        {
            float anguloActual = i * pasoAngulo + offsetAngulo;
            Quaternion rotacion = Quaternion.Euler(0f, 0f, anguloActual);

            GameObject proyectil = Instantiate(prefabProyectil, transform.position, rotacion);

            ValaMoscaController valaScript = proyectil.GetComponent<ValaMoscaController>();
            if (valaScript == null) valaScript = proyectil.GetComponentInChildren<ValaMoscaController>();

            if (valaScript != null)
            {
                valaScript.Inicializar(daño, 25f, true);
            }
            else
            {
                EntidadDaño dañoScript = proyectil.GetComponent<EntidadDaño>();
                if (dañoScript == null) dañoScript = proyectil.GetComponentInChildren<EntidadDaño>();

                if (dañoScript != null)
                {
                    dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, true);
                }
            }
        }

        Debug.Log($"[Araña Jefe] Ráfaga en estrella disparada ({cantidadProyectilesEstrella} balas).");
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
        playerObjetivo = masCercano;
    }

    private void ActualizarOrientacionVisual()
    {
        if (spriteRenderer == null || rb == null) return;

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            spriteRenderer.flipX = rb.linearVelocity.x < 0f;
        }
    }

    /// <summary>
    /// Recibe daño de las armas de Génesis y gestiona la salud y muerte del jefe Araña.
    /// </summary>
    public void RecibirDaño(float cantidad)
    {
        if (estaMuerta) return;

        vida = Mathf.Max(0f, vida - cantidad);

        // Crear número de daño flotante en color blanco
        TextoDañoFlotante.Crear(transform.position, cantidad, Color.white);

        if (vida <= 0f)
        {
            estaMuerta = true;
            StartCoroutine(MuerteCo());
        }
    }

    private IEnumerator MuerteCo()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        gameObject.tag = "Untagged";
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.MuerteEnemigo);
        }

        if (animator != null)
        {
            animator.SetTrigger("muere");
        }

        yield return new WaitForSeconds(1.5f);

        // Spawnear drop de material masivo
        if (prefabMaterial != null)
        {
            for (int i = 0; i < 5; i++)
            {
                Instantiate(prefabMaterial, (Vector2)transform.position + Random.insideUnitCircle * 0.8f, Quaternion.identity);
            }
        }

        // Spawnear cofre de recompensa garantizado
        if (prefabCofre != null)
        {
            Instantiate(prefabCofre, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
