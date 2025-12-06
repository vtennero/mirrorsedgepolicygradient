using UnityEngine;
using System;

/// <summary>
/// Diagnostic script to check actual Time.timeScale at runtime.
/// Add this to any GameObject in the scene to see what time scale is actually active.
/// </summary>
public class TimeScaleDiagnostic : MonoBehaviour
{
    private string debugId;
    
    void Start()
    {
        // Generate unique debug ID
        debugId = System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] ========== UNITY RUNTIME TIMESCALE CHECK ==========");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Debug ID: {debugId} (use this to filter logs)");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.timeScale at Start: {Time.timeScale}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.deltaTime: {Time.deltaTime}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Time.realtimeSinceStartup: {Time.realtimeSinceStartup}");
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] =================================================");
    }
    
    void Update()
    {
        // Log every 60 frames to see if it changes
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[TIMESCALE-DEBUG-{debugId}] Frame {Time.frameCount}: Time.timeScale = {Time.timeScale}, Time.deltaTime = {Time.deltaTime:F6}, Time.realtimeSinceStartup = {Time.realtimeSinceStartup:F2}");
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"[TIMESCALE-DEBUG-{debugId}] OnApplicationPause: {pauseStatus}, Time.timeScale = {Time.timeScale}");
    }
}

