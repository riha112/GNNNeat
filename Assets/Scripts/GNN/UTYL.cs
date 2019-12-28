
using System.Collections.Generic;
using UnityEngine;

public class UTYL
{
    public static GameObject agentPrefab;
    public static GameObject foodPrefab;
    public static GameObject boxPrefab;

    public static List<GameObject> food;
    public static List<GameObject> boxes = new List<GameObject>();


    private static List<Vector2> foodGrid;
    private static List<Vector2> agentGrid;
    private static List<Vector2> boxGrid;

    private static void InitFoodGrid()
    {
        foodGrid = new List<Vector2>();
        for (int x = 1; x < CONFIG.WORLD_SIZE / 5 -1; x++)
            for (int y = 1; y < CONFIG.WORLD_SIZE / 5 -1; y++)
                foodGrid.Add(new Vector2(x * 5 - CONFIG.HALF_SIZE, y * 5 - CONFIG.HALF_SIZE));
    }

    private static void InitBoxGrid()
    {
        boxGrid = new List<Vector2>();
        for (int x = 1; x < CONFIG.WORLD_SIZE / 5 - 1; x++)
            for (int y = 1; y < CONFIG.WORLD_SIZE / 5 - 1; y++)
                boxGrid.Add(new Vector2(x * 5 - CONFIG.HALF_SIZE + 1, y * 5 - CONFIG.HALF_SIZE + 1));
    }

    private static void InitAgentGrid()
    {
        agentGrid = new List<Vector2>();
        for (int x = 0; x < CONFIG.WORLD_SIZE / 5 - 1; x++)
            for (int y = 0; y < CONFIG.WORLD_SIZE / 5 - 1; y++)
                agentGrid.Add(new Vector2(x * 5 - CONFIG.HALF_SIZE + 2.5f, y * 5 - CONFIG.HALF_SIZE + 2.5f));
    }

    public static void InitFood()
    {
        InitFoodGrid();

        food = new List<GameObject>();
        for (int i = 0; i < CONFIG.FOOD_COUNT && i < foodGrid.Count; i++)
            InitOneFood();
    }

    private static void InitOneFood()
    {
        GameObject foodGO = GameObject.Instantiate(foodPrefab);

        int id = Random.Range(0, foodGrid.Count);
        foodGO.transform.position = foodGrid[id];
        foodGrid.RemoveAt(id);
        food.Add(foodGO);

        foodGO.transform.Rotate(Vector3.forward * Random.Range(0, 360));
    }

    public static void KeepFood()
    {
        if(food.Count < CONFIG.FOOD_COUNT)
        {
            if (foodGrid.Count == 0)
                InitFoodGrid();

            InitOneFood();
        }
    }

    public static void RemoveFood(GameObject foodGO)
    {
        food.Remove(foodGO);
    }

    public static void InitBox()
    {
        if(boxes.Count == 0)
            for(int i = 0; i < CONFIG.BOX_COUNT; i++)
                boxes.Add(GameObject.Instantiate(boxPrefab));

        InitBoxGrid();

        foreach(GameObject box in boxes)
        {
            int id = Random.Range(0, foodGrid.Count);

            box.transform.position = boxGrid[id];
            box.transform.Rotate(Vector3.forward * Random.Range(0, 360));

            boxGrid.RemoveAt(id);
        }
    }


    public static Agent InitAgent()
    {
        if (agentGrid == null || agentGrid.Count == 0)
            InitAgentGrid();

        GameObject agentGO = GameObject.Instantiate(agentPrefab);

        int id = Random.Range(0, agentGrid.Count);
        agentGO.transform.position = agentGrid[id];
        agentGrid.RemoveAt(id);

        agentGO.transform.Rotate(Vector3.forward * Random.Range(0, 360));
        return agentGO.GetComponent<Agent>();
    }







    public static int[] InnovRange(List<Connect> left, List<Connect> right)
    {
        if (left.Count == 0 && right.Count == 0)
            return new int[] { 0, 0 };

        if (left.Count == 0)
            return new int[] { right[0].innov, right[right.Count - 1].innov + 1 };

        if (right.Count == 0)
            return new int[] { left[0].innov, left[left.Count - 1].innov + 1 };

        int smallest = right[0].innov;
        if (smallest > left[0].innov)
            smallest = left[0].innov;

        int largest = right[right.Count - 1].innov;
        if (largest < left[left.Count - 1].innov)
            largest = left[left.Count - 1].innov;

        return new int[] { smallest,  largest + 1 };
    }

    // δ = E/N + D/N + W/M | + U/N, where U - is count of difrence in disabled
    public static double GetDistance(GNNNet a, GNNNet b)
    {
        if (a.connections.Count == 0 && b.connections.Count == 0)
            return 0;

        double W;
        int M, D, E, N, U;

        M = 1;
        W = D = E = U = 0;

        N = a.connections.Count;
        if (N < b.connections.Count)
            N = b.connections.Count;

        if (N < 10)
            N = 3;

        GNNNet[] nets = new GNNNet[] { a, b };
        int[] range = UTYL.InnovRange(a.connections, b.connections);
        Connect?[,] table = new Connect?[2, range[1] - range[0]];

        for (int i = 0; i < 2; i++)
            for (int c = 0; c < nets[i].connections.Count; c++)
                table[i, nets[i].connections[c].innov - range[0]] = nets[i].connections[c];


        bool isDisjoint = true;
        for (int c = range[1] - range[0] - 1; c >= 0; c--)
        {
            // End of
            if (table[0, c] != null || table[1, c] != null)
                isDisjoint = false;

            if (isDisjoint)
            {
                D++;
                continue;
            }

            if(table[0,c] == null && table[1,c] != null || table[0, c] != null && table[1, c] == null)
            {
                E++;
                continue;
            }

            if(table[0, c] != null && table[1, c] != null)
            {
                if(table[0, c].Value.isDisabled != table[1, c].Value.isDisabled)
                    U++;

                M++;
                W += System.Math.Abs(table[0, c].Value.weight - table[1, c].Value.weight);
            }
        }
        return (double)E / (double)N + 
               (double)D / (double)N +
               (double)U / (double)N +
                       W /         M;
    }
}
