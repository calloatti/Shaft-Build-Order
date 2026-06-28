using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calloatti.ShaftBuildOrder
{
  public class ShaftDirectionVisualizer : BaseComponent, IAwakableComponent, IUpdatableComponent, IInitializablePreview, IUnfinishedStateListener, IFinishedStateListener
  {
    private static Mesh _sharedMesh;
    private static Material _sharedMaterial;

    private MeshRenderer _meshRenderer;
    private BlockObject _blockObject;
    private ShaftBuildOrderBlocker _blocker;

    public void Awake()
    {
      _blockObject = GetComponent<BlockObject>();
      _blocker = GetComponent<ShaftBuildOrderBlocker>();

      if (_sharedMesh == null)
      {
        _sharedMesh = new Mesh();
        _sharedMesh.vertices = new Vector3[] {
            new Vector3(0.5f, 1.05f, 0.2f),
            new Vector3(0.24f, 1.05f, 0.65f),
            new Vector3(0.76f, 1.05f, 0.65f)
        };
        _sharedMesh.triangles = new int[] { 0, 1, 2, 0, 2, 1 };
      }

      if (_sharedMaterial == null)
      {
        _sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        _sharedMaterial.color = Color.yellow;
      }

      MeshFilter meshFilter = GameObject.AddComponent<MeshFilter>();
      meshFilter.sharedMesh = _sharedMesh;

      _meshRenderer = GameObject.AddComponent<MeshRenderer>();
      _meshRenderer.sharedMaterial = _sharedMaterial;
      _meshRenderer.enabled = false;
    }

    public void Update()
    {
      if (!_blockObject.IsPreview && !_blockObject.IsUnfinished)
      {
        if (_meshRenderer.enabled) _meshRenderer.enabled = false;
        return;
      }

      bool shouldShowArrow = false;

      if (_blockObject.IsPreview)
      {
        shouldShowArrow = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
      }
      else if (_blocker != null)
      {
        shouldShowArrow = _blocker.EnforceBuildOrder;
      }

      if (_meshRenderer.enabled != shouldShowArrow)
      {
        _meshRenderer.enabled = shouldShowArrow;
      }
    }

    // Replaces OnEnterPreviewState()
    public void InitializePreview()
    {
      _meshRenderer.enabled = false;
      EnableComponent();
    }

    public void OnEnterUnfinishedState()
    {
      _meshRenderer.enabled = true;
      EnableComponent();
    }

    public void OnEnterFinishedState()
    {
      _meshRenderer.enabled = false;
      DisableComponent();
    }

    public void OnExitFinishedState() { }
    public void OnExitUnfinishedState() { }
  }
}