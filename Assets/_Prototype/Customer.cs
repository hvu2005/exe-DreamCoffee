



using UnityEngine;

public class Customer : MonoBehaviour
{
    public int customerCount;

    public void MoveToTable()
    {
        Table table = TableController.instance.GetAvailableTable();
        if (table != null)
        {
            table.SeatCustomer(this);
        }
        this.transform.position = table.transform.position;
    }
}