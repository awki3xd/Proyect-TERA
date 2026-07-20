using System.Collections;
using UnityEngine;

public class EscorpionController : MonoBehaviour
{
    public enum EstadoEscorpion
    {
        PatrullandoCentro,
        PersiguiendoNodo,
        AtacandoNodo
    }

    [Header("Estadísticas del Escorpión")]
    [Tooltip("Vida del escorpión (inicializada con el valor base, se multiplicará en Inicializar).")]
    public float vida = 50f;
    [Tooltip("Daño del escorpión (inicializado con el valor base, se multiplicará en Inicializar).")]
    public float daño = 10f;
    [Tooltip("Velocidad de movimiento en unidades/segundo (se multiplicará en Inicializar).")]
    public float velocidadMovimiento = 2.5f;
    [Tooltip("Cadencia de ataque en ataques/segundo (se multiplicará en Inicializar).")]
    public float velocidadAtaque = 1f;
    [Tooltip("Rango de ataque en unidades (se multiplicará en Inicializar).")]
    public float rangoAtaque = 1.2f;

    [Header("Referencias de Prefabs")]
    [Tooltip("Prefab del objeto de daño (EntidadDaño) que instanciará al atacar.")]
    public GameObject prefabEntidadDaño;
    [Tooltip("Prefab del material o recurso (ej: Bridgmanita) que soltará al morir.")]
    public GameObject prefabMaterial;

    [Header("Configuración de IA y Órbita")]
    [Tooltip("Distancia a la que el escorpión detectará un nodo y empezará a perseguirlo.")]
    public float distanciaDeteccionNodo = 10f;
    [Tooltip("Multiplicador del rango de ataque para definir el radio real de órbita (ej: 0.7f es un 70% del rango de ataque).")]
    [Range(0.1f, 1f)]
    public float porcentajeRadioOrbita = 0.7f;
    [Tooltip("Distancia hacia adelante (según la dirección hacia el nodo) donde se instanciará la EntidadDaño.")]
    public float offsetDistanciaAtaque = 1f;
    [Tooltip("Desplazamiento del centro del nodo para corregir visualmente la órbita (por si el pivote del nodo está descentrado).")]
    public Vector2 offsetCentroNodo = Vector2.zero;

    [Header("Estado Actual de IA")]
    public EstadoEscorpion estadoActual = EstadoEscorpion.PatrullandoCentro;
    public NodoEstandar nodoObjetivo;

    private Vector2 direccionPatrulla;
    private float anguloOrbita;
    private float cooldownAtaque;
    private bool inicializado = false;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool estaMuerto = false;

    private void Start()
    {
        // Obtener el componente SpriteRenderer en este objeto o en sus hijos
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Obtener el componente Animator en este objeto o en sus hijos
        animator = GetComponentInChildren<Animator>();

        // Si no ha sido inicializado por el spawner, nos auto-inicializamos con multiplicadores por defecto
        if (!inicializado)
        {
            Inicializar(null, transform.position);
        }
    }

    /// <summary>
    /// Método de inicialización llamado por el spawner global al spawnear al enemigo.
    /// Aplica los multiplicadores porcentuales directamente sobre las variables de estadísticas principales.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales, Vector2 posicionInicial)
    {
        if (inicializado) return;
        inicializado = true;

        transform.position = posicionInicial;
        CalcularDireccionPatrulla(posicionInicial);

        // Si no hay datos globales de enemigos, el multiplicador es 1f (100%)
        float multVida = datosGlobales != null ? datosGlobales.vida / 100f : 1f;
        float multDaño = datosGlobales != null ? datosGlobales.daño / 100f : 1f;
        float multVelocidad = datosGlobales != null ? datosGlobales.velocidadMovimiento / 100f : 1f;
        float multVelocidadAtaque = datosGlobales != null ? datosGlobales.velocidadAtaque / 100f : 1f;
        float multRango = datosGlobales != null ? datosGlobales.rangoAtaque / 100f : 1f;

        // Sobrescribir las variables aplicando los multiplicadores directamente
        vida = vida * multVida;
        daño = daño * multDaño;
        velocidadMovimiento = velocidadMovimiento * multVelocidad;
        velocidadAtaque = velocidadAtaque * multVelocidadAtaque;
        rangoAtaque = rangoAtaque * multRango;

        estadoActual = EstadoEscorpion.PatrullandoCentro;
        nodoObjetivo = null;
    }

    private void Update()
    {
        switch (estadoActual)
        {
            case EstadoEscorpion.PatrullandoCentro:
                ActualizarPatrulla();
                break;
            case EstadoEscorpion.PersiguiendoNodo:
                ActualizarPersecucion();
                break;
            case EstadoEscorpion.AtacandoNodo:
                ActualizarAtaque();
                break;
        }

        ActualizarEstadoAnimador();
    }

