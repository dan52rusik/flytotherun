using UnityEngine;

public class MatrixBurstEffect : MonoBehaviour
{
    private const int shardCount = 6;
    private const float duration = 0.7f;

    private Transform[] shards;
    private Vector3[] directions;
    private Material[] materials;
    private float timer;

    public static void Spawn(Vector3 position)
    {
        GameObject effectObject = new GameObject("MatrixBurstEffect");
        effectObject.transform.position = position;
        MatrixBurstEffect effect = effectObject.AddComponent<MatrixBurstEffect>();
        effect.Build();
    }

    private void Build()
    {
        shards = new Transform[shardCount];
        directions = new Vector3[shardCount];
        materials = new Material[shardCount];

        for (int i = 0; i < shardCount; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shard.name = $"DigitShard_{i}";
            shard.transform.SetParent(transform, false);
            shard.transform.localScale = Vector3.one * Random.Range(0.16f, 0.3f);
            shard.transform.localPosition = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.12f, 0.12f), -0.1f);

            Collider collider = shard.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = shard.GetComponent<Renderer>();
            Material material = new Material(renderer.material);
            renderer.material = material;
            materials[i] = material;

            MatrixTheme.ConfigureMaterial(material, MatrixSurfaceType.Preview, Random.Range(0f, 1f));

            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.green;
            color.a = 0.85f;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            float angle = (360f / shardCount) * i + Random.Range(-18f, 18f);
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            directions[i] = direction * Random.Range(0.35f, 0.9f) + Vector3.down * 0.3f;
            shards[i] = shard.transform;
        }
    }

    private void Update()
    {
        if (shards == null)
            return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        for (int i = 0; i < shards.Length; i++)
        {
            if (shards[i] == null)
                continue;

            shards[i].localPosition += directions[i] * Time.deltaTime;
            shards[i].Rotate(0f, 0f, 90f * Time.deltaTime);

            if (materials[i] != null)
            {
                Color color = materials[i].HasProperty("_BaseColor") ? materials[i].GetColor("_BaseColor") : Color.green;
                color.a = Mathf.Lerp(0.85f, 0f, t);
                if (materials[i].HasProperty("_BaseColor"))
                    materials[i].SetColor("_BaseColor", color);
                if (materials[i].HasProperty("_Color"))
                    materials[i].SetColor("_Color", color);
            }
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
