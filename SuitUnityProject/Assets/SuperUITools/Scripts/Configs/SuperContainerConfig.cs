using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

public class SuperContainerConfig : MonoBehaviour 
{   
    //a container can either be a true container or a proxy for a UI control (most commonly buttons)
    public CustomClass[] customContainers;

    private static SuperContainerConfig _instance = null;
    public static Dictionary<string, Type> containerClasses;    

    public static SuperContainerConfig instance
    {
        get
        {
            if(_instance == null)
            {
                GameObject config_go = GameObject.Find("SuperConfig");
                if(config_go == null)
                {
                    config_go = new GameObject();
                    config_go.name = "SuperConfig";
                }

                SuperContainerConfig container_config = config_go.GetComponent<SuperContainerConfig>();
                if(container_config == null)
                {
                    container_config = config_go.AddComponent(typeof(SuperContainerConfig)) as SuperContainerConfig;
                    container_config.customContainers = new CustomClass[0];
                }

                _instance = container_config;
            }

            return _instance;
        }
    }

    public static void ProcessNode(SuperMetaNode root_node, Transform parent, Dictionary<string,object> node)
    {
        string name = (string)node["name"];
        string container_type = name.Split('_')[0];

        List<string> keys = new List<string>(containerClasses.Keys);
        if(containerClasses.ContainsKey(container_type))
        {
            object[] args = new object[3];
            args[0] = root_node;
            args[1] = parent;
            args[2] = node;
            containerClasses[container_type].GetMethod("ProcessNode").Invoke(null, args);
            return;
        }

        GameObject game_object = new GameObject();
        SuperContainer container = game_object.AddComponent(typeof(SuperContainer)) as SuperContainer;

        container.CreateRectTransform(game_object, node);

        root_node.containerReferences.Add(new ContainerReference(name, container));
        container.name = name;
        container.hierarchyDescription = "CONTAINER";

        container.cachedMetadata = node;
        container.rootNode = root_node;

        game_object.transform.SetParent(parent);
        container.Reset();

        root_node.ProcessChildren(container.transform, node["children"] as List<object>);
    }

    public static void RefreshClasses()
    {
        containerClasses = new Dictionary<string, Type>();
        foreach(CustomClass custom_container in instance.customContainers)
        {
            Type container_class = Type.GetType(custom_container.scriptName);
            if(container_class == null)
            {
                Debug.Log("[ERROR] " + custom_container.scriptName + " COULD NOT BE FOUND");
                continue;
            }
            containerClasses[custom_container.prefix] = container_class;
        }
    }
    
}
