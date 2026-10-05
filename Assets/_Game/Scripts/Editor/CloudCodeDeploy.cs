using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;

public static class CloudCodeDeploy
{
    private const string Folder = "Assets/_Game/Services/CloudCode/";

    [MenuItem("Pingi/Deploy Ghost Scripts")]
    public static void DeployGhostScripts()
    {
        EditorApplication.ExecuteMenuItem("Services/Deployment");

        var items = new List<IDeploymentItem>();
        foreach (var provider in Deployments.Instance.DeploymentProviders)
        {
            if (provider.Service != "Cloud Code") continue;
            foreach (var item in provider.DeploymentItems)
            {
                if (item.Path == null || !item.Path.Replace('\\', '/').Contains(Folder)) continue;
                RestoreName(item);
                items.Add(item);
            }
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("No Cloud Code scripts found in " + Folder);
            return;
        }

        Deployments.Instance.DeploymentWindow.Deploy(items).ContinueWith(task =>
        {
            var report = new StringBuilder(task.IsFaulted ? "Ghost scripts failed to deploy: " + task.Exception?.GetBaseException().Message : "Ghost scripts deployed:");
            foreach (var item in items) report.Append("\n  " + item.Name + " · " + item.Status.Message + " " + item.Status.MessageDetail);
            Debug.Log(report.ToString());
        });
    }

    private static void RestoreName(IDeploymentItem item)
    {
        var name = item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
        var fromPath = name?.PropertyType.GetMethod("FromPath", BindingFlags.Public | BindingFlags.Static);
        if (name == null || fromPath == null || !name.CanWrite) return;
        name.SetValue(item, fromPath.Invoke(null, new object[] { Path.GetFullPath(item.Path) }));
    }
}