    /// <summary>
    /// Mantiene sincronizado el estado del Animator ("atacando") con la máquina de estados de la IA.
    /// </summary>
    private void ActualizarEstadoAnimador()
    {
        if (animator != null)
        {
            bool esAtacando = (estadoActual == EstadoEscorpion.AtacandoNodo);
            animator.SetBool("Atacando", esAtacando);
        }
    }

    private void CalcularDireccionPatrulla(Vector2 posicionInicial)
    {
        // Vector dirección apuntando directo al centro (0,0)
        Vector2 direccionAlCentro = (Vector2.zero - posicionInicial).normalized;

        // Generamos una desviación aleatoria de entre -10 y +10 grados
        float desviacionAngulo = Random.Range(-10f, 10f);
        float anguloRadianes = desviacionAngulo * Mathf.Deg2Rad;

        // Aplicamos la matriz de rotación 2D
        float cos = Mathf.Cos(anguloRadianes);
        float sin = Mathf.Sin(anguloRadianes);

        direccionPatrulla = new Vector2(
            direccionAlCentro.x * cos - direccionAlCentro.y * sin,
            direccionAlCentro.x * sin + direccionAlCentro.y * cos
        ).normalized;
    }

    /// <summary>
    /// En lugar de rotar físicamente todo el GameObject (lo que pondría al sprite de cabeza),
    /// simplemente volteamos horizontalmente el sprite dependiendo de si va hacia la izquierda o derecha.
    /// </summary>
    private void ControlarVolteo(Vector2 direccion)
    {
        if (spriteRenderer != null && direccion.x != 0f)
        {
            // Asume que el sprite original mira a la derecha. Si mira a la izquierda, invierte la condición.
            spriteRenderer.flipX = direccion.x < 0f;
        }
    }

    private void ActualizarPatrulla()
    {
        // Voltear sprite según dirección de patrulla
        ControlarVolteo(direccionPatrulla);

        // Movimiento rectilíneo hacia el centro con la desviación
        transform.position = (Vector2)transform.position + direccionPatrulla * velocidadMovimiento * Time.deltaTime;

        // Buscar el nodo funcionando más cercano
        NodoEstandar closestNode = BuscarNodoMasCercano();
        if (closestNode != null)
        {
            float distancia = Vector2.Distance(transform.position, closestNode.transform.position);
            if (distancia <= distanciaDeteccionNodo)
            {
                nodoObjetivo = closestNode;
                estadoActual = EstadoEscorpion.PersiguiendoNodo;
            }
        }
    }

    private void ActualizarPersecucion()
    {
        // Si el nodo objetivo fue destruido o es nulo, buscar uno nuevo
        if (nodoObjetivo == null || nodoObjetivo.EstaRoto())
        {
            nodoObjetivo = BuscarNodoMasCercano();
            if (nodoObjetivo == null)
            {
                estadoActual = EstadoEscorpion.PatrullandoCentro;
                return;
            }
        }

        // Obtener dirección hacia el centro de órbita y voltear sprite
        Vector2 centroOrbita = (Vector2)nodoObjetivo.transform.position + offsetCentroNodo;
        Vector2 direccion = (centroOrbita - (Vector2)transform.position).normalized;
        ControlarVolteo(direccion);

        // Moverse directo hacia el centro de órbita del nodo
        transform.position = Vector2.MoveTowards(transform.position, centroOrbita, velocidadMovimiento * Time.deltaTime);

        // Si entramos en el rango de ataque, cambiar al estado de ataque y calcular ángulo de órbita
        float distancia = Vector2.Distance(transform.position, centroOrbita);
        if (distancia <= rangoAtaque)
        {
            Vector2 direccionOrbita = (Vector2)transform.position - centroOrbita;
            anguloOrbita = Mathf.Atan2(direccionOrbita.y, direccionOrbita.x);
            estadoActual = EstadoEscorpion.AtacandoNodo;
            cooldownAtaque = 0f; // Atacar inmediatamente al llegar
        }
    }

