class Simulatable//<Bot>
{
    //public Bot bot;
    public GNNNet network;
    public bool isDone;


    // Call when using GNN
    public void Run()
    {
        if (isDone)
            return;

        UpdateSimulation();
    }


    // Define functionality in child classes
    public void UpdateSimulation() { }

}
