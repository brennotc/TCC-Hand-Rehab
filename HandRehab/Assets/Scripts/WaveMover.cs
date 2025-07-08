using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;

public class WaveMover : Strength
{
    public float speed = 5f;
    public float lifetime = 5f;
    public bool isRoot = true;
    public float waveHP = 100f;
    public float damagePerHit = 50f;
    public float knockbackForce = 5f;

    void Start()
    {
        Debug.Log($"[WAVE] Iniciando {gameObject.name} | Root: {isRoot}");

        StartCoroutine(GetMyoData("http://localhost:8000/strength"));

        if (GameObject.FindGameObjectsWithTag("Magic").Length > 100)
        {
            Debug.LogWarning("[WAVE] Limite de 100 ondas atingido — destruindo esta instância.");
            Destroy(gameObject);
            return;
        }

        if (isRoot)
        {
            SpawnWavesInAllDirections();
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

   
    void SpawnWavesInAllDirections()
    {
        Debug.Log("[WAVE] Criando ondas em 4 direções");

        Vector3[] directions = new Vector3[] {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        foreach (Vector3 dir in directions)
        {
            GameObject wave = Instantiate(gameObject, transform.position, Quaternion.LookRotation(dir));
            WaveMover mover = wave.GetComponent<WaveMover>();
            mover.isRoot = false;
            mover.waveHP = waveHP;
            wave.tag = "Magic";
            Debug.Log($"[WAVE] Criada instância na direção: {dir}");
        }
    }

    void Update()
    {
        if (!isRoot)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
            Debug.Log($"[WAVE] Movendo {gameObject.name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isRoot && other.CompareTag("Enemy"))
        {
            Debug.Log("[WAVE] Inimigo atingido pela onda!");

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                float actualDamage = Mathf.Min(damagePerHit, enemy.hp);
                enemy.hp -= actualDamage;
                waveHP -= actualDamage;

                Debug.Log($"[WAVE] Dano aplicado: {actualDamage}. HP restante da onda: {waveHP}. HP do inimigo: {enemy.hp}");

                // Rebote
                Rigidbody enemyRb = other.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                    Debug.Log("[WAVE] Rebote aplicado no inimigo.");
                }

                if (enemy.hp <= 0)
                {
                    Debug.Log("[WAVE] Inimigo destruído.");
                    Destroy(other.gameObject);
                }

                if (waveHP <= 0)
                {
                    Debug.Log("[WAVE] Onda destruída após atingir inimigos.");
                    Destroy(gameObject);
                }
            }
        }
    }
}

