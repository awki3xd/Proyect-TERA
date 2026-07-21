using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        try
        {
            // Crear un perfil único para evitar conflictos entre la Ventana 1 y la Ventana 2
            InitializationOptions options = new InitializationOptions();
            options.SetProfile("Player_" + Random.Range(1000, 9999));

            // Inicializar los servicios de Unity con el perfil único
            await UnityServices.InitializeAsync(options);

            // Iniciar sesión de forma anónima (necesario para usar Relay)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log("Sesión iniciada anónimamente. Player ID: " + AuthenticationService.Instance.PlayerId);
                };
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al inicializar Unity Services: " + e.Message);
        }
    }

    /// <summary>
    /// Crea una sala en Relay y devuelve el código de unión.
    /// </summary>
    public async Task<string> CrearSalaRelay(int maxJugadores = 4)
    {
        try
        {
            // Restamos 1 porque el Host ya cuenta como un jugador
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);

            // Obtener el código de la sala
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Sala creada con éxito. Código: " + joinCode);

            // Configurar el NetworkManager para usar este Relay
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Iniciar el Host
            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error al crear la sala de Relay: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Se une a una sala existente usando un código.
    /// </summary>
    public async Task<bool> UnirseSalaRelay(string joinCode)
    {
        try
        {
            Debug.Log("Intentando unirse a la sala con código: " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Configurar el NetworkManager para usar este Relay
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Iniciar el Cliente
            NetworkManager.Singleton.StartClient();
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error al unirse a la sala de Relay: " + e.Message);
            return false;
        }
    }
}
