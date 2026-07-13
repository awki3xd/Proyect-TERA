using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Menu : MonoBehaviour
{
    [SerializeField] private UIDocument _UIDocument;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AudioSource _audioSource;

    private VisualElement _CreditosImg;
    private VisualElement _ConfiguracionPartida;
    private VisualElement _AjustesSonido;

    private Button _Jugar;
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
        _Historia=root.Q<Button>("Historia");
        _Ajustes = root.Q<Button>("Ajustes");
        _Creditos=root.Q<Button>("Creditos");
        _Salir=root.Q<Button>("Salir");
        _Salida=root.Q<Button>("Salida");
        _Listo=root.Q<Button>("BotonListo");

        _CreditosImg = root.Q<VisualElement>("CreditosFoto");
        _ConfiguracionPartida = root.Q<VisualElement>("OpcionesJuego");
        _AjustesSonido = root.Q<VisualElement>("AjustesSonido");

        _Jugar.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _ConfiguracionPartida.style.display = DisplayStyle.Flex;
            _AjustesSonido.style.display = DisplayStyle.None;
        };
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
            SceneManager.LoadSceneAsync(0);
            _AjustesSonido.style.display = DisplayStyle.None;
        };


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
