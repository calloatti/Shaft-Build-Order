using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;      // This is where IInitializableEntity lives!
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.StatusSystem;
using Timberborn.WorldPersistence;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Calloatti.ShaftBuildOrder
{
  public class ShaftBuildOrderBlocker : BaseComponent, IAwakableComponent, IInitializableEntity, IPostPlacementChangeListener, IPersistentEntity, IUnfinishedStateListener, IFinishedStateListener
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

      _statusToggle = StatusToggle.CreateNormalStatus("DirectionalBlocking", "Waiting for previous segment to be built");
    }

    // Replaces Start() and IStartableComponent
    public void InitializeEntity()
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