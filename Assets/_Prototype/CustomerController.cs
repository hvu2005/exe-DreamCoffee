using System.Collections;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    [SerializeField] private Customer customerPrefab;

    [SerializeField] private Transform spawnPoint;

    [SerializeField] private float spawnInterval = 5f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            StartCoroutine(SpawnCustomerCoroutine());
        }
    }

    private Customer SpawnCustomer()
    {
        return Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
    }

    public IEnumerator SpawnCustomerCoroutine()
    {
            var customer = SpawnCustomer();
            yield return new WaitForSeconds(2f);

            customer.MoveToTable();
    }
}