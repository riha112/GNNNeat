public class GNNSimulation
{
    public Agent agent;
    public GNNNet network;

    public bool isDone = false;
    private byte step = 0;

    public void SetName()
    {
        agent.name = "AGENT:" + network.id;
    }
     
    public void UpdateAgent()
    {
        if (isDone)
            return;

        if(step++ > 4)
        {
            // Removes health & kills agent
            if (--agent.health <= 0)
            {
                isDone = true;
                agent.enabled = false;
                return;
            }
            step = 0;
        }

        // Moves agents score to sim score
        if(agent.score > 0)
        {
            network.fitnessScore += agent.score;
            agent.score = 0;
        }


        // Updates nn actions
        agent.SetActions(network.FeedForward(agent.GetOutputData()));
    }
}
