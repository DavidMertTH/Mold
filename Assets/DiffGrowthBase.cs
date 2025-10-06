using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class DiffGrowthBase : MonoBehaviour
{
    List<Particle> particles = new List<Particle>();
    [Range(10, 5000)] [SerializeField] public int maxParticles = 100;
    [Range(0, 100)] [SerializeField] public int debugParticle = 0;
    [Range(0.1f, 2)] [SerializeField] public float splitValue = 0.9f;
    [Range(0.1f, 2)] [SerializeField] public float preferedDistance = 1f;
    [Range(0.1f, 5)] [SerializeField] public float lookAroundDistance = 3f;
    public bool reset = false;
    private HashMap<Particle> _map;


    void Start()
    {
        particles = InitWithCircle(10);
        _map = new HashMap<Particle>(3f);
    }

    private List<Particle> InitWithCircle(int particleAmount)
    {
        List<Particle> p = new List<Particle>();
        float pDistance = Mathf.PI * 2 / particleAmount;
        for (int i = 0; i < particleAmount; i++)
        {
            Particle particle = new Particle();

            particle.Position = new Vector3(Mathf.Sin(i * pDistance), Mathf.Cos(i * pDistance), 0) *
                                (1 + Random.value / 5);
            p.Add(particle);
        }

        return p;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            int hashValue = _map.GetHashIndex(particles[i].Position);
            float saturation = (float)hashValue / (float)_map.MaxCells;
            Gizmos.color = new Color(1 - 1 * saturation, 1, 1 * saturation);

            Gizmos.DrawSphere(particles[i].Position, 0.04f);
            Gizmos.DrawLine(particles[i].Position, particles[(i + 1) % particles.Count].Position);
        }

        if (particles == null || particles.Count == 0) return;
        List<Particle> surroundingParticles = _map.GetSurroundingItems(particles[debugParticle].Position);

        for (int i = 0; i < surroundingParticles.Count; i++)
        {
            Debug.DrawLine(particles[debugParticle].Position, surroundingParticles[i].Position, Color.red);
        }
    }

    void Update()
    {
        UpdatePositions();
        if (reset)
        {
            reset = false;
            Reset();
        }
    }

    private void Reset()
    {
        particles.ForEach(particle => _map.RemoveObject(particle, particle.Position));
        particles.Clear();
        particles = InitWithCircle(10);
    }

    private void UpdatePositions()
    {
        List<Particle>[] surroundingParticles = new List<Particle>[particles.Count];
        int maxSurroundingParticles = 0;
        for (int i = 0; i < particles.Count; i++)
        {
            surroundingParticles[i] = _map.GetSurroundingItems(particles[i].Position);
            if (surroundingParticles[i].Count > maxSurroundingParticles)
                maxSurroundingParticles = surroundingParticles[i].Count;
        }

        NativeArray<Vector3> results = new NativeArray<Vector3>(particles.Count(), Allocator.TempJob);
        NativeArray<Vector3> positions =
            new NativeArray<Vector3>(particles.Select(particle => particle.Position).ToArray(), Allocator.TempJob);

        Vector3[] sur = new Vector3[maxSurroundingParticles * particles.Count];
        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = 0; j < surroundingParticles[i].Count; j++)
            {
                sur[i * maxSurroundingParticles + j] = surroundingParticles[i][j].Position;
            }
        }

        NativeArray<Vector3> surroundingPositions = new NativeArray<Vector3>(sur, Allocator.TempJob);


        PushJob job = new PushJob()
        {
            LookAroundDistance = lookAroundDistance,
            Result = results,
            SurroundingPoints = surroundingPositions,
            Position = positions,
            Offset = maxSurroundingParticles
        };
        JobHandle handle = job.Schedule(surroundingParticles.Length, 64);
        handle.Complete();

        Vector3[] resultsManaged = job.Result.ToArray();

        for (int i = 0; i < particles.Count(); i++)
        {
            _map.RemoveObject(particles[i], particles[i].Position);
            particles[i].Position += job.Result[i] * Time.deltaTime;
        }

        results.Dispose();
        positions.Dispose();
        surroundingPositions.Dispose();

        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Position += Time.deltaTime * PullToNeighbor(i, preferedDistance);
            particles[i].Position += Time.deltaTime * KeepDistance(i, surroundingParticles[i]);
            // DrawArrow.ForDebug(particles[i].Position, KeepDistance(i));
            _map.AddObject(particles[i], particles[i].Position);
        }

        for (int i = 0; i < particles.Count; i++)
        {
            if (particles.Count < maxParticles) Split(i, splitValue);
        }
    }

    private Vector3 KeepDistance(int particleIndex, List<Particle> surroundingParticles)
    {
        Vector3 result = Vector3.zero;
        // List<Particle> surroundingParticles = _map.GetSurroundingItems(particles[particleIndex].Position);


        return result;
    }

    private void Split(int particleIndex, float maxDistance)
    {
        Particle neighborRight = particles[(particleIndex + 1) % particles.Count];
        Particle particle = particles[particleIndex];

        Vector3 rightToMiddle = particle.Position - neighborRight.Position;
        float distanceRight = rightToMiddle.magnitude;

        if (distanceRight < maxDistance) return;
        Particle newParticle = new Particle();
        newParticle.Position = neighborRight.Position + 0.5f * rightToMiddle;

        particles.Insert(particleIndex + 1, newParticle);
        _map.AddObject(newParticle, newParticle.Position);
    }

    private Vector3 PullToNeighbor(int particleIndex, float idealDistance)
    {
        Particle neighborLeft = particles[Mod(particleIndex - 1, particles.Count)];
        Particle neighborRight = particles[(particleIndex + 1) % particles.Count];
        Particle particle = particles[particleIndex];

        Vector3 leftToMiddle = particle.Position - neighborLeft.Position;
        Vector3 rightToMiddle = particle.Position - neighborRight.Position;

        float distanceLeft = leftToMiddle.magnitude;
        float distanceRight = rightToMiddle.magnitude;

        distanceLeft = idealDistance - distanceLeft;
        distanceRight = idealDistance - distanceRight;

        leftToMiddle *= distanceLeft;
        rightToMiddle *= distanceRight;
        return leftToMiddle + rightToMiddle;
    }

    [BurstCompile]
    public struct PushJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<Vector3> Result;

        [ReadOnly] public float LookAroundDistance;
        [ReadOnly] public int Offset;

        [ReadOnly] public NativeArray<Vector3> SurroundingPoints; // Länge = Position.Length * Offset
        [ReadOnly] public NativeArray<Vector3> Position;

        public void Execute(int index)
        {
            int baseIndex = index * Offset;
            if (baseIndex >= SurroundingPoints.Length) return;

            Vector3 currentPosition = Position[index];
            Vector3 sum = Vector3.zero;

            float radius = LookAroundDistance;
            float radiusSquared = radius * radius;

            for (int i = 0; i < Offset; i++)
            {
                int si = baseIndex + i;
                if (si >= SurroundingPoints.Length) break;

                Vector3 neighbor = SurroundingPoints[si];

                // Padding überspringen
                if (neighbor.sqrMagnitude < 1e-12f) continue;

                Vector3 direction = currentPosition - neighbor;
                float distanceSquared = direction.sqrMagnitude;
                if (distanceSquared > radiusSquared || distanceSquared <= 1e-12f) continue;

                float distance = Mathf.Sqrt(distanceSquared);
                float diff = radius - distance;
                float scale = (diff * diff * diff) / distance;

                sum += direction * (scale * 0.1f);
            }

            Result[index] = sum;
        }
    }


    private int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}