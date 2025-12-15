using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Helper class to log training data to JSON files in the results directory.
/// Tracks per-episode data, stamina trajectories, reward components, and metadata.
/// </summary>
public class TrainingLogger
{
    private static TrainingLogger instance;
    private string resultsDir;
    private string runLogsDir;
    private bool initialized = false;
    
    // Episode data tracking
    private List<EpisodeData> episodeDataList = new List<EpisodeData>();
    
    // Stamina trajectory tracking (sampled to avoid huge files)
    private List<StaminaDataPoint> currentEpisodeStamina = new List<StaminaDataPoint>();
    private int staminaSampleInterval = 10; // Sample every 10 steps to reduce file size
    
    // Reward component tracking (per episode)
    private RewardComponents currentEpisodeRewards = new RewardComponents();
    private List<RewardComponents> rewardComponentsList = new List<RewardComponents>();
    
    // Metadata
    private TrainingMetadata metadata = new TrainingMetadata();
    
    private TrainingLogger() { }
    
    public static TrainingLogger Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TrainingLogger();
            }
            return instance;
        }
    }
    
    /// <summary>
    /// Check if the logger has been initialized.
    /// </summary>
    public bool IsInitialized()
    {
        return initialized;
    }
    
    /// <summary>
    /// Initialize the logger with the results directory path.
    /// Should be called once at the start of training.
    /// Only scans directories if not already initialized (performance optimization).
    /// </summary>
    public void Initialize()
    {
        // If already initialized, skip expensive directory scanning
        if (initialized && !string.IsNullOrEmpty(runLogsDir) && Directory.Exists(runLogsDir))
        {
            return;
        }
        
        // Find results directory (same logic as ParkourAgent)
        // Application.dataPath is Assets/, so going up one level gives src/, then results/ is directly under src/
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        resultsDir = Path.Combine(projectRoot, "results");
        
        if (!Directory.Exists(resultsDir))
        {
            Debug.LogError($"[TrainingLogger] Results directory does not exist: {resultsDir}");
            return;
        }
        
        // Find the most recent training_* directory
        var trainingDirs = Directory.GetDirectories(resultsDir, "training_*");
        if (trainingDirs.Length == 0)
        {
            Debug.LogWarning($"[TrainingLogger] No training_* directories found in {resultsDir}. Will retry on next episode.");
            return;
        }
        
        // Sort by last write time, get most recent
        var sortedDirs = trainingDirs.OrderByDescending(d => Directory.GetLastWriteTime(d)).ToArray();
        string latestTrainingDir = sortedDirs[0];
        string newRunLogsDir = Path.Combine(latestTrainingDir, "run_logs");
        
        // Create run_logs directory if it doesn't exist
        if (!Directory.Exists(newRunLogsDir))
        {
            try
            {
                Directory.CreateDirectory(newRunLogsDir);
                Debug.Log($"[TrainingLogger] Created run_logs directory: {newRunLogsDir}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TrainingLogger] Failed to create run_logs directory: {e.Message}");
                return;
            }
        }
        
        // Update runLogsDir if it changed (or if not initialized yet)
        if (!initialized || runLogsDir != newRunLogsDir)
        {
            runLogsDir = newRunLogsDir;
            initialized = true;
            Debug.Log($"[TrainingLogger] ✓ Initialized. Logging to: {runLogsDir}");
            Debug.Log($"[TrainingLogger] Training directory: {latestTrainingDir}");
        }
    }
    
    /// <summary>
    /// Set metadata (style frequency, etc.) - should be called once at initialization
    /// </summary>
    public void SetMetadata(float styleEpisodeFrequency)
    {
        metadata.styleEpisodeFrequency = styleEpisodeFrequency;
        metadata.trainingStartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteMetadata();
    }
    
    /// <summary>
    /// Start tracking a new episode
    /// </summary>
    public void StartEpisode(int episodeNumber, int stepCount)
    {
        if (!initialized)
        {
            Debug.LogWarning($"[TrainingLogger] StartEpisode called but not initialized. Episode {episodeNumber} will not be tracked.");
            return;
        }
        
        currentEpisodeRewards = new RewardComponents
        {
            episodeNumber = episodeNumber,
            stepCount = stepCount
        };
        currentEpisodeStamina.Clear();
    }
    
    /// <summary>
    /// Record stamina at current timestep (sampled to reduce file size)
    /// </summary>
    public void RecordStamina(int timestep, float stamina, int stepCount)
    {
        if (!initialized) return;
        
        // Sample every N steps to reduce file size
        if (timestep % staminaSampleInterval == 0)
        {
            currentEpisodeStamina.Add(new StaminaDataPoint
            {
                timestep = timestep,
                stamina = stamina,
                stepCount = stepCount
            });
        }
    }
    
    /// <summary>
    /// Record reward component (called every step)
    /// </summary>
    public void RecordRewardComponent(string component, float value)
    {
        if (!initialized) return;

        switch (component)
        {
            case "progress":
                currentEpisodeRewards.progressReward += value;
                break;
            case "roll_base":
                currentEpisodeRewards.rollBaseReward += value;
                break;
            case "roll_style":
                currentEpisodeRewards.rollStyleBonus += value;
                break;
            case "target_reach":
                currentEpisodeRewards.targetReachReward += value;
                break;
            case "grounded":
                currentEpisodeRewards.groundedReward += value;
                break;
            case "low_stamina_penalty":
                currentEpisodeRewards.lowStaminaPenalty += value;
                break;
            case "time_penalty":
                currentEpisodeRewards.timePenalty += value;
                break;
            case "fall_penalty":
                currentEpisodeRewards.fallPenalty += value;
                break;
        }
    }
    
    /// <summary>
    /// End episode and save data
    /// </summary>
    public void EndEpisode(int episodeNumber, float episodeLength, float maxDistance, string endReason, int stepCount)
    {
        if (!initialized)
        {
            Debug.LogWarning($"[TrainingLogger] EndEpisode called but not initialized. Episode {episodeNumber} data will not be saved.");
            return;
        }
        
        // Save episode data
        episodeDataList.Add(new EpisodeData
        {
            episodeNumber = episodeNumber,
            stepCount = stepCount,
            length = episodeLength,
            maxDistance = maxDistance,
            success = endReason == "Success"
        });
        
        // Save reward components
        currentEpisodeRewards.totalReward = currentEpisodeRewards.progressReward +
                                           currentEpisodeRewards.rollBaseReward +
                                           currentEpisodeRewards.rollStyleBonus +
                                           currentEpisodeRewards.targetReachReward +
                                           currentEpisodeRewards.groundedReward +
                                           currentEpisodeRewards.lowStaminaPenalty +
                                           currentEpisodeRewards.timePenalty +
                                           currentEpisodeRewards.fallPenalty;
        rewardComponentsList.Add(currentEpisodeRewards);
        
        // Save stamina trajectory for this episode (write immediately)
        if (currentEpisodeStamina.Count > 0)
        {
            WriteStaminaTrajectory(episodeNumber, currentEpisodeStamina);
        }
        else if (episodeNumber > 1) // Don't warn on first episode
        {
            Debug.LogWarning($"[TrainingLogger] Episode {episodeNumber} has no stamina data. RecordStamina may not have been called or timestep % {staminaSampleInterval} != 0.");
        }
        
        // Batch flush every 10 episodes for performance (was flushing every episode, too slow)
        // FlushAll() in OnDestroy() ensures final data is saved even if training stops early
        if (episodeDataList.Count >= 10)
        {
            FlushEpisodeData();
        }
        if (rewardComponentsList.Count >= 10)
        {
            FlushRewardComponents();
        }
        
        // Only log every 10 episodes to reduce spam
        if (episodeNumber % 10 == 0)
        {
            Debug.Log($"[TrainingLogger] Episode {episodeNumber} ended ({endReason}). Data will be flushed at next batch.");
        }
    }
    
    /// <summary>
    /// Write all remaining data to disk (call at end of training)
    /// </summary>
    public void FlushAll()
    {
        if (!initialized)
        {
            Debug.LogWarning("[TrainingLogger] FlushAll called but not initialized. No data to flush.");
            return;
        }
        
        Debug.Log("[TrainingLogger] FlushAll called - writing all remaining data...");
        FlushEpisodeData();
        FlushRewardComponents();
        WriteMetadata();
        
        // Verify critical files exist
        VerifyFilesExist();
    }
    
    /// <summary>
    /// Verify that expected files exist and log their status
    /// </summary>
    private void VerifyFilesExist()
    {
        if (!initialized || string.IsNullOrEmpty(runLogsDir)) return;
        
        string[] expectedFiles = {
            "episode_data.json",
            "reward_components.json",
            "stamina_trajectories.json"
        };
        
        Debug.Log("[TrainingLogger] Verifying files exist...");
        foreach (string fileName in expectedFiles)
        {
            string filePath = Path.Combine(runLogsDir, fileName);
            if (File.Exists(filePath))
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                Debug.Log($"[TrainingLogger] ✓ {fileName} exists ({fileInfo.Length} bytes, modified: {fileInfo.LastWriteTime})");
            }
            else
            {
                Debug.LogWarning($"[TrainingLogger] ✗ {fileName} does NOT exist at {filePath}");
            }
        }
        
        // Check metadata
        string trainingDir = Path.GetDirectoryName(runLogsDir);
        if (!string.IsNullOrEmpty(trainingDir))
        {
            string metadataPath = Path.Combine(trainingDir, "metadata.json");
            if (File.Exists(metadataPath))
            {
                Debug.Log($"[TrainingLogger] ✓ metadata.json exists");
            }
            else
            {
                Debug.LogWarning($"[TrainingLogger] ✗ metadata.json does NOT exist at {metadataPath}");
            }
        }
    }
    
    private void FlushEpisodeData()
    {
        // Early return if no data to flush (not an error)
        if (episodeDataList.Count == 0) return;
        
        if (!initialized || string.IsNullOrEmpty(runLogsDir))
        {
            Debug.LogWarning("[TrainingLogger] Cannot flush episode data: not initialized");
            return;
        }
        
        string filePath = Path.Combine(runLogsDir, "episode_data.json");
        
        try
        {
            // Read existing data if file exists (skip File.Exists check - just try/catch)
            List<EpisodeData> allData = new List<EpisodeData>();
            try
            {
                string existingJson = File.ReadAllText(filePath);
                var existing = JsonUtility.FromJson<EpisodeDataList>(existingJson);
                if (existing != null && existing.episodes != null)
                {
                    allData.AddRange(existing.episodes);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File doesn't exist yet, start fresh
            }
            catch (System.Exception readEx)
            {
                Debug.LogWarning($"[TrainingLogger] Could not read existing episode_data.json, starting fresh: {readEx.Message}");
            }
            
            // Add new data
            allData.AddRange(episodeDataList);
            
            // Write back
            EpisodeDataList dataList = new EpisodeDataList { episodes = allData.ToArray() };
            string json = JsonUtility.ToJson(dataList, true);
            File.WriteAllText(filePath, json);
            
            int count = episodeDataList.Count;
            episodeDataList.Clear();
            Debug.Log($"[TrainingLogger] ✓ Wrote {count} new episode data entries (total: {allData.Count}) to {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrainingLogger] CRITICAL ERROR writing episode data to {filePath}: {e.Message}\nStackTrace: {e.StackTrace}");
            // Don't clear the list on error - keep data for retry
        }
    }
    
    private void FlushRewardComponents()
    {
        // Early return if no data to flush (not an error)
        if (rewardComponentsList.Count == 0) return;
        
        if (!initialized || string.IsNullOrEmpty(runLogsDir))
        {
            Debug.LogWarning("[TrainingLogger] Cannot flush reward components: not initialized");
            return;
        }
        
        string filePath = Path.Combine(runLogsDir, "reward_components.json");
        
        try
        {
            // Read existing data if file exists (skip File.Exists check - just try/catch)
            List<RewardComponents> allData = new List<RewardComponents>();
            try
            {
                string existingJson = File.ReadAllText(filePath);
                var existing = JsonUtility.FromJson<RewardComponentsList>(existingJson);
                if (existing != null && existing.rewards != null)
                {
                    allData.AddRange(existing.rewards);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File doesn't exist yet, start fresh
            }
            catch (System.Exception readEx)
            {
                Debug.LogWarning($"[TrainingLogger] Could not read existing reward_components.json, starting fresh: {readEx.Message}");
            }
            
            // Add new data
            allData.AddRange(rewardComponentsList);
            
            // Write back
            RewardComponentsList dataList = new RewardComponentsList { rewards = allData.ToArray() };
            string json = JsonUtility.ToJson(dataList, true);
            File.WriteAllText(filePath, json);
            
            int count = rewardComponentsList.Count;
            rewardComponentsList.Clear();
            Debug.Log($"[TrainingLogger] ✓ Wrote {count} new reward component entries (total: {allData.Count}) to {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrainingLogger] CRITICAL ERROR writing reward components to {filePath}: {e.Message}\nStackTrace: {e.StackTrace}");
            // Don't clear the list on error - keep data for retry
        }
    }
    
    private void WriteStaminaTrajectory(int episodeNumber, List<StaminaDataPoint> trajectory)
    {
        if (!initialized || string.IsNullOrEmpty(runLogsDir))
        {
            Debug.LogWarning("[TrainingLogger] Cannot write stamina trajectory: not initialized");
            return;
        }
        
        string filePath = Path.Combine(runLogsDir, "stamina_trajectories.json");
        
        try
        {
            // Read existing data if file exists (skip File.Exists check - just try/catch)
            List<StaminaTrajectory> allTrajectories = new List<StaminaTrajectory>();
            try
            {
                string existingJson = File.ReadAllText(filePath);
                var existing = JsonUtility.FromJson<StaminaTrajectoryList>(existingJson);
                if (existing != null && existing.trajectories != null)
                {
                    allTrajectories.AddRange(existing.trajectories);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // File doesn't exist yet, start fresh
            }
            catch (System.Exception readEx)
            {
                Debug.LogWarning($"[TrainingLogger] Could not read existing stamina_trajectories.json, starting fresh: {readEx.Message}");
            }
            
            // Add new trajectory
            allTrajectories.Add(new StaminaTrajectory
            {
                episodeNumber = episodeNumber,
                dataPoints = trajectory.ToArray()
            });
            
            // Write back
            StaminaTrajectoryList trajectoryList = new StaminaTrajectoryList { trajectories = allTrajectories.ToArray() };
            string json = JsonUtility.ToJson(trajectoryList, true);
            File.WriteAllText(filePath, json);
            
            // Only log every 10 episodes to reduce spam
            if (episodeNumber % 10 == 0)
            {
                Debug.Log($"[TrainingLogger] ✓ Wrote stamina trajectory for episode {episodeNumber} ({trajectory.Count} points) to {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrainingLogger] CRITICAL ERROR writing stamina trajectory to {filePath}: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    private void WriteMetadata()
    {
        // Only write if initialized and runLogsDir is set
        if (!initialized || string.IsNullOrEmpty(runLogsDir))
        {
            Debug.LogWarning("[TrainingLogger] Cannot write metadata: not initialized or runLogsDir is null");
            return;
        }
        
        // Write metadata to training directory (parent of run_logs)
        string trainingDir = Path.GetDirectoryName(runLogsDir);
        if (string.IsNullOrEmpty(trainingDir))
        {
            Debug.LogError("[TrainingLogger] Cannot write metadata: training directory path is null");
            return;
        }
        
        string filePath = Path.Combine(trainingDir, "metadata.json");
        
        try
        {
            string json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(filePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TrainingLogger] Failed to write metadata to {filePath}: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    // Data structures for JSON serialization
    [System.Serializable]
    private class EpisodeData
    {
        public int episodeNumber;
        public int stepCount;
        public float length;
        public float maxDistance;
        public bool success;
    }
    
    [System.Serializable]
    private class EpisodeDataList
    {
        public EpisodeData[] episodes;
    }
    
    [System.Serializable]
    private class StaminaDataPoint
    {
        public int timestep;
        public float stamina;
        public int stepCount;
    }
    
    [System.Serializable]
    private class StaminaTrajectory
    {
        public int episodeNumber;
        public StaminaDataPoint[] dataPoints;
    }
    
    [System.Serializable]
    private class StaminaTrajectoryList
    {
        public StaminaTrajectory[] trajectories;
    }
    
    [System.Serializable]
    public class RewardComponents
    {
        public int episodeNumber;
        public int stepCount;
        public float progressReward;
        public float rollBaseReward;
        public float rollStyleBonus;
        public float targetReachReward;
        public float groundedReward;
        public float lowStaminaPenalty;
        public float timePenalty;
        public float fallPenalty;
        public float totalReward;
    }
    
    [System.Serializable]
    private class RewardComponentsList
    {
        public RewardComponents[] rewards;
    }
    
    [System.Serializable]
    private class TrainingMetadata
    {
        public float styleEpisodeFrequency;
        public string trainingStartTime;
    }
}

