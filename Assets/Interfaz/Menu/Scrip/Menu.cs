using UnityEngine;
using UnityEngine.UIElements;

public class Menu : MonoBehaviour
{
    [SerializeField] private UIDocument _UIDocument;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AudioSource _audioSource;

    private VisualElement _CreditosImg;

    private Button _Jugar;
    private Button _Historia;
    private Button _Ajustes;
    private Button _Creditos;
    private Button _Salir;
    private Button _Salida;

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

        _CreditosImg = root.Q<VisualElement>("CreditosFoto");

        _Jugar.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
        };
        _Historia.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
        };
        _Ajustes.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
        };
        _Creditos.clicked += () => 
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.Flex;

        };
        _Salir.clicked += () => 
        {
            
            _audioSource.PlayOneShot(_clip);
        };

        _Salida.clicked += () =>
        {
            _audioSource.PlayOneShot(_clip);
            _CreditosImg.style.display = DisplayStyle.None;
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
