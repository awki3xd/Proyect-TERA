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
    private VisualElement _PanelOnline;
    private DropdownField _CantidadNodos;

    private Button _Jugar;
    private Button _JugarOnline;
    private Button _CrearSalaOnline;
    private Button _UnirseOnline;
    private Button _VolverOnline;
    private TextField _CodigoSala;
    private TextField _NombreJugador;
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
        _CrearSalaOnline=root.Q<Button>("CrearSalaOnline");
        _UnirseOnline=root.Q<Button>("UnirseOnline");
        _VolverOnline=root.Q<Button>("VolverOnline");
        _CodigoSala=root.Q<TextField>("CodigoSala");
        _NombreJugador=root.Q<TextField>("NombreJugador");
        _Historia=root.Q<Button>("Historia");
        _Ajustes = root.Q<Button>("Ajustes");
        _Creditos=root.Q<Button>("Creditos");
        _Salir=root.Q<Button>("Salir");
        _Salida=root.Q<Button>("Salida");
        _Listo=root.Q<Button>("BotonListo");

        _CreditosImg = root.Q<VisualElement>("CreditosFoto");
        _ConfiguracionPartida = root.Q<VisualElement>("OpcionesJuego");
        _AjustesSonido = root.Q<VisualElement>("AjustesSonido");
        _PanelOnline = root.Q<VisualElement>("PanelOnline");

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
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
        };

        if (_JugarOnline != null)
        {
            _JugarOnline.clicked += () => 
            {
                _audioSource.PlayOneShot(_clip);
                _ConfiguracionPartida.style.display = DisplayStyle.None;
                _AjustesSonido.style.display = DisplayStyle.None;
                if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.Flex;
            };
        }

        if (_VolverOnline != null)
        {
            _VolverOnline.clicked += () => 
            {
                _audioSource.PlayOneShot(_clip);
                if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
            };
        }

        if (_CrearSalaOnline != null)
        {
            _CrearSalaOnline.clicked += async () => 
            {
                _audioSource.PlayOneShot(_clip);
                if (RelayManager.Instance != null)
                {
                    _CrearSalaOnline.SetEnabled(false);
                    
                    // Guardar el nombre del jugador
                    if (_NombreJugador != null)
                    {
                        PlayerPrefs.SetString("PlayerName", _NombreJugador.value);
                        PlayerPrefs.Save();
                    }

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
                        _CrearSalaOnline.SetEnabled(true);
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
                    
                    // Guardar el nombre del jugador
                    if (_NombreJugador != null)
                    {
                        PlayerPrefs.SetString("PlayerName", _NombreJugador.value);
                        PlayerPrefs.Save();
                    }

                    bool exito = await RelayManager.Instance.UnirseSalaRelay(codigo);
                    if (!exito)
                    {
                        _UnirseOnline.SetEnabled(true);
                    }
                }
            };
        }

        _Historia.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
        };
        _Ajustes.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.Flex;
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
        };
        _Creditos.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.Flex;
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
        };
        _Salir.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
        };

        _Salida.clicked += () =>
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.None;
            _ConfiguracionPartida.style.display = DisplayStyle.None;
            _AjustesSonido.style.display = DisplayStyle.None;
            if (_PanelOnline != null) _PanelOnline.style.display = DisplayStyle.None;
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
