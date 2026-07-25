using Bindito.Core;
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Navigation;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.StatusSystem;
using Timberborn.WorldPersistence;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calloatti.ShaftBuildOrder
{
  public class ShaftBuildOrderBlocker : BaseComponent, IAwakableComponent, IStartableComponent, IPostPlacementChangeListener, IPersistentEntity, IUnfinishedStateListener, IFinishedStateListener
  {
    private static readonly ComponentKey ShaftBuildOrderBlockerKey = new ComponentKey("ShaftBuildOrderBlocker");
    private static readonly PropertyKey<bool> EnforceOrderKey = new PropertyKey<bool>("EnforceOrder");

    private BlockObject _blockObject;
    private StatusToggle _statusToggle;
    private EventBus _eventBus;
    private IBlockService _blockService;
    private NavMeshGroupService _navMeshGroupService;
    private NavMeshUpdater _navMeshUpdater;

    private readonly List<NavMeshEdge> _roadEdges = new List<NavMeshEdge>();

    public bool EnforceBuildOrder { get; set; } = false;

    [Inject]
    public void InjectDependencies(EventBus eventBus, IBlockService blockService, NavMeshGroupService navMeshGroupService, NavMeshUpdater navMeshUpdater)
    {
      _eventBus = eventBus;
      _blockService = blockService;
      _navMeshGroupService = navMeshGroupService;
      _navMeshUpdater = navMeshUpdater;
    }

    public void Awake()
    {
      _blockObject = GetComponent<BlockObject>();

      // We keep the UI alert, but there is no BlockableObject kill switch here
      _statusToggle = StatusToggle.CreateNormalStatus("DirectionalBlocking", "Waiting for previous segment to be built");
    }

    public void Start()
    {
      GetComponent<StatusSubject>().RegisterStatus(_statusToggle);
    }

    public void OnPostPlacementChanged()
    {
      if (_blockObject.IsUnfinished && !_blockObject.IsPreview)
      {
        if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
        {
          EnforceBuildOrder = true;
          AddRoadEdges();
          UpdateStatusUI();
        }
      }
    }

    public void OnEnterUnfinishedState()
    {
      _eventBus.Register(this);
      if (EnforceBuildOrder)
      {
        AddRoadEdges();
      }
      UpdateStatusUI();
    }

    public void OnExitUnfinishedState()
    {
      _eventBus.Unregister(this);
      _statusToggle.Deactivate();
    }

    public void OnEnterFinishedState()
    {
      RemoveRoadEdges();
    }
    public void OnExitFinishedState() { }

    [OnEvent]
    public void OnEnteredFinishedState(EnteredFinishedStateEvent e)
    {
      UpdateStatusUI();
    }

    // --- READ BY THE HARMONY PATCH ---
    public bool ShouldBlockBuilders()
    {
      if (!EnforceBuildOrder || _blockObject.IsPreview || !_blockObject.IsUnfinished) return false;

      Vector3Int tileBehind = _blockObject.CoordinatesBehind();
      BlockObject behindBlock = _blockService.GetBottomObjectAt(tileBehind);

      return (behindBlock != null && behindBlock.IsUnfinished);
    }

    public void UpdateStatusUI()
    {
      if (ShouldBlockBuilders())
      {
        if (!_statusToggle.IsActive) _statusToggle.Activate();
      }
      else
      {
        if (_statusToggle.IsActive) _statusToggle.Deactivate();
      }
    }

    private void AddRoadEdges()
    {
      if (_roadEdges.Count > 0) return;

      int defaultGroupId = _navMeshGroupService.GetDefaultGroupId();
      var neighborDeltas = new Vector3Int[]
      {
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 1, 0)
      };

      foreach (Block block in _blockObject.PositionedBlocks.GetAllBlocks())
      {
        Vector3Int coord = block.Coordinates;
        foreach (Vector3Int delta in neighborDeltas)
        {
          Vector3Int neighbor = coord + delta;
          NavMeshEdge edgeFwd = NavMeshEdge.CreateGrouped(coord, neighbor, defaultGroupId, isRoad: true, cost: 1f);
          NavMeshEdge edgeBack = NavMeshEdge.CreateGrouped(neighbor, coord, defaultGroupId, isRoad: true, cost: 1f);

          _roadEdges.Add(edgeFwd);
          _roadEdges.Add(edgeBack);

          _navMeshUpdater.EnqueueRegularChange(new NavMeshChangeSpecification(edgeFwd, NavMeshChangeType.AddEdge));
          _navMeshUpdater.EnqueueRegularChange(new NavMeshChangeSpecification(edgeBack, NavMeshChangeType.AddEdge));
        }
      }
    }

    private void RemoveRoadEdges()
    {
      foreach (NavMeshEdge edge in _roadEdges)
      {
        _navMeshUpdater.EnqueueRegularChange(new NavMeshChangeSpecification(edge, NavMeshChangeType.RemoveEdge));
      }
      _roadEdges.Clear();
    }

    // --- SAVE AND LOAD LOGIC ---
    public void Save(IEntitySaver entitySaver)
    {
      entitySaver.GetComponent(ShaftBuildOrderBlockerKey).Set(EnforceOrderKey, EnforceBuildOrder);
    }

    public void Load(IEntityLoader entityLoader)
    {
      if (entityLoader.TryGetComponent(ShaftBuildOrderBlockerKey, out var objectLoader))
      {
        EnforceBuildOrder = objectLoader.Get(EnforceOrderKey);
      }
    }
  }
}