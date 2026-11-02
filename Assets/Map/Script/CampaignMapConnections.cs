using System.Collections.Generic;
using UnityEngine;

/// <summary>Draws the scene's authored connections; moving a node updates its lines in the Editor.</summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public sealed class CampaignMapConnections : MonoBehaviour
{
    [SerializeField] private MapNode[] nodes;
    private Mesh mesh;
    private Material material;
    private int lastHash;
    private bool initialized;
    public void Configure(MapNode[] value){nodes=value;initialized=false;Refresh();}
    private void OnEnable(){initialized=false;Refresh();}
    private void LateUpdate()=>Refresh();
    private void Refresh()
    {
        if(nodes==null)return;
        int hash=17;
        unchecked {
            foreach(var node in nodes)if(node!=null) {
                hash=hash*31+node.transform.position.GetHashCode();
                foreach(var next in node.ConnectedNodes)hash=hash*31+(next!=null?next.GetInstanceID():0);
            }
        }
        if(initialized && hash==lastHash)return;
        initialized=true;lastHash=hash;
        if(mesh==null){mesh=new Mesh{name="Campaign connections",hideFlags=HideFlags.DontSave};GetComponent<MeshFilter>().sharedMesh=mesh;}
        if(material==null) {
            material=new Material(Resources.Load<Shader>("CampaignLines")){hideFlags=HideFlags.DontSave};
            material.SetColor("_BaseColor",UITheme.Alpha(UITheme.Line,.55f));GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        var vertices=new List<Vector3>();var indices=new List<int>();
        foreach(var node in nodes)if(node!=null)foreach(var next in node.ConnectedNodes)if(next!=null) {
            indices.Add(vertices.Count);vertices.Add(transform.InverseTransformPoint(node.transform.position));
            indices.Add(vertices.Count);vertices.Add(transform.InverseTransformPoint(next.transform.position));
        }
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetIndices(indices,MeshTopology.Lines,0);mesh.RecalculateBounds();
    }
    private void OnDisable()
    {
        if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
        if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        mesh=null;material=null;initialized=false;
    }
}
