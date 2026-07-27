using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    [Header("Referencias de Datos")]
    [Tooltip("Referencia a DatosNivel para conocer el nivel actual en partida.")]
    [SerializeField] private DatosNivel datosNivel;

    private UIDocument uiDocument;
    private VisualElement root;

    // Elemento del Relleno de la Terraformación
    private VisualElement barTerraRelleno;

    // Elementos del Jugador Local (Estático)
    private VisualElement localPlayerCard;
    private Label localNameLabel;
    private VisualElement localHealthRelleno;

    // Cartel Central de Nivel / Victoria
    private VisualElement bannerContainer;
    private Label bannerTitle;
    private Label bannerSubtitle;
    private Coroutine corrutinaBanner;

    // Instancia del gestor de terraformación en escena
    private GestorTerraformacion gestorTerra;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // Enlazar elementos de Terraformación (Izquierda)
        barTerraRelleno = root.Q<VisualElement>("terra-relleno");

        // Enlazar elementos del Jugador Local (Derecha - Estático)
        localPlayerCard = root.Q<VisualElement>("local-player-card");
        localNameLabel = root.Q<Label>("local-player-name");
        localHealthRelleno = root.Q<VisualElement>("local-health-relleno");

        // Enlazar Cartel Central
        bannerContainer = root.Q<VisualElement>("banner-container");
        bannerTitle = root.Q<Label>("banner-title");
        bannerSubtitle = root.Q<Label>("banner-subtitle");
    }

    private void Start()
    {
        // Buscar el gestor de terraformación en la escena
        gestorTerra = FindAnyObjectByType<GestorTerraformacion>();

        // Mostrar el cartel del nivel actual al iniciar la partida
        int nivelActual = datosNivel != null ? datosNivel.numeroNivel : 1;
        MostrarCartelTemporizado($"NIVEL {nivelActual}", "¡Defiende las Antenas de Terraformación!", 3.5f);
    }

    private void Update()
    {
        ActualizarTerraformacion();
        ActualizarVidaJugador();
    }

    public void MostrarCartelTemporizado(string titulo, string subtitulo, float duracion)
    {
        if (bannerContainer == null) return;

        if (corrutinaBanner != null)
        {
            StopCoroutine(corrutinaBanner);
        }

        corrutinaBanner = StartCoroutine(MostrarCartelCo(titulo, subtitulo, duracion));
    }

    public void MostrarVictoria(int nivelCompletado)
    {
        MostrarCartelTemporizado("¡VICTORIA!", $"¡Nivel {nivelCompletado} Completado!", 4.0f);
    }

    private IEnumerator MostrarCartelCo(string titulo, string subtitulo, float duracion)
    {
        if (bannerTitle != null) bannerTitle.text = titulo;
        if (bannerSubtitle != null) bannerSubtitle.text = subtitulo;
        if (bannerContainer != null) bannerContainer.style.display = DisplayStyle.Flex;

        yield return new WaitForSeconds(duracion);

        if (bannerContainer != null) bannerContainer.style.display = DisplayStyle.None;
        corrutinaBanner = null;
    }

    private void ActualizarTerraformacion()
    {
        if (gestorTerra == null)
        {
            gestorTerra = FindAnyObjectByType<GestorTerraformacion>();
            if (gestorTerra == null) return;
        }

        float porcentaje = Mathf.Clamp01(gestorTerra.porcentajeActual.Value);

        // Actualizar el ancho porcentual de la barra de terraformación
        if (barTerraRelleno != null)
        {
            barTerraRelleno.style.width = Length.Percent(porcentaje * 100f);
        }
    }

    private void ActualizarVidaJugador()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            if (localPlayerCard != null && !localPlayerCard.visible)
            {
                localPlayerCard.visible = true;
            }

            if (localNameLabel != null)
            {
                string nombre = player.playerName.Value.ToString();
                localNameLabel.text = string.IsNullOrEmpty(nombre) ? "Génesis" : nombre;
            }

            if (localHealthRelleno != null)
            {
                float pct = player.vidaMaxima > 0f ? Mathf.Clamp01(player.vida / player.vidaMaxima) : 0f;
                localHealthRelleno.style.width = Length.Percent(pct * 100f);
            }
        }
        else
        {
            if (localPlayerCard != null && localPlayerCard.visible)
            {
                localPlayerCard.visible = false;
            }
        }
    }
}
