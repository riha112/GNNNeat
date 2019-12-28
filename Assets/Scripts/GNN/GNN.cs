using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GNN : MonoBehaviour
{
    // Sim data
    public List<GNNSimulation> activeSimulations;

    // Net data
    ushort GEN = 0;
    private List<GNNSpiecies> spiecies;
    public List<GNNNet> unisignedNet;

    private double fullSpieciesScore;

    private void Start()
    {
        UTYL.agentPrefab = Resources.Load("prefabs/AI") as GameObject;
        UTYL.foodPrefab = Resources.Load("prefabs/Food") as GameObject;
        UTYL.boxPrefab = Resources.Load("prefabs/Box") as GameObject;

        unisignedNet = new List<GNNNet>();
        SaveObject so = SaveLoadManager.Load();
        if(so == null) // No save file new sim
        {
            DB.SendData("clean_up", new Dictionary<string, string>());

            for (int i = 0; i < CONFIG.POPULATION; i++)
            {
                GNNNet net = new GNNNet();

                for (ushort o = 0; o < 4; o++)
                    net.MutateLink();

                net.connections = net.connections.OrderBy(x => x.innov).ToList();
                unisignedNet.Add(net);
            }
        }
        else
        {
            unisignedNet.AddRange(so.networks);
            GEN = so.GEN;
            InnovController.innovations = new List<Innovation>(so.innovations);
            unisignedNet.RemoveAt(0);
            InnovController.innov = so.innovID;
        }

        UTYL.InitFood();
        InitGeneration();
    }

    private void Update()
    {
        UTYL.KeepFood();
    }

    void InitGeneration()
    {
        // Step 0: Cleanup after prev GEN
        CleanUp();

        // STEP 0.25 Backup
        SaveLoadManager.Save(GEN, unisignedNet);

        // STEP 0.5: Init  food
        UTYL.InitBox();

        // Step 1: Sort unisigned networks into spiecies
        // Step 2: Build simulations
        SpieciefyNetworks();

        // Step 3: Run simmulations
        StartCoroutine("UpdateSimulation");
    }

    private void CleanUp()
    {
        fullSpieciesScore = 0;

      //  if(UTYL.food != null)
      //      foreach (GameObject f in UTYL.food)
      //          Destroy(f);

        // Reset fitness scores of spiecies & networks
        if (spiecies != null)
            foreach(GNNSpiecies spiecie in spiecies)
                spiecie.CleanUp();

        // Resets fitness score of networks
        // Every 50 gens simplyfies network
        if (unisignedNet != null)
        {
           // bool doDeepClean = GEN % 50 == 0;
            foreach (GNNNet net in unisignedNet)
            {
               // if (doDeepClean)
               //     net.CleanUp();
                net.fitnessScore = 0;
            }
        }

        // Remove all old agents
        if (activeSimulations != null)
            foreach (GNNSimulation sim in activeSimulations)
                Destroy(sim.agent.gameObject);
    }

    private void SpieciefyNetworks()
    {
        activeSimulations = new List<GNNSimulation>();
        spiecies = new List<GNNSpiecies>();

        foreach(GNNNet net in unisignedNet)
        {
            activeSimulations.Add(new GNNSimulation()
            {
                agent = UTYL.InitAgent(),
                network = net
            });

            activeSimulations.Last().SetName();

            bool isNewSpiecies = true;
            for(int i = 0; i < spiecies.Count; i++)
            {
                double dist = UTYL.GetDistance(spiecies[i].head, net);
                //Debug.Log("Dist:" + dist);
                if (dist <= 3) 
                {
                    spiecies[i].family.Add(net);
                    isNewSpiecies = false;
                    break;
                }
            }

            if (isNewSpiecies)
                spiecies.Add(new GNNSpiecies(net));
        }

        for (int i = spiecies.Count - 1; i >= 0; i--)
            if (spiecies[i].family.Count == 0)
                spiecies.RemoveAt(i);
    }

    IEnumerator UpdateSimulation()
    {
        while(true)
        {
            int doneNetworkCount = 0;
            foreach(GNNSimulation sim in activeSimulations)
            {
                if (!sim.isDone) sim.UpdateAgent();
                else doneNetworkCount++;
            }

            if (doneNetworkCount == activeSimulations.Count)
                EndGeneration();

            yield return new WaitForSeconds(0.5f);
        }
    }

    void EndGeneration()
    {
        StopAllCoroutines();

        // Step 0: Evaluate simulations
        EvaluateNetworks();

        // Step 1: Populate simulations
        PopulateSimulations();

        Debug.Log($"<color=green>GEN</color>:{GEN}[spiecies:{spiecies.Count}, topScore:{spiecies[0].score}], topFitnessScore:{spiecies[0].family[0].fitnessScore}]");

        //Backup
        InnovController.BackUp();
        BackUp();

        // Step 2: Run next Simulation
        GEN++;
        InitGeneration();
    }

    private void EvaluateNetworks()
    {
        // Orders networks by score;
        foreach (GNNSpiecies spiecie in spiecies)
        {
            spiecie.UpdateScore();
            spiecie.SortNetsByScore();
        }

        // Orders spiecies by score
        spiecies = spiecies.OrderByDescending(x => x.score).ToList();

    }

    private void PopulateSimulations()
    {
        unisignedNet = new List<GNNNet>();
        foreach (GNNSpiecies spiecie in spiecies)
        {
            int keep = (int)(spiecie.family.Count * 0.2f);
            if (keep == 0)
                keep = 1;

            for(int i = 0; i < keep; i++)
                unisignedNet.Add(spiecie.family[i]);
        }

        GNNNet[] parents = new GNNNet[2];
        GNNNet child;

        int freeSpaces = CONFIG.POPULATION - unisignedNet.Count;
        for (int i = 0; i < freeSpaces; i++)
        {
            GNNSpiecies spiecie = GetRandomSpiecieByFitness();

            parents[0] = spiecie.GetRandomNetByFitness();
            parents[1] = spiecie.GetRandomNetByFitness();

            if(parents[0].fitnessScore < parents[1].fitnessScore)
            {
                GNNNet tmp = parents[0];
                parents[0] = parents[1];
                parents[1] = tmp;
            }

            child = parents[0].Breed(parents[1]);

            if (Random.Range(0, 101) < 50) child.Mutation();
            if (Random.Range(0, 101) < 10) child.SafeMutation("node");
            if (Random.Range(0, 101) < 10) child.SafeMutation("link");

            child.connections = child.connections.OrderBy(x => x.innov).ToList();
            unisignedNet.Add(child);
        }
    }

    private GNNSpiecies GetRandomSpiecieByFitness()
    {
        GNNSpiecies famTree = spiecies[0];
        float num = Random.Range(0, 1.0f);

        if (fullSpieciesScore == 0)
            foreach (GNNSpiecies sp in spiecies)
                fullSpieciesScore += sp.score;

        double cumulative = 0;
        foreach (GNNSpiecies sp in spiecies)
        {
            cumulative += sp.score / fullSpieciesScore;
            if (num < cumulative)
            {
                famTree = sp;
                break;
            }
        }
        return famTree;
    }

    private void BackUp()
    {

        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("id", GEN.ToString());
        data.Add("score", spiecies[0].score.ToString());
        data.Add("spiecies_count", spiecies.Count.ToString());
        DB.SendData("gen", data);
        return;

        Dictionary<string, string> netData, connData;
        DB.SendData("clean_net_innov", new Dictionary<string, string>());

        foreach (GNNSpiecies spiecie in spiecies)
        {
            foreach(GNNNet net in spiecie.family)
            {
                netData = new Dictionary<string, string>();
                netData.Add("id", net.id.ToString());
                netData.Add("gen", GEN.ToString());
                netData.Add("score", net.fitnessScore.ToString());
                DB.SendData("net", netData);

                foreach(Connect conn in net.connections)
                {
                    connData = new Dictionary<string, string>();
                    connData.Add("net_id", net.id.ToString());
                    connData.Add("innov_id", conn.innov.ToString());
                    connData.Add("weight", conn.weight.ToString());
                    connData.Add("is_disabled", conn.isDisabled ? "1" : "0");
                    DB.SendData("net_innov", connData);
                }

            }
        }
    }

}
