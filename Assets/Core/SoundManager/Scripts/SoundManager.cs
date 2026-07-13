using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundID
{
    // Efectos de Sonido (SFX)
    DisparoPistola,
    DisparoRifle,
    DisparoMetralleta,
    CorteSable,
    MotosierraSierra,
    DisparoEnemigo,
    MuerteEnemigo,
    DañoPersonaje,
    DañoNodo,
    CurarNodo,
    ClickBoton,
    Expancion,
    RecolectarMaterial,
    
    // Música
    MusicaMenu,
    MusicaNivel
}

public class SoundManager : MonoBehaviour
{
    [System.Serializable]
    public struct SoundEffect
    {
        public SoundID id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen;
        [Range(0.5f, 1.5f)] public float pitchBase;
        [Tooltip("Si se activa, el pitch tendrá una variación aleatoria leve en cada reproducción (+/- 0.1) para que suene orgánico.")]
        public bool variarPitch;
    }

    public static SoundManager Instance { get; private set; }

    [Header("Canales de Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Biblioteca de Sonidos")]
    [SerializeField] private List<SoundEffect> bibliotecaSonidos = new List<SoundEffect>();

    private Dictionary<SoundID, SoundEffect> tablaSonidos = new Dictionary<SoundID, SoundEffect>();

    private void Awake()
    {
        // Implementación del patrón Singleton con DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InicializarFuentesAudio();
            ConstruirDiccionario();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InicializarFuentesAudio()
    {
        // Si no se asignaron en el inspector, los creamos automáticamente
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void ConstruirDiccionario()
    {
        tablaSonidos.Clear();
        foreach (var sound in bibliotecaSonidos)
        {
            if (!tablaSonidos.ContainsKey(sound.id))
            {
                tablaSonidos.Add(sound.id, sound);
            }
            else
            {
                Debug.LogWarning($"Sonido duplicado detectado en la biblioteca: {sound.id}");
            }
        }
    }

    /// <summary>
    /// Reproduce un efecto de sonido único (SFX) por su identificador.
    /// Soporta variación de pitch aleatoria y volumen configurable.
    /// </summary>
    public void PlaySFX(SoundID id)
    {
        if (sfxSource == null) return;

        if (tablaSonidos.TryGetValue(id, out SoundEffect sound))
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"El clip de sonido para {id} está vacío en la biblioteca.");
                return;
            }

            // Aplicar variación de pitch orgánica si está habilitada
            float pitchActual = sound.pitchBase;
            if (sound.variarPitch)
            {
                pitchActual += Random.Range(-0.08f, 0.08f);
            }

            // Reproducir usando PlayOneShot para que múltiples sonidos se escuchen simultáneamente sin cortarse
            sfxSource.pitch = pitchActual;
            sfxSource.PlayOneShot(sound.clip, sound.volumen);
        }
        else
        {
            Debug.LogWarning($"El sonido {id} no está registrado en el SoundManager.");
        }
    }

    /// <summary>
    /// Reproduce una música de fondo en loop continuo.
    /// Si ya hay una música sonando, detiene la anterior.
    /// </summary>
    public void PlayMusic(SoundID id)
    {
        if (musicSource == null) return;

        if (tablaSonidos.TryGetValue(id, out SoundEffect sound))
        {
            if (sound.clip == null)
            {
                Debug.LogWarning($"El clip de música para {id} está vacío.");
                return;
            }

            // Si la pista solicitada ya está sonando, no la reiniciamos
            if (musicSource.clip == sound.clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = sound.clip;
            musicSource.volume = sound.volumen;
            musicSource.pitch = sound.pitchBase;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"La música {id} no está registrada en el SoundManager.");
        }
    }

    /// <summary>
    /// Detiene de inmediato la música de fondo que esté sonando.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
}
