using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
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

    public bool EnforceBuildOrder { get; set; } = false;

    [Inject]
    public void InjectDependencies(EventBus eventBus, IBlockService blockService)
    {
      _eventBus = eventBus;
      _blockService = blockService;
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
          UpdateStatusUI();
        }
      }
    }

    public void OnEnterUnfinishedState()
    {
      _eventBus.Register(this);
      UpdateStatusUI();
    }

    public void OnExitUnfinishedState()
    {
      _eventBus.Unregister(this);
      _statusToggle.Deactivate();
    }

    public void OnEnterFinishedState() { }
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