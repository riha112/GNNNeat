using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    // 16 Directions around agent
    public static readonly int DIR_COUNT = 5;
    private static readonly Vector2[] sensorDir = new Vector2[]
    {
        Vector2.up, //Vector2.left, Vector2.right,
        //new Vector2(0.5f, 0.5f),   new Vector2(-0.5f, 0.5f),
        //new Vector2(0.5f, 0.25f),   new Vector2(-0.5f, 0.25f),
        new Vector2(0.25f, 0.5f),   new Vector2(-0.25f, 0.5f),
        new Vector2(0.125f, 0.5f),   new Vector2(-0.125f, 0.5f),
    };

    public int movSpeed;
    public int rotSpeed;

    public int health = 100;
    public short[] action = { 0, 0 };

    public int score = 0;

    public double[] GetOutputData()
    {
        double[] input = new double[CONFIG.INPUT];

        // Previus actions
        input[0] = action[0] == 0 ? 0.01F : action[0];
        input[1] = action[1] == 0 ? 0.01F : action[1];

        // HP stats
        input[2] = health / 100.0;

        // Rot status
        input[3] = transform.localEulerAngles.z / 360.0;

        // Sensor information
        double[,] sd = GetSensorData();
        for (int i = 0; i < DIR_COUNT; i++)
        {
            input[4 + i * 2] = sd[0, i];
            input[5 + i * 2] = sd[1, i];
        }
        return input;
    }

    private void OnDrawGizmos() 
    {
        if(health > 0)
            foreach (var dir in sensorDir)
                Gizmos.DrawRay(new Ray(transform.position, transform.TransformDirection(dir)));
    }

    private double[,] GetSensorData()
    {
        double[,] output = new double[2, DIR_COUNT];

        RaycastHit2D hit;
        for (int i = 0; i < DIR_COUNT; i++)
        {
            Vector3 to = transform.TransformDirection(sensorDir[i]);
            hit = Physics2D.Raycast(transform.position, to);

            output[0, i] = -1;
            output[1, i] = 1;

            if(hit.collider != null)
            {
                string hitTag = hit.collider.tag;
                output[1, i] = hit.distance / 430.0;

                if (hitTag == "Food")
                {
                    output[0, i] = 1;
                    if(hit.distance < 2)
                    {
                        UTYL.RemoveFood(hit.collider.gameObject);
                        Destroy(hit.collider.gameObject);
                        score++;
                        health += 10;
                        if (health > 100)
                            health = 100;
                    }
                }
                else if (hitTag == "AI")
                {
                    output[0, i] = 0.5;
                }
                else if (hitTag == "Box")
                {
                    output[0, i] = -0.5;
                }
            }
        }
        return output;
    }

    public void SetActions(double[] input)
    {
        action[0] = (short)Mathf.RoundToInt((float)input[0]);
        action[1] = (short)Mathf.RoundToInt((float)input[1]);
    }

    public void Update()
    {
        float td = Time.deltaTime;

        // Moves
        if (action[0] != 0)
        {
            // Moves forward
            transform.position += transform.up * td * movSpeed * action[0];

            // Limits movement
            Vector3 atp = transform.position;
            transform.position = new Vector3(
                Mathf.Clamp(atp.x, -CONFIG.HALF_SIZE, CONFIG.HALF_SIZE),
                Mathf.Clamp(atp.y, -CONFIG.HALF_SIZE, CONFIG.HALF_SIZE),
                0
            );
        }

        // Rotates
        if (action[1] != 0)
            transform.Rotate(Vector3.forward * td * rotSpeed * action[1]);
    }
}
