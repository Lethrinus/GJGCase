using UnityEngine;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    public ParticleSettings particleSettings;
    Queue<ParticleSystem> availablePS = new Queue<ParticleSystem>();
    List<ActivePS> inUsePS = new List<ActivePS>();

    class ActivePS
    {
        public ParticleSystem ps;
        public float returnTime;
    }

    public void SpawnParticle(Vector3 position, Color color)
    {
        if (!particleSettings || !particleSettings.particlePrefab) return;
        float lifeTime = particleSettings.particleLifeTime;
        ParticleSystem ps;
        if (availablePS.Count > 0)
        {
            ps = availablePS.Dequeue();
            ps.gameObject.SetActive(true);
        }
        else
        {
            ps = Instantiate(particleSettings.particlePrefab, transform);
        }
        ps.transform.position = position;
        var main = ps.main;
        main.startColor = color;
        ps.Play();
        inUsePS.Add(new ActivePS
        {
            ps = ps,
            returnTime = Time.time + lifeTime
        });
    }

    void Update()
    {
        float now = Time.time;
        for (int i = inUsePS.Count - 1; i >= 0; i--)
        {
            if (now >= inUsePS[i].returnTime)
            {
                ParticleSystem ps = inUsePS[i].ps;
                ps.Stop();
                ps.gameObject.SetActive(false);
                availablePS.Enqueue(ps);
                inUsePS.RemoveAt(i);
            }
        }
    }
}