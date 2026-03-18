using UnityEngine;

/// <summary>
/// Менеджер звуков для игры.
/// Управляет воспроизведением звуковых эффектов: взятие, установка, отмена, сгорание линий.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Источник для воспроизведения звуковых эффектов (SFX)")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip grabClip;      // При взятии фигуры
    public AudioClip dropClip;      // При успешной установке на поле
    public AudioClip returnClip;    // При неудачной установке (возврат)
    public AudioClip clearClip;     // При взрыве линий
    public AudioClip gameOverClip;  // При проигрыше

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        // Автоматически добавляем AudioSource, если еще не добавлен
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Воспроизводит аудиоклип с определенным питчем (высотой тона)
    /// </summary>
    private void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;
        
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayGrab() => PlaySFX(grabClip, 1f);
    public void PlayDrop() => PlaySFX(dropClip, 1f);
    public void PlayReturn() => PlaySFX(returnClip, 1f);
    
    /// <summary>
    /// Звук уничтожения линий. Чем больше комбо — тем выше питч!
    /// </summary>
    public void PlayClear(int comboCount) 
    {
        // 1 линия = 1.0f pitch, 2 линии = 1.1f pitch, 3 линии = 1.2f pitch, и т.д.
        float pitch = 1f + (comboCount - 1) * 0.1f;
        PlaySFX(clearClip, pitch);
    }
    
    public void PlayGameOver() => PlaySFX(gameOverClip, 1f);
}
