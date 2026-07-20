using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UIElements;
using System.Collections.Generic;
public class Menu : MonoBehaviour
{
    [SerializeField] private UIDocument _UIDocument;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AudioSource _audioSource;

    private VisualElement _CreditosImg;
    private VisualElement _ConfiguracionPartida;
    private VisualElement _AjustesSonido;
    private DropdownField _CantidadNodos;

    private Button _Jugar;
    private Button _JugarOnline;
    private Button _UnirseOnline;
    private TextField _CodigoSala;
    private Button _Historia;
    private Button _Ajustes;
    private Button _Creditos;
    private Button _Salir;
    private Button _Salida;
    private Button _Listo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var root = _UIDocument.rootVisualElement;
        _Jugar=root.Q<Button>("Jugar");
        _JugarOnline=root.Q<Button>("JugarOnline");
        _UnirseOnline=root.Q<Button>("UnirseOnline");
        _CodigoSala=root.Q<TextField>("CodigoSala");
        _Historia=root.Q<Button>("Historia");
        _Ajustes = root.Q<Button>("Ajustes");
        _Creditos=root.Q<Button>("Creditos");
        _Salir=root.Q<Button>("Salir");
        _Salida=root.Q<Button>("Salida");
        _Listo=root.Q<Button>("BotonListo");

        _CreditosImg = root.Q<VisualElement>("CreditosFoto");
        _ConfiguracionPartida = root.Q<VisualElement>("OpcionesJuego");
        _AjustesSonido = root.Q<VisualElement>("AjustesSonido");

        _CantidadNodos = root.Q<DropdownField>("CantidadNodos");
        if (_CantidadNodos != null)
        {
            _CantidadNodos.choices = new List<string> { "1", "2", "3", "4", "5" };
            _CantidadNodos.value = "1"; // Valor por defecto
        }

        _Jugar.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.Flex;
            _AjustesSonido.style.display = DisplayStyle.None;
        };

        if (_JugarOnline != null)
        {
            _JugarOnline.clicked += async () => 
            {
                _audioSource.PlayOneShot(_clip);
                if (RelayManager.Instance != null)
                {
                    // Desactivar botón para evitar doble clic
                    _JugarOnline.SetEnabled(false);
                    string joinCode = await RelayManager.Instance.CrearSalaRelay();
                    
                    if (!string.IsNullOrEmpty(joinCode))
                    {
                        Debug.Log("¡Sala creada! Comparte este código con tu amigo: " + joinCode);
                        // Idealmente aquí mostraríamos el código en la pantalla antes de cambiar de escena,
                        // pero por ahora lo imprimimos en consola y cargamos el nivel.
                        NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
                    }
                    else
                    {
                        _JugarOnline.SetEnabled(true);
                    }
                }
            };
        }

        if (_UnirseOnline != null && _CodigoSala != null)
        {
            _UnirseOnline.clicked += async () =>
            {
                _audioSource.PlayOneShot(_clip);
                string codigo = _CodigoSala.value;
                if (!string.IsNullOrEmpty(codigo) && RelayManager.Instance != null)
                {
                    _UnirseOnline.SetEnabled(false);
                    bool exito = await RelayManager.Instance.UnirseSalaRelay(codigo);
                    if (!exito)
                    {
                        _UnirseOnline.SetEnabled(true);
                    }
                    // Si tiene éxito, el NetworkManager se encarga de sincronizar la escena automáticamente
                }
            };
        }

        _Historia.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
        };
        _Ajustes.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.Flex;
        };
        _Creditos.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.Flex;
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;

        };
        _Salir.clicked += () => 
        {
            
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
        };

        _Salida.clicked += () =>
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.None;
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
        };
        _Listo.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _AjustesSonido.style.display = DisplayStyle.None;
            
            // Para jugar "Offline" (Solo), iniciamos un Host local sin Relay
            if (NetworkManager.Singleton != null)
            {
                // Asegurarnos de que use la IP local por defecto
                NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData("127.0.0.1", 7777);
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
            }
        };


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