    private void ActualizarAtaque()
    {
        // Si el nodo objetivo fue destruido o es nulo, buscar uno nuevo
        if (nodoObjetivo == null || nodoObjetivo.EstaRoto())
        {
            nodoObjetivo = BuscarNodoMasCercano();
            if (nodoObjetivo == null)
            {
                estadoActual = EstadoEscorpion.PatrullandoCentro;
            }
            else
            {
                estadoActual = EstadoEscorpion.PersiguiendoNodo;
            }
            return;
        }

        // Voltear sprite según dirección al centro de órbita del nodo
        Vector2 centroOrbita = (Vector2)nodoObjetivo.transform.position + offsetCentroNodo;
        Vector2 direccionAlNodo = (centroOrbita - (Vector2)transform.position).normalized;
        ControlarVolteo(direccionAlNodo);

        // Lógica de Órbita alrededor del nodo usando Seno y Coseno
        float radioOrbitaReal = rangoAtaque * porcentajeRadioOrbita;

        // Velocidad angular (v = w * r => w = v / r). Dividida a la mitad para ralentizar la órbita en modo ataque.
        float velocidadAngular = (radioOrbitaReal > 0.05f ? velocidadMovimiento / radioOrbitaReal : velocidadMovimiento) * 0.5f;
        anguloOrbita += velocidadAngular * Time.deltaTime;

        // Calcular posición en la circunferencia del nodo
        Vector2 posicionObjetivo = centroOrbita + new Vector2(Mathf.Cos(anguloOrbita), Mathf.Sin(anguloOrbita)) * radioOrbitaReal;
        transform.position = Vector2.MoveTowards(transform.position, posicionObjetivo, velocidadMovimiento * Time.deltaTime);

        // Lógica de Cooldown de Ataque
        cooldownAtaque -= Time.deltaTime;
        if (cooldownAtaque <= 0f)
        {
            Atacar();
            // La frecuencia está determinada por velocidadAtaque (ej: 1 ataque por segundo)
            cooldownAtaque = 1f / Mathf.Max(0.1f, velocidadAtaque);
        }
    }

    private void Atacar()
    {
        if (prefabEntidadDaño == null || nodoObjetivo == null)
        {
            Debug.LogWarning("Falta asignar el prefab de daño o no hay nodo objetivo en " + gameObject.name);
            return;
        }

        // Dirección hacia el centro de órbita del nodo objetivo
        Vector2 centroOrbita = (Vector2)nodoObjetivo.transform.position + offsetCentroNodo;
        Vector2 direccionAlNodo = (centroOrbita - (Vector2)transform.position).normalized;

        // Instancia la EntidadDaño desplazada hacia adelante en la dirección al nodo
        Vector2 posicionAtaque = (Vector2)transform.position + direccionAlNodo * offsetDistanciaAtaque;

        // Calculamos la rotación del proyectil/hitbox para que apunte directamente al nodo
        float anguloAtaque = Mathf.Atan2(direccionAlNodo.y, direccionAlNodo.x) * Mathf.Rad2Deg;
        Quaternion rotacionAtaque = Quaternion.Euler(0f, 0f, anguloAtaque);

        GameObject objDaño = Instantiate(prefabEntidadDaño, posicionAtaque, rotacionAtaque);
        EntidadDaño dañoScript = objDaño.GetComponent<EntidadDaño>();
        if (dañoScript != null)
        {
            // Inicializar pasándole nuestro daño y el origen
            dañoScript.Inicializar(daño, EntidadDaño.OrigenDaño.Enemigo, true);
        }
    }

    private NodoEstandar BuscarNodoMasCercano()
{
    // 1. Buscamos rápidamente los objetos que tienen el tag "Nodo"
    GameObject[] objetosNodos = GameObject.FindGameObjectsWithTag("Nodo");
    NodoEstandar closest = null;
    float minDist = float.MaxValue;

    foreach (var obj in objetosNodos)
    {
        if (obj != null)
        {
            // 2. Extraemos su componente de forma individual
            NodoEstandar nodo = obj.GetComponent<NodoEstandar>();
            if (nodo != null && !nodo.EstaRoto())
            {
                float dist = Vector2.Distance(transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = nodo;
                }
            }
        }
    }
    return closest;
}


    /// <summary>
    /// Permite al jugador infligir daño a este enemigo en el futuro.
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

    /// <summary>
    /// Corrutina que maneja la secuencia de muerte del escorpión (animación, desactivación de colisiones, drop y destrucción).
    /// </summary>
    private IEnumerator MuerteCo()
    {
        // 1. Apagar colisiones inmediatamente para que no lo golpeen más balas
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

        // Cambiar la etiqueta para que las armas del jugador dejen de apuntarle
        gameObject.tag = "Untagged";

        // Detener por completo el movimiento de IA
        velocidadMovimiento = 0f;

        // 2. Activar la animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("muere");
        }

        // 3. Esperar a que la animación termine (1 segundo)
        yield return new WaitForSeconds(1f);

        // 4. Instanciar el material (Bridgmanita) de forma independiente en su posición
        if (prefabMaterial != null)
        {
            Instantiate(prefabMaterial, transform.position, Quaternion.identity);
        }

        // 5. Destruir el enemigo
        Destroy(gameObject);
    }
}
