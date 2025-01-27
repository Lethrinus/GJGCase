using UnityEngine;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    [Header("Particle System Prefab")]
    public ParticleSystem particlePrefab;

    [Header("Particle LifeTime")]
    public float particleLifeTime = 1.0f;
   
    private Queue<ParticleSystem> availablePS = new Queue<ParticleSystem>();

    private List<ActivePS> inUsePS = new List<ActivePS>();
    private class ActivePS
    {
        public ParticleSystem ps;
        public float returnTime;
    }
    public ParticleSystem GetParticle(Vector3 position, Color color, float customLifetime = -1f)
    {
        float finalLifetime = (customLifetime > 0f) ? customLifetime : particleLifeTime;
        ParticleSystem ps;
        
        if (availablePS.Count > 0)
        {
            ps = availablePS.Dequeue();
            ps.gameObject.SetActive(true);
        }
        else
        {
            ps = Instantiate(particlePrefab, transform);
        }
        ps.transform.position = position;
        var main = ps.main;
        main.startColor = color;
        ps.Play();
        inUsePS.Add(new ActivePS
        {
            ps = ps,
            returnTime = Time.time + finalLifetime
        });
        return ps;
    }

    private void Update()
    {
        float currentTime = Time.time;
        for (int i = inUsePS.Count - 1; i >= 0; i--)
        {
            if (currentTime >= inUsePS[i].returnTime)
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
