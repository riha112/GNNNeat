using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GNNSpiecies
{
    public double score;
    public List<GNNNet> family;
    public GNNNet head;

    public GNNSpiecies(GNNNet head)
    {
        family = new List<GNNNet>();
        family.Add(head);
        this.head = head;
    }

    public void UpdateScore()
    {
        score = 0;
        foreach (GNNNet net in family)
            score += net.fitnessScore;
        score /= family.Count;
    }

    public void CleanUp()
    {
        head = family[Random.Range(0, family.Count)];
        score = 0;

        family.Clear();

        head.fitnessScore = 0;

    }

    double[] propobilty;
    public void SortNetsByScore()
    {
        family = family.OrderByDescending(x => x.fitnessScore).ToList();

        propobilty = new double[10];
        double fullScore = 0;
        for (int i = 0; i < family.Count && i < 10; i++)
            fullScore += family[i].fitnessScore;

        for (int i = 0; i < family.Count && i < 10; i++)
            propobilty[i] = family[i].fitnessScore / fullScore;
    }


    public GNNNet GetRandomNetByFitness()
    {
        GNNNet net = family[0];

        float random = Random.Range(0, 1.0f);

        double cumulative = 0;
        for(int i = 0; i < family.Count && i < 50; i++)
        {
            cumulative += propobilty[i];
            if(random < cumulative)
            {
                net = family[i];
                break;
            }
        }
        return net;
    }
}
