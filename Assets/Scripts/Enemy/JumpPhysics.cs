using UnityEngine;

public static class JumpPhysics
{
    private const int MaxSteps = 400;

    public static float ApexHeight(float v0, float gravity, float damping, float dt)
    {
        float y = 0f, v = v0, max = 0f;
        for (int i = 0; i < MaxSteps; i++)
        {
            v -= gravity * dt;
            v /= 1f + damping * dt;
            y += v * dt;
            if (y > max) max = y;
            if (v <= 0f) break;
        }
        return max;
    }

    // Time at which a body launched upward at v0 comes back DOWN through relativeHeight
    // (measured from the launch point). Returns -1 if it never gets there.
    public static float TimeToDescendThrough(float v0, float relativeHeight, float gravity, float damping, float dt)
    {
        float y = 0f, v = v0, t = 0f, prevY = 0f;
        for (int i = 0; i < MaxSteps; i++)
        {
            v -= gravity * dt;
            v /= 1f + damping * dt;
            y += v * dt;
            t += dt;

            if (prevY >= relativeHeight && y < relativeHeight && v < 0f)
                return t - dt + dt * (prevY - relativeHeight) / (prevY - y);

            prevY = y;
        }
        return -1f;
    }
}