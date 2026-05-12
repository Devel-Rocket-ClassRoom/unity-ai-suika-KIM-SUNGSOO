using System.Collections.Generic;
using UnityEngine;

// Attached to the trigger zone above the game-over line.
// Triggers game over when a settled fruit stays inside for STAY_SECONDS.
public class GameOverChecker : MonoBehaviour
{
    private const float StaySeconds     = 5f;
    private const float VelocitySettle  = 1.5f; // units/s — below this counts as "settled"

    // Key: instance ID, Value: accumulated settled-time
    private readonly Dictionary<int, float>      timers  = new();
    private readonly Dictionary<int, Rigidbody2D> bodies  = new();

    void OnTriggerEnter2D(Collider2D col)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        var fruit = col.GetComponent<Fruit>();
        if (fruit == null || !fruit.isDropped) return;

        int id = col.gameObject.GetInstanceID();
        if (!timers.ContainsKey(id))
        {
            timers[id] = 0f;
            bodies[id] = col.GetComponent<Rigidbody2D>();
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        int id = col.gameObject.GetInstanceID();
        timers.Remove(id);
        bodies.Remove(id);
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        var ids = new List<int>(timers.Keys);
        foreach (int id in ids)
        {
            // Fruit was destroyed (merged) — remove
            if (!bodies.ContainsKey(id) || bodies[id] == null)
            {
                timers.Remove(id);
                bodies.Remove(id);
                continue;
            }

            float speed = bodies[id].linearVelocity.magnitude;

            if (speed < VelocitySettle)
            {
                timers[id] += Time.deltaTime;
                if (timers[id] >= StaySeconds)
                {
                    GameManager.Instance.TriggerGameOver();
                    return;
                }
            }
            else
            {
                // Still moving — reset timer
                timers[id] = 0f;
            }
        }
    }
}
