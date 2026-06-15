




using UnityEngine;

public class Table : MonoBehaviour
{
    public Customer currentCustomer;

    public void SeatCustomer(Customer customer)
    {
        currentCustomer = customer;
    }
}