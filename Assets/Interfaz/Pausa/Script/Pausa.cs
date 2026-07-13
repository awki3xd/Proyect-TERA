using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Pausa : MonoBehaviour
{
    [SerializeField] private UIDocument UiDocument;
    [SerializeField] private AudioClip AudioClik;
    [SerializeField] private AudioSource _AudioSource;


    private VisualElement _Contenedor;
    private VisualElement _PanelAjustes;
    private VisualElement _Volumen;
    private VisualElement _Tablero;

    private Button _Pausa;
    private Button _Continuar;
    private Button _Ajustes;
    private Button _Menu;
    private Button _Volver;

    private int _ContadorPausa = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var root = UiDocument.rootVisualElement;

        _Contenedor = root.Q<VisualElement>("Contenedor");
        _PanelAjustes = root.Q<VisualElement>("PanelAjustes");
        _Volumen = root.Q<VisualElement>("Volumen");
        _Tablero = root.Q<VisualElement>("Tablero");

        _Pausa = root.Q<Button>("Pausa");
        _Continuar = root.Q<Button>("Continuar");
        _Ajustes = root.Q<Button>("Ajustes");
        _Menu = root.Q<Button>("Menu");
        _Volver = root.Q<Button>("Volver");

        _Pausa.clicked += () =>
        {
            _ContadorPausa++;
           _PanelAjustes.style.display = DisplayStyle.Flex;
            _Tablero.style.display = DisplayStyle.None;
            

                Debug.Log(_ContadorPausa);
            _AudioSource.PlayOneShot(AudioClik);
            

        };

        _Continuar.clicked += () =>
        {
            Debug.Log("Continuar");
            _AudioSource.PlayOneShot(AudioClik);
            _Tablero.style.display = DisplayStyle.Flex;
            _PanelAjustes.style.display = DisplayStyle.None;
        };

       
        _Ajustes.clicked += () => 
        {
            Debug.Log("Ajustes");
            _AudioSource.PlayOneShot(AudioClik);
            _Volumen.style.display = DisplayStyle.Flex;
            _PanelAjustes.style.display = DisplayStyle.None;
        };

        _Menu.clicked += () =>
        {
            Debug.Log("Menu");
            _AudioSource.PlayOneShot(AudioClik);
            SceneManager.LoadSceneAsync(1);
        };

        _Volver.clicked += () =>
        {
            Debug.Log("Volver");
            _AudioSource.PlayOneShot(AudioClik);
            _PanelAjustes.style.display = DisplayStyle.Flex;
            _Volumen.style.display = DisplayStyle.None;
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
