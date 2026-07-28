using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class AvispaController : MonoBehaviour
{
    public enum EstadoAvispa
    {
        VolandoAlCentro,
        Francotirador
    }

    [Header("Estadísticas de la Avispa")]
    [Tooltip("Vida inicial de la avispa (mayor resistencia que la mosca).")]
    public float vida = 75f;
    [Tooltip("Daño por cada disparo penetrante.")]
    public float daño = 15f;
    [Tooltip("Velocidad de desplazamiento hacia el centro.")]
    public float velocidadMovimiento = 1.5f;
    [Tooltip("Tiempo de recarga entre disparos del francotirador.")]
    public float velocidadAtaque = 3.5f;

    [Header("Referencias de Prefabs y Recompensas")]
    [Tooltip("Prefab de la bala de la avispa (con ValaMoscaController y EntidadDaño con destruirAlImpactar = false).")]
    public GameObject prefabProyectil;
    [Tooltip("Prefab del material o recurso que soltará al morir.")]
    public GameObject prefabMaterial;
    [Tooltip("Prefab del cofre de armas élite.")]
    public GameObject prefabCofre;
    [Tooltip("Referencia al ScriptableObject DatosNivel para calcular probabilidad de drop.")]
    public DatosNivel datosNivel;

    [Header("Configuración de IA")]
    [Tooltip("Distancia respecto al centro (0,0) a la que se detendrá permanentemente.")]
    public float distanciaAlCentroDetencion = 8f;
    [Tooltip("Rango de alcance del disparo. Al ser un francotirador, es muy alto (casi infinito).")]
    public float rangoDisparo = 30f;
    [Tooltip("Distancia hacia adelante desde el centro de la avispa donde nace el proyectil.")]
    public float offsetDistanciaDisparo = 0.6f;

    [Header("Estado Actual de IA")]
    public EstadoAvispa estadoActual = EstadoAvispa.VolandoAlCentro;

    private Transform playerObjetivo;
    private Rigidbody2D rb;
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
    /// Método de inicialización llamado por el spawner de enemigos.
    /// Aplica el escalado de estadísticas locales en base al nivel.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales, Vector2 posicionInicial)
    {
        if (inicializado) return;
        inicializado = true;

        transform.position = posicionInicial;

        // Modificadores de estadísticas globales
        float multVida = datosGlobales != null ? datosGlobales.vida / 100f : 1f;
        float multDaño = datosGlobales != null ? datosGlobales.daño / 100f : 1f;
        float multVelocidad = datosGlobales != null ? datosGlobales.velocidadMovimiento / 100f : 1f;
        float multVelocidadAtaque = datosGlobales != null ? datosGlobales.velocidadAtaque / 100f : 1f;

        vida = vida * multVida;
        daño = daño * multDaño;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;
        velocidadAtaque = velocidadAtaque * multVelocidadAtaque;

        cooldownDisparo = 0f;
        estadoActual = EstadoAvispa.VolandoAlCentro;
    }

    private void Update()
    {
        if (estaMuerto) return;

        // Descontar cooldown
        if (cooldownDisparo > 0f)
        {
            cooldownDisparo -= Time.deltaTime;
        }

        // 1. Evaluar si debe transicionar a modo Francotirador permanente
        if (estadoActual == EstadoAvispa.VolandoAlCentro)
        {
            float distAlCentro = Vector2.Distance(transform.position, Vector2.zero);
            if (distAlCentro <= distanciaAlCentroDetencion)
            {
                estadoActual = EstadoAvispa.Francotirador;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        // 2. Apuntar y disparar en modo Francotirador
        if (estadoActual == EstadoAvispa.Francotirador)
        {
            BuscarJugadorMasCercano();

            if (playerObjetivo != null)
            {
                Vector2 dirHaciaPlayer = ((Vector2)playerObjetivo.position - (Vector2)transform.position).normalized;

                // Apuntar rotando el transform hacia el jugador
                float angulo = Mathf.Atan2(dirHaciaPlayer.y, dirHaciaPlayer.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angulo);

                // Control del flip del sprite
                if (spriteRenderer != null)
                {
                    // Si el eje X de la dirección es negativo, mirar hacia la izquierda
                    spriteRenderer.flipX = dirHaciaPlayer.x < 0f;
                }

                // Intentar disparar
                if (cooldownDisparo <= 0f)
                {
                    Disparar(dirHaciaPlayer);
                    cooldownDisparo = velocidadAtaque;
                }
            }
        }
        else
        {
            // Si está volando al centro, orientar la vista
            Vector2 dirMov = -((Vector2)transform.position).normalized;
            if (spriteRenderer != null && dirMov.sqrMagnitude > 0.01f)
            {
                spriteRenderer.flipX = dirMov.x < 0f;
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

        if (estadoActual == EstadoAvispa.VolandoAlCentro)
        {
            Vector2 dirHaciaCentro = ((Vector2)Vector2.zero - (Vector2)transform.position).normalized;
            rb.linearVelocity = dirHaciaCentro * velocidadMovimiento;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void BuscarJugadorMasCercano()
    {
        PlayerController[] jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Transform target = null;
        float minDist = float.MaxValue;

        foreach (var p in jugadores)
        {
            if (p != null)
            {
                float dist = Vector2.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = p.transform;
                }
            }
        }
        playerObjetivo = target;
    }

    private void Disparar(Vector2 direccion)
    {
        if (prefabProyectil == null) return;

        // Instanciar proyectil
        Vector2 posDisparo = (Vector2)transform.position + direccion * offsetDistanciaDisparo;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
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

        // Sonido de disparo francotirador/enemigo
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.DisparoEnemigo);
        }

        // Lanzar animación de disparo
        if (animator != null)
        {
            animator.SetTrigger("dispara");
        }

        // Inicializar la bala de la avispa
        ValaMoscaController valaScript = proyectil.GetComponent<ValaMoscaController>();
        if (valaScript == null)
        {
            valaScript = proyectil.GetComponentInChildren<ValaMoscaController>();
        }

        if (valaScript != null)
        {
            // AL SER FRANCOTIRADOR: la bala penetra todo sin destruirse (tercer parámetro = false)
            valaScript.Inicializar(daño, rangoDisparo, false);
        }
        else
        {
            // Fallback directo a EntidadDaño si no tiene script de vuelo
            EntidadDaño dañoScript = proyectil.GetComponent<EntidadDaño>();
            if (dañoScript == null)
            {
                dañoScript = proyectil.GetComponentInChildren<EntidadDaño>();
            }

            if (dañoScript != null)
            {
                dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, false);
            }
        }
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
        // 1. Desactivar colisiones
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Cambiar tag para que el jugador deje de apuntarle
        gameObject.tag = "Untagged";
        velocidadMovimiento = 0f;

        // Sonido de muerte
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.MuerteEnemigo);
        }

        // 2. Lanzar animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("muere");
        }

        yield return new WaitForSeconds(1f);

        // 3. Spawnear 3 materiales dispersos (+/- 0.5f unidades)
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

        // 4. Evaluar probabilidad de drop de cofre de armas según el nivel
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
            Debug.Log($"[Avispa Muerte] ¡Cofre generado en Nivel {nivelActual}! (Probabilidad: {probCofre * 100:F0}%).");
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
