using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BaseMenuNetworkManager : NetworkBehaviour
{
    public static BaseMenuNetworkManager Instance { get; private set; }

    [Header("Variables de Sincronización Netcode")]
    public NetworkVariable<int> numListos = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> totalClientes = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private HashSet<ulong> clientesListosServidor = new HashSet<ulong>();
    private bool nivelCargando = false;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;

        if (IsServer)
        {
            clientesListosServidor.Clear();
            numListos.Value = 0;
            int total = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) 
                ? NetworkManager.Singleton.ConnectedClientsList.Count 
                : 1;
            totalClientes.Value = Mathf.Max(1, total);
        }

        numListos.OnValueChanged += OnEstadoListoCambiado;
        totalClientes.OnValueChanged += OnEstadoListoCambiado;

        if (BaseMenuController.Instance != null)
        {
            BaseMenuController.Instance.RefrescarEstadoListoUI();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        numListos.OnValueChanged -= OnEstadoListoCambiado;
        totalClientes.OnValueChanged -= OnEstadoListoCambiado;
    }

    private void OnEstadoListoCambiado(int oldValue, int newValue)
    {
        if (BaseMenuController.Instance != null)
        {
            BaseMenuController.Instance.RefrescarEstadoListoUI();
        }
    }

    public void MarcarListoLocal()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            // Singleplayer / Offline
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level");
            return;
        }

        ulong miClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[BaseMenuNetworkManager] MarcarListoLocal invocado por cliente {miClientId}. IsServer: {IsServer}");

        if (IsServer)
        {
            MarcarClienteListoEnServidor(miClientId);
        }
        else
        {
            MarcarListoServerRpc(miClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MarcarListoServerRpc(ulong clientId)
    {
        Debug.Log($"[BaseMenuNetworkManager] ServerRpc recibido del cliente {clientId}");
        MarcarClienteListoEnServidor(clientId);
    }

    private void MarcarClienteListoEnServidor(ulong clientId)
    {
        if (!IsServer) return;

        clientesListosServidor.Add(clientId);
        int totalConectados = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 1;
        totalClientes.Value = Mathf.Max(1, totalConectados);
        numListos.Value = clientesListosServidor.Count;

        Debug.Log($"[BaseMenuNetworkManager] Servidor: Cliente {clientId} listo. Total listos: {numListos.Value}/{totalClientes.Value}");

        if (numListos.Value >= totalClientes.Value && !nivelCargando)
        {
            nivelCargando = true;
            Debug.Log("[BaseMenuNetworkManager] ¡Todos los jugadores listos! Cargando Level mediante NetworkSceneManager...");
            NetworkManager.Singleton.SceneManager.LoadScene("Level", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
