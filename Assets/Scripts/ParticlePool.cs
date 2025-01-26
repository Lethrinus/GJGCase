using UnityEngine;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    [Header("Tek bir ParticleSystem prefab'i")]
    public ParticleSystem particlePrefab;

    [Header("Varsayılan Partikül Yaşam Süresi (sn)")]
    public float particleLifeTime = 1.0f;

    // Boşta bekleyen partiküller (kullanıma hazır)
    private Queue<ParticleSystem> availablePS = new Queue<ParticleSystem>();

    // Şu an sahnede aktif olan partiküller
    private List<ActivePS> inUsePS = new List<ActivePS>();

    /// <summary>
    /// Havuzda tuttuğumuz aktif partikülleri temsil eden küçük veri yapısı.
    /// </summary>
    private class ActivePS
    {
        public ParticleSystem ps;
        public float returnTime; // Bu zamandan sonra havuza dönecek
    }

    /// <summary>
    /// Herhangi bir partikülü havuzdan al, pozisyon ve renk ver, çalsın.
    /// İsteğe bağlı özel yaşam süresi girebilirsin; girmezsen (<= 0) particleLifeTime kullanılır.
    /// </summary>
    public ParticleSystem GetParticle(Vector3 position, Color color, float customLifetime = -1f)
    {
        float finalLifetime = (customLifetime > 0f) ? customLifetime : particleLifeTime;
        ParticleSystem ps;

        // Havuzda boşta partikül varsa kullan, yoksa yeni Instantiate
        if (availablePS.Count > 0)
        {
            ps = availablePS.Dequeue();
            ps.gameObject.SetActive(true);
        }
        else
        {
            ps = Instantiate(particlePrefab, transform);
        }

        // Konum ayarla
        ps.transform.position = position;

        // Renk ayarla (tek bir prefab kullanarak her renge uyum sağlarız)
        var main = ps.main;
        main.startColor = color;

        // Partikülü çal
        ps.Play();

        // Listemize ekleyip, ne zaman geri döneceğini kaydediyoruz
        inUsePS.Add(new ActivePS
        {
            ps = ps,
            returnTime = Time.time + finalLifetime
        });

        return ps;
    }

    private void Update()
    {
        // Her karede aktif partikülleri kontrol edelim
        // Süresi dolmuş olanları havuza iade edelim
        float currentTime = Time.time;

        // Sondan başa doğru ilerlemek, remove yaparken index sorununu engeller
        for (int i = inUsePS.Count - 1; i >= 0; i--)
        {
            if (currentTime >= inUsePS[i].returnTime)
            {
                // Süresi doldu, havuza geri at
                ParticleSystem ps = inUsePS[i].ps;

                ps.Stop(); 
                ps.gameObject.SetActive(false);

                // Havuz (Queue) sırasına ekliyoruz
                availablePS.Enqueue(ps);

                // Listeden çıkar
                inUsePS.RemoveAt(i);
            }
        }
    }
}
