



using UnityEngine;

public class TableController : MonoBehaviour
{
    public static TableController instance;
    public Table[] tables;

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        
    }

    public Table GetAvailableTable()
    {
        foreach (Table table in tables)
        {
            if (table.currentCustomer == null)
            {
                return table;
            }
        }
        return null;
    }
}