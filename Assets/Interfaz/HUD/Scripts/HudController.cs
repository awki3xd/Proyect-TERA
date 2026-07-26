using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    [Header("Plantillas UXML")]
    [Tooltip("La plantilla UXML del card de barra de vida de jugador (PlayerHealthBar.uxml).")]
    [SerializeField] private VisualTreeAsset playerBarTemplate;

    private UIDocument uiDocument;
    private VisualElement root;

    // Elemento del Relleno de la Terraformación
    private VisualElement barTerraRelleno;

    // Elementos del Jugador Local (Estático)
    private VisualElement localPlayerCard;
    private Label localNameLabel;
    private VisualElement localHealthRelleno;

    // Contenedor de jugadores extra
    private VisualElement extraPlayersContainer;

    // Instancia del gestor en escena
    private GestorTerraformacion gestorTerra;

    // Mapeo para controlar qué jugador tiene cuál tarjeta de vida instanciada en la UI
    private Dictionary<ulong, VisualElement> playerBars = new Dictionary<ulong, VisualElement>();

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

        // Enlazar contenedor de jugadores extra (Derecha - Dinámico)
        extraPlayersContainer = root.Q<VisualElement>("extra-players-container");
    }

    private void Start()
    {
        // Buscar el gestor de terraformación en la escena
        gestorTerra = FindAnyObjectByType<GestorTerraformacion>();
    }

    private void Update()
    {
        ActualizarTerraformacion();
        ActualizarListaJugadores();
    }

    private void ActualizarTerraformacion()
    {
        if (gestorTerra == null)
        {
            gestorTerra = FindAnyObjectByType<GestorTerraformacion>();
            if (gestorTerra == null) return;
        }

        float porcentaje = Mathf.Clamp01(gestorTerra.porcentajeActual.Value);

        // Actualizar únicamente el ancho (width) de la barra de progreso
        if (barTerraRelleno != null)
        {
            barTerraRelleno.style.width = Length.Percent(porcentaje * 100f);
        }
    }

    private void ActualizarListaJugadores()
    {
        if (extraPlayersContainer == null || playerBarTemplate == null) return;

        // Buscar todos los jugadores activos en la red
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        PlayerController localPlayer = null;
        List<PlayerController> remotePlayers = new List<PlayerController>();

        // Identificar cuál es el cliente local
        foreach (var p in players)
        {
            if (p == null) continue;

            if (p.IsLocalPlayer || p.IsOwner)
            {
                localPlayer = p;
            }
            else
            {
                remotePlayers.Add(p);
            }
        }

        // Fallback: si no hay un cliente local de red (ej: testing offline), tomar el primero como local
        if (localPlayer == null && players.Length > 0)
        {
            localPlayer = players[0];
            remotePlayers.Clear();
            for (int i = 1; i < players.Length; i++)
            {
                remotePlayers.Add(players[i]);
            }
        }

        // 1. Actualizar barra estática del jugador local
        if (localPlayer != null)
        {
            if (localPlayerCard != null && !localPlayerCard.visible) localPlayerCard.visible = true;

            if (localNameLabel != null)
            {
                string nombre = localPlayer.playerName.Value.ToString();
                localNameLabel.text = string.IsNullOrEmpty(nombre) ? "Génesis" : nombre;
            }

            if (localHealthRelleno != null)
            {
                float pct = localPlayer.vidaMaxima > 0f ? Mathf.Clamp01(localPlayer.vida / localPlayer.vidaMaxima) : 0f;
                localHealthRelleno.style.width = Length.Percent(pct * 100f);
            }
        }
        else
        {
            if (localPlayerCard != null && localPlayerCard.visible) localPlayerCard.visible = false;
        }

        // 2. Limpiar barras dinámicas para jugadores extra que se desconectaron
        List<ulong> idsParaEliminar = new List<ulong>();
        foreach (var key in playerBars.Keys)
        {
            bool existe = false;
            foreach (var p in remotePlayers)
            {
                if (p != null && p.OwnerClientId == key)
                {
                    existe = true;
                    break;
                }
            }
            if (!existe)
            {
                idsParaEliminar.Add(key);
            }
        }

        foreach (var id in idsParaEliminar)
        {
            if (playerBars.TryGetValue(id, out VisualElement barElement))
            {
                extraPlayersContainer.Remove(barElement);
            }
            playerBars.Remove(id);
        }

        // 3. Crear o actualizar barras dinámicas para jugadores extra
        foreach (var p in remotePlayers)
        {
            if (p == null) continue;

            ulong clientId = p.OwnerClientId;

            // Si no tiene barra de vida, instanciarla y agregarla
            if (!playerBars.TryGetValue(clientId, out VisualElement barElement))
            {
                VisualElement tpl = playerBarTemplate.Instantiate();
                barElement = tpl.Q<VisualElement>(null, "player-bar-card");
                if (barElement == null) continue;

                extraPlayersContainer.Add(barElement);
                playerBars.Add(clientId, barElement);
            }

            // Actualizar Nombre
            Label nameLabel = barElement.Q<Label>("player-name");
            if (nameLabel != null)
            {
                string nombre = p.playerName.Value.ToString();
                nameLabel.text = string.IsNullOrEmpty(nombre) ? $"Jugador {clientId}" : nombre;
            }

            // Actualizar Relleno de Barra
            VisualElement healthRelleno = barElement.Q<VisualElement>("health-relleno");
            if (healthRelleno != null)
            {
                float pct = p.vidaMaxima > 0f ? Mathf.Clamp01(p.vida / p.vidaMaxima) : 0f;
                healthRelleno.style.width = Length.Percent(pct * 100f);
            }
        }
    }
}
