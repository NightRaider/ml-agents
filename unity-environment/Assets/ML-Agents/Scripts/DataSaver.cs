using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using Newtonsoft.Json;
using System.Linq;


/** Saves data states, actions and rewards taken by the brain
 * 
 */ 
public class DataSaver
{

    struct SavedBrainInfo
    {
        public List<int> observations;
        public List<float> state;
        public List<float> memory;
        public List<float> reward;
        public List<bool> done;
        public List<int> agents;
        public List<float> actions;
    }

    struct SavedBrainParameters
    {
        public string timeStamp;
        public BrainParameters brainParameters;
        public string brainType;
    }

    struct SavedReset
    {
        public int episodeCount;
        public Dictionary<string,float> resetParameters;
    }



    private Brain brain;
    private string saveFilePath;
    private SavedBrainInfo savedData;

    /// The constructor of DataSaver. It determines the saving path.
    public DataSaver(Brain b)
    {
        brain = b;
        #if UNITY_STANDALONE
        string logDirectoryPath = Path.GetFullPath(".") + "/";
        if (Application.isEditor)
        {
            logDirectoryPath = logDirectoryPath + "../";
        }
        logDirectoryPath = logDirectoryPath + "saved-plays/" + brain.gameObject.transform.parent.name;

        if (!Directory.Exists(logDirectoryPath))
        { 
            Directory.CreateDirectory(logDirectoryPath);
        } 
        int file_number = 0;
        saveFilePath = logDirectoryPath + "/" + brain.gameObject.name;
        while (File.Exists(saveFilePath))
        {
            file_number += 1;
            saveFilePath = logDirectoryPath + "/" + brain.gameObject.name + file_number;
        }

        if (!File.Exists(saveFilePath))
        {
            FileStream fs = File.Create(saveFilePath);
            fs.Close();
        }
        #endif
    }

    /// Saves the brain parameters and a time stamp 
    public void SaveBrain()
    {
        #if UNITY_STANDALONE
        SavedBrainParameters bp = new SavedBrainParameters();
        bp.timeStamp = System.DateTime.Now.ToString();
        bp.brainParameters = brain.brainParameters;
        bp.brainType = brain.brainType.ToString();
        StreamWriter sr = File.AppendText(saveFilePath);
        sr.WriteLine(JsonConvert.SerializeObject(bp, Formatting.None));
        sr.Close();
        //Write a timeStamp and the brainParameters
        #endif
    }

    /// Saves an Academy Reset event: Contains episodeCount and resetParameters
    public void SaveReset()
    {
        #if UNITY_STANDALONE
        Academy academy = brain.gameObject.transform.parent.GetComponent<Academy>();
        SavedReset savedReset = new SavedReset();
        savedReset.episodeCount = academy.episodeCount;
        savedReset.resetParameters = academy.resetParameters;
        StreamWriter sr = File.AppendText(saveFilePath);
        sr.WriteLine(JsonConvert.SerializeObject(savedReset, Formatting.None));
        sr.Close();
        #endif
    }

    /// Saves the state as collected by the brain for each agent
    public void SaveState()
    {
        #if UNITY_STANDALONE
        savedData = new SavedBrainInfo();
        savedData.agents = new List<int>(brain.agents.Keys);
        if (savedData.agents.Count() > 0)
        {
            savedData.state = new List<float>();
            savedData.reward = new List<float>();
            savedData.memory = new List<float>();
            savedData.done = new List<bool>();
            Dictionary<int, List<float>> collectedStates = brain.CollectStates();
            Dictionary<int, float> collectedRewards = brain.CollectRewards();
            Dictionary<int, float[]> collectedMemories = brain.CollectMemories();
            Dictionary<int, bool> collectedDones = brain.CollectDones();

            foreach (int id in savedData.agents)
            {
                savedData.state = savedData.state.Concat(collectedStates[id]).ToList();
                savedData.reward.Add(collectedRewards[id]);
                savedData.memory = savedData.memory.Concat(collectedMemories[id].ToList()).ToList();
                savedData.done.Add(collectedDones[id]);
            }
            savedData.observations = new List<int>();
            List<float[,,,]> obs_list_matrix = brain.GetObservationMatrixList(savedData.agents);
            for (int obs_number = 0; obs_number < brain.brainParameters.cameraResolutions.Count(); obs_number++)
            {
                savedData.observations = savedData.observations.Concat(flatten_obs_matrix(obs_list_matrix[obs_number])).ToList();
            }
        }
        #endif
    }


    /// Saves the action decided by the brain for each agent
    public void SaveAction()
    {
        #if UNITY_STANDALONE
        if (savedData.agents == null)
        {
            return;
        }
        savedData.actions = new List<float>();
        Dictionary<int, float[]> collectedActions = brain.CollectActions();
        foreach (int id in savedData.agents)
        {
            savedData.actions = savedData.actions.Concat(collectedActions[id].ToList()).ToList();
        }
        if (savedData.agents.Count() > 0)
        {
            StreamWriter sr = File.AppendText(saveFilePath);
            sr.WriteLine(JsonConvert.SerializeObject(savedData, Formatting.None));
            sr.Close();
        }
        #endif
    }

    private List<int> flatten_obs_matrix(float[,,,] mat)
    {
        List<int> r = new List<int>();
        for (int i = 0; i < mat.GetLength(0); i++)
        {
            for (int j = 0; j < mat.GetLength(1); j++)
            {
                for (int k = 0; k < mat.GetLength(2); k++)
                {
                    for (int l = 0; l < mat.GetLength(3); l++)
                    {
                        r.Add((int)(mat[i, j, k, l] * 255));
                    }
                }
            }
        }
        return r;
    }

}