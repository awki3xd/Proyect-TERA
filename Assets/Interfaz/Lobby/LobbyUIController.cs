using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _UIDocument;

    private Label _TextoCodigo;
    private Label _TextoCantJugadores;
    private ScrollView _ListaJugadores;
    private Button _BotonAccion;
    private Button _BotonSalir;

    private bool _isLocalPlayerReady = false;

    private void OnEnable()
    {
        if (_UIDocument == null)
        {
            _UIDocument = GetComponent<UIDocument>();
        }

        if (_UIDocument == null)
        {
            Debug.LogError("[LobbyUIController] UIDocument no encontrado en el GameObject.");
            return;
        }

        var root = _UIDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[LobbyUIController] rootVisualElement es nulo.");
            return;
        }

        _TextoCodigo = root.Q<Label>("TextoCodigo");
        _TextoCantJugadores = root.Q<Label>("TextoCantJugadores");
        _ListaJugadores = root.Q<ScrollView>("ListaJugadores");
        _BotonAccion = root.Q<Button>("BotonAccion");
        _BotonSalir = root.Q<Button>("BotonSalir");

        if (_BotonSalir != null)
        {
            _BotonSalir.clicked += OnSalirClicked;
        }

        if (_BotonAccion != null)
        {
            _BotonAccion.clicked += OnAccionClicked;
        }

        // Mostrar el código de sala si existe
        StartCoroutine(MostrarCodigoSalaCo());
    }

    private void OnDisable()
    {
        if (_BotonSalir != null)
        {
            _BotonSalir.clicked -= OnSalirClicked;
        }

        if (_BotonAccion != null)
        {
            _BotonAccion.clicked -= OnAccionClicked;
        }
    }

    private IEnumerator MostrarCodigoSalaCo()
    {
        // Esperar un frame para asegurar que RelayManager esté inicializado
        yield return null;

        if (_TextoCodigo != null && RelayManager.Instance != null)
        {
            string code = RelayManager.Instance.JoinCode;
            if (!string.IsNullOrEmpty(code))
            {
                _TextoCodigo.text = "Código de Sala: " + code;
            }
            else
            {
                _TextoCodigo.text = "Código de Sala: Local (No Relay)";
            }
        }
    }

    private void Start()
    {
        ActualizarBotonAccionTexto();
    }

    private void Update()
    {
        // Actualizar la lista de jugadores buscando los PlayerControllers en la escena
        ActualizarListaJugadoresDesdePersonajes();

        // Si somos el host, habilitar/deshabilitar el botón de comenzar según si todos están listos
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            ActualizarBotonComenzarHabilitado();
        }
    }

    private void ActualizarBotonAccionTexto()
    {
        if (_BotonAccion == null) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            _BotonAccion.text = "COMENZAR PARTIDA";
            _BotonAccion.RemoveFromClassList("boton-salir");
            _BotonAccion.AddToClassList("boton-comenzar");
        }
        else
        {
            _BotonAccion.text = _isLocalPlayerReady ? "NO LISTO" : "LISTO";
            if (_isLocalPlayerReady)
            {
                _BotonAccion.RemoveFromClassList("boton-comenzar");
                _BotonAccion.AddToClassList("boton-salir"); // Usar color gris/rojo para cancelar listo
            }
            else
            {
                _BotonAccion.RemoveFromClassList("boton-salir");
                _BotonAccion.AddToClassList("boton-comenzar");
            }
        }
    }

    private void ActualizarBotonComenzarHabilitado()
    {
        if (_BotonAccion == null) return;

        // El host puede empezar si todos los clientes están listos
        bool todosListos = true;
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        
        foreach (var player in players)
        {
            // El host siempre se considera listo, solo validamos a los clientes
            if (player.OwnerClientId != NetworkManager.ServerClientId && !player.isReady.Value)
            {
                todosListos = false;
                break;
            }
        }
        
        _BotonAccion.SetEnabled(todosListos);
    }

    private void OnAccionClicked()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            // El Host comienza la partida y carga la escena de juego
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
            }
        }
        else
        {
            // El Cliente cambia su estado de listo
            _isLocalPlayerReady = !_isLocalPlayerReady;
            ActualizarBotonAccionTexto();

            // Buscar el PlayerController local y actualizar su estado en red
            PlayerController localPlayer = GetLocalPlayerController();
            if (localPlayer != null)
            {
                localPlayer.SetReadyStatusServerRpc(_isLocalPlayerReady);
            }
        }
    }

    private void OnSalirClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("Menu");
    }

    private PlayerController GetLocalPlayerController()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player.IsOwner) return player;
        }
        return null;
    }

    private int lastPlayerCount = -1;

    private void ActualizarListaJugadoresDesdePersonajes()
    {
        if (_ListaJugadores == null) return;

        // Buscar todos los PlayerController en la escena
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        int currentPlayersCount = players.Length;

        if (currentPlayersCount != lastPlayerCount)
        {
            lastPlayerCount = currentPlayersCount;
            Debug.Log($"[LobbyUIController] Lista de jugadores actualizada. Cantidad: {currentPlayersCount}");
        }
        
        _ListaJugadores.Clear();

        int maxPlayers = 4; // Limitar a 4 jugadores

        if (_TextoCantJugadores != null)
        {
            _TextoCantJugadores.text = $"Máx jugadores: {currentPlayersCount}/{maxPlayers}";
        }

        // Renderizar jugadores conectados
        for (int i = 0; i < currentPlayersCount; i++)
        {
            if (i >= maxPlayers) break; // Evitar desbordamiento si hay más de 4 por algún motivo

            var player = players[i];
            bool isHost = player.OwnerClientId == NetworkManager.ServerClientId;

            VisualElement slot = new VisualElement();
            slot.AddToClassList("jugador-slot");

            // Avatar
            VisualElement avatar = new VisualElement();
            avatar.AddToClassList("jugador-avatar");
            slot.Add(avatar);

            // Info (Nombre y Rol)
            VisualElement info = new VisualElement();
            info.AddToClassList("jugador-info");

            VisualElement nombreContenedor = new VisualElement();
            nombreContenedor.AddToClassList("jugador-nombre-contenedor");

            Label nombre = new Label(player.playerName.Value.ToString());
            nombre.AddToClassList("jugador-nombre");
            nombreContenedor.Add(nombre);

            if (isHost)
            {
                Label rol = new Label("HOST / LÍDER");
                rol.AddToClassList("jugador-rol");
                nombreContenedor.Add(rol);
            }

            info.Add(nombreContenedor);
            slot.Add(info);

            // Estado (Listo / Esperando)
            VisualElement estadoContenedor = new VisualElement();
            estadoContenedor.AddToClassList("jugador-estado-contenedor");

            Label estadoBadge = new Label();
            estadoBadge.AddToClassList("badge-estado");

            if (isHost)
            {
                estadoBadge.text = "LISTO";
                estadoBadge.AddToClassList("estado-listo");
            }
            else
            {
                bool ready = player.isReady.Value;
                estadoBadge.text = ready ? "LISTO" : "ESPERANDO";
                estadoBadge.AddToClassList(ready ? "estado-listo" : "estado-esperando");
            }

            estadoContenedor.Add(estadoBadge);
            slot.Add(estadoContenedor);

            _ListaJugadores.Add(slot);
        }

        // Renderizar slots vacíos
        for (int i = currentPlayersCount; i < maxPlayers; i++)
        {
            VisualElement slotVacio = new VisualElement();
            slotVacio.AddToClassList("jugador-slot");
            slotVacio.AddToClassList("slot-vacio");

            Label textoVacio = new Label("[Espacio para unirse]");
            textoVacio.AddToClassList("slot-vacio-texto");
            slotVacio.Add(textoVacio);

            _ListaJugadores.Add(slotVacio);
        }
    }
}
