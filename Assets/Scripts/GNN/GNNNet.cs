using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GNNNet
{
    private static int COUNTER = 0;
    public int id;

    public const int INPUT = CONFIG.INPUT;
    public const int OUTPUT = CONFIG.OUTPUT;

    public double fitnessScore;
    public List<Node> nodes;
    public List<Connect> connections;

    public bool isInfected = false;

    public GNNNet()
    {
        Init();
        BuildNodes();
        id = ++COUNTER;
    }

    private void Init()
    {
        nodes = new List<Node>();
        connections = new List<Connect>();
        fitnessScore = 0;
    }

    // Finds all nodes and initiates them
    private void BuildNodes()
    {
        nodes = new List<Node>();
        for (int i = 0; i < INPUT + OUTPUT; i++)
            nodes.Add(new Node()
            {
                ID = i,
                value = 1
            });

        HashSet<int> ids = new HashSet<int>();
        foreach (Connect c in connections)
        {
            if (c.inID >= INPUT + OUTPUT)
                ids.Add(c.inID);
            if (c.outID >= INPUT + OUTPUT)
                ids.Add(c.outID);
        }

        foreach (int i in ids)
            nodes.Add(new Node()
            {
                    ID = i,
                    value = 0
            });
    }


    private Node GetNode(int nodeID)
    {
        if (nodeID < INPUT + OUTPUT)
            return nodes[nodeID];

        // Idea for larger networks
        // start in middle
        
        for (int i = INPUT + OUTPUT; i < nodes.Count; i++)
            if (nodes[i].ID == nodeID)
                return nodes[i];

        return null;
    }

    // -- START OF: Feed forwad
    private ushort[] RunFeedStep(ushort ID)
    {
        List<ushort> connID = new List<ushort>();

        double newValue = 0;
        foreach (Connect c in connections)
        {
            if (c.isDisabled)
                continue;

            if (c.inID == ID)
                connID.Add(c.outID);
            else if (c.outID == ID)
                // Sitas varetu but nepareizi japadoma velak
                newValue += c.weight * GetNode(c.inID).value;
        }

        // Sitas varetu but nepareizi japadoma velak
        if (ID >= INPUT)
            GetNode(ID).value = System.Math.Tanh(newValue);

        return connID.ToArray();
    }

    private List<Connect> GetNodesConnections(int node)
    {
        List<Connect> nodesConnections = new List<Connect>();
        foreach(Connect c in connections)
            if (c.inID == node)
                nodesConnections.Add(c);
        return nodesConnections;
    }
    
    public bool EvaluateFeedForwardness(int node)
    {
        // Es dabuju nodes savienojumus
        List<Connect> nodesConnections = GetNodesConnections(node);
        // Es izeju tam cauri lidz beigam
        int step = 0;
        while (nodesConnections.Count > 0)
        {
            List<Connect> newConns = new List<Connect>();
            foreach (Connect n in nodesConnections)
            {
                // Ja process atgriezas atpakal uz Node ir slikti
                if (n.outID == node)
                    return false;
                newConns.AddRange(GetNodesConnections(n.outID));
            }
            nodesConnections = newConns;
            if(step++ > connections.Count)
                return false;
        }
        // Ja procesa tika novadits lidz outputam tad ir ok
        return true;
    }

    public double[] FeedForward(double[] input)
    {
        // QUICKFIX - todo find why my safe mutation failed
        if(isInfected == true)
        {
            fitnessScore = 0;
            double[] o = new double[OUTPUT];
            for (int i = INPUT; i < INPUT + OUTPUT; i++)
                o[i - INPUT] = 0;
            return o;
        }

        HashSet<ushort> activeBatch = new HashSet<ushort>();

        for (ushort i = 0; i < INPUT; i++) {
            nodes[i].value = input[i];
            activeBatch.Add(i);
        }

        int step = 0;
        do
        {
            HashSet<ushort> newBatch = new HashSet<ushort>();
            foreach(ushort i in activeBatch)
            {
                ushort[] connIDs = RunFeedStep(i);
                for (ushort c = 0; c < connIDs.Length; c++)
                    newBatch.Add(connIDs[c]);
            }
            activeBatch = newBatch;
            if(step++ > connections.Count * 2)
                break;
        } while (activeBatch.Count != 0);

        if (activeBatch.Count != 0)
        {
            Debug.LogError("<color=red>BAD CONNECTION</color>");
            isInfected = true;
        }

        double[] output = new double[OUTPUT];
        for (int i = INPUT; i < INPUT + OUTPUT; i++)
            output[i - INPUT] = nodes[i].value;
        return output;
    }
    // -- END OF: Feed forward --

    // HOW BREEDING WORKS:
    // 1. Two parents
    // 2. If both have connection then random
    // 3. If one have then take from strongest
    public GNNNet Breed(GNNNet parthner)
    {
        GNNNet child = new GNNNet();
        int[] range = UTYL.InnovRange(connections, parthner.connections);
        bool isAsFit = fitnessScore == parthner.fitnessScore;

        GNNNet[] parents = new GNNNet[] { this, parthner };
        Connect?[,] table = new Connect?[2, range[1] - range[0]];

        for (int i = 0; i < 2; i++)
            for (int c = 0; c < parents[i].connections.Count; c++)
                table[i, parents[i].connections[c].innov - range[0]] = parents[i].connections[c];

        for (int c = 0; c < range[1] - range[0]; c++)
        {
            // If both cells are empty, or parhner is less fit then skip connection
            if ((table[0, c] == null && table[1, c] == null) || (!isAsFit && table[0,c] == null ))
                continue;

            // If both have same fitness adds parthners connection
            if (table[0, c] == null)
                child.connections.Add(table[1, c].Value);
            // Adds my connection if prev is empty
            else if (table[1, c] == null)
                child.connections.Add(table[0, c].Value);
            // If both have connections select random
            else
                child.connections.Add(table[Random.Range(0, 2), c].Value);
        }

        child.BuildNodes();
        return child;
    }


    /*
     * MUTATION
     */
    public void SafeMutation(string type)
    {
        // Rollback memmory
        List<Connect> memmoryConnections = new List<Connect>(connections);
        List<Node> memmoryNodes = new List<Node>(nodes);

        int nodeID = 0;
        if (type == "link")
            nodeID = MutateLink(true);
        else
            nodeID = MutateNode();

        if (nodeID == -1)
            return;

        // Check net & rollback;
        if (!EvaluateFeedForwardness(nodeID))
        {
            connections = memmoryConnections;
            nodes = memmoryNodes;
            Debug.LogError("<color=red>BAD CONNECTION ROLLING BACK</color>");
        }
    }


    public void Mutation()
    {
     //   Debug.Log("Change weight"); 

        for (int c = 0; c < connections.Count; c++)
        {
            if (Random.Range(0, 101) < 80)
            {
                if (Random.Range(0, 101) < 90)
                    MutateWeightShift();
                else
                    MutateWeightChange();
            }
        }
    }


    // Creates link between two nodes
    public int MutateLink(bool toggle)
    {
        return MutateLink(-1, -1, -2, toggle);
    }

    private int ConnectionExists (int left, int right)
    {
        for (int i = 0; i < connections.Count; i++)
            if (
                (connections[i].inID == left && connections[i].outID == right) ||
                (connections[i].outID == left && connections[i].inID == right)
               )
                return i;
        return -1;
    }

    public int MutateLink(int left = -1, int right = -1, double weight = -2, bool toggle = false)
    {
      //  Debug.Log("Add link");

        if (left == -1 && right == -1)
        {
            do
            {
                left = Random.Range(0, nodes.Count - OUTPUT);
                right = Random.Range(INPUT, nodes.Count);
                if (left >= INPUT && left < INPUT + OUTPUT)
                    left += OUTPUT;
            } while (left == right);

            left = nodes[left].ID;
            right = nodes[right].ID;
        }

        // Checks if connection already exists
        int connExists = ConnectionExists(left, right);
        if (connExists != -1)
        {
            if (toggle)
                MutateToggleLink(connExists);
            return -1;
        }

        List<Connect> memmoryConnections = new List<Connect>(connections);

        AddLink((ushort)left, (ushort)right, weight);

        return left;
    }

    private void AddLink(ushort left, ushort right, double weight = -2)
    {
        if (weight == -2)
            weight = Random.Range(-1.0f, 1.0f);

        Innovation innov = InnovController.AddInnovation(InovType.CONNECTION, left, right);
        connections.Add(new Connect()
        {
            inID = left,
            outID = right,
            innov = innov.ID,
            weight = weight,
            isDisabled = false
        });
    }

    // Adds node at random point
    public int MutateNode()
    {
    //    Debug.Log("Add node");

        if (connections.Count == 0)
            return -1;

        // Finds connection - random
        int conID = Random.Range(0, connections.Count);

        // Finds if innovetion exists in DB
        Connect mem = connections[conID];
        Innovation innov = InnovController.AddInnovation(
            InovType.NEW_NODE,
            mem.inID,
            mem.outID
        );

        int nodeID = innov.ID;

        // Makes new connections
        MutateLink(mem.inID, nodeID, mem.weight);
        MutateLink(nodeID, mem.outID, 1);

        // Disables old connection
        MutateToggleLink(conID, true);

        // Check if node exists
        bool isIn = false;
        foreach (Node n in nodes)
            if (n.ID == innov.ID)
            {
                isIn = true;
                break;
            }

        if (isIn == false)
            nodes.Add(new Node()
            {
                ID = nodeID
            });

        return nodeID;
    }

    // Toggles connection 
    private void MutateToggleLink(int conID = -1, bool? value=null)
    {
        if (connections.Count == 0)
            return;

        if (conID < 0)
            conID = Random.Range(0, connections.Count);

        Connect mem = connections[conID];
        if (value == null)
            mem.isDisabled = !mem.isDisabled;
        else
            mem.isDisabled = value.Value;
        connections[conID] = mem;
    }

    // Shifts weight
    private void MutateWeightShift()
    {
        if (connections.Count == 0)
            return;

        int conID = Random.Range(0, connections.Count);

        Connect mem = connections[conID];
        mem.weight *= Random.Range(0, 1.0f);
        connections[conID] = mem;
    }

    // Changes weight
    private void MutateWeightChange()
    {
        if (connections.Count == 0)
            return;

        int conID = Random.Range(0, connections.Count);

        Connect mem = connections[conID];
        mem.weight *= Random.Range(-1f, 1.0f);
        connections[conID] = mem;
    }

    // Removes disabled connections and removes
    // "dead nodes" - nodes that arent connected to anything
    // Run this function every 100 gens, maybe 50
    public void CleanUp()
    {
        // Step one removes disabled connections
        for (int c = connections.Count - 1; c >= 0; c--)
            if (connections[c].isDisabled == true)
                connections.RemoveAt(c);

        // List of nodes that are connected in network
        HashSet<int> activeNodeIDs = new HashSet<int>();
        HashSet<int> activeConnIDs = new HashSet<int>();

        // Current layer nodes
        HashSet<int> currNodes = new HashSet<int>();
        for (int i = 0; i < INPUT; i++)
            currNodes.Add(i);

        // Next layer nodes
        HashSet<int> tmpNodes = new HashSet<int>();

        // Connections of current layer
        List<Connect> nodesConenctions;

        do {
            tmpNodes = new HashSet<int>();
            foreach (int cn in currNodes)
            {
                // If already in active nodes that means that
                // we are not intrested in this node, becouse we already
                // checkt it & also drunk 8 loop
                if (activeNodeIDs.Contains(cn))
                    continue;

                activeNodeIDs.Add(cn);

                // Gets nodes connections
                nodesConenctions = GetNodesConnections(cn);
                foreach (Connect c in nodesConenctions)
                {
                    tmpNodes.Add(c.outID);  // Adds next layers nodes
                    activeConnIDs.Add(c.innov);
                }
            }
            currNodes = tmpNodes; // Moves from this to next layer
        } while (currNodes.Count != 0);

        // Removes all "dead nodes"
        for (int n = nodes.Count - 1; n >= INPUT + OUTPUT; n--)
            if (!activeNodeIDs.Contains(nodes[n].ID))
                nodes.RemoveAt(n);

        // Removes all "dead connections"
        for (int n = connections.Count - 1; n >= 0; n--)
            if (!activeConnIDs.Contains(connections[n].innov))
                nodes.RemoveAt(n);
    }

}
