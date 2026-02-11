using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[ExecuteInEditMode]
public class ExecuteInUpdate : MonoBehaviour
{
    static List<Action> Actions;
    private void Awake()
    {
        Actions = new List<Action>();
    }
    private void Update()
    {
      
        if(Actions != null && Actions.Count > 0)
        {
            for(int i = 0; i < Actions.Count;i++)
            {
                Actions[i]?.Invoke();
            }
            Actions.Clear();
        }
    }

    public static void ExecuteInNextFrame(Action _action)
    {
        if (Actions == null)
        {
            Actions = new List<Action>();
        }
        Actions.Add(_action);
    }
}
